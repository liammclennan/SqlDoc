module PostgresDoc.Doc

open Npgsql
open Newtonsoft.Json

type IPDDocument =
    abstract id: unit -> System.Guid

[<CLIMutable>]
type Store = { connString: string }

type Operation =
    | Insert of obj
    | Update of obj
    | Delete of obj

type UnitOfWork = Operation list

let private tableName o = 
    o.GetType().Name

let commit (store:Store) (uow:UnitOfWork) = 
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let transaction = conn.BeginTransaction()

    let insert (o:IPDDocument) =
        let pattern = o |> tableName |> sprintf "insert into %s (data) values(:data)"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.ExecuteNonQuery()

    let update (o:IPDDocument) = 
        let pattern = o |> tableName |> sprintf "update %s set data = :data where data->>'_id' = :id"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = o.id())) |> ignore
        command.ExecuteNonQuery()

    let delete (o:IPDDocument) =
        let pattern = o |> tableName |> sprintf "delete from %s where data->>'_id' = :id"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = o.id())) |> ignore
        command.ExecuteNonQuery()

    if List.isEmpty uow then 
        ()
    else
        try
            try
                for op in List.rev uow do
                    match op with
                        | Insert o -> o :?> IPDDocument |> insert
                        | Update o -> o :?> IPDDocument |> update
                        | Delete o -> o :?> IPDDocument |> delete
                    |> ignore
            with
            | :? NpgsqlException -> 
                transaction.Rollback()
                reraise()
            transaction.Commit()
        finally
            conn.Close()
        ()

let query<'a> (store:Store) select (m:('b * 'c) list) : 'a array = 
    let ps = Map.ofList m
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    use command = new NpgsqlCommand(select, conn)
    let parameters = 
        ps 
        |> Map.map (fun k v -> new NpgsqlParameter(ParameterName = k, Value = v)) 
        |> Map.toArray 
        |> Array.map (fun (k,v) -> v)
    command.Parameters.AddRange(parameters)
    try
        use dr = command.ExecuteReader()
        seq {
            while dr.Read() do
                let data = dr.[0] :?> string
                yield JsonConvert.DeserializeObject<'a>(data)
        } |> Seq.toArray
    finally
        conn.Close()


