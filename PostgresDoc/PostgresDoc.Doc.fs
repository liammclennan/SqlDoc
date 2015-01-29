module PostgresDoc.Doc

open Npgsql
open Newtonsoft.Json

[<CLIMutable>]
type Store = { connString: string }

type KeyedValue<'a> = 'a * obj

type Operation<'a> =
    | Insert of KeyedValue<'a>
    | Update of KeyedValue<'a>
    | Delete of KeyedValue<'a>

type UnitOfWork<'a> = Operation<'a> list

let insert (key:'a) (value:'b) =
    Insert (key, box value)

let update (key:'a) (value:'b) =
    Update (key, box value)

let delete (key:'a) (value:'b) =
    Delete (key, box value)

let private tableName o = 
    o.GetType().Name.ToLowerInvariant()

let commit (store:Store) (uow:UnitOfWork<'a>) = 
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    use transaction = conn.BeginTransaction()

    let insert id o =
        let pattern = o |> tableName |> sprintf @"insert into ""%s"" (id, data) values(:id, :data)"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = id)) |> ignore
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.ExecuteNonQuery()   

    let update id o = 
        let pattern = o |> tableName |> sprintf @"update ""%s"" set data = :data where id = :id"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = id)) |> ignore
        command.ExecuteNonQuery()

    let delete id o =
        let pattern = o |> tableName |> sprintf @"delete from ""%s"" where id = :id"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = id)) |> ignore
        command.ExecuteNonQuery()

    if List.isEmpty uow then 
        ()
    else
        try
            try
                for op in List.rev uow do
                    match op with
                        | Insert kv -> kv ||> insert
                        | Update kv -> kv ||> update
                        | Delete kv -> kv ||> delete
                    |> ignore
            with
            | :? NpgsqlException -> 
                transaction.Rollback()
                reraise()
            transaction.Commit()
        finally
            conn.Close()
        ()

let query<'a> (store:Store) select (m:(string * 'c) list) : 'a array = 
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


