module PostgresDoc.UnitOfWork
open Npgsql
open Newtonsoft.Json
open PostgresDoc.Doc

type Operation =
    | Insert of obj
    | Update of obj
    | Delete of obj

type UnitOfWork = Operation list

let commit (store:Store) (uow:UnitOfWork) = 
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let transaction = conn.BeginTransaction()

    let insert (o:IDocument) =
        let pattern = o.tableName() |> sprintf "insert into %s (data) values(:data)"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.ExecuteNonQuery()

    let update (o:IDocument) = 
        let pattern = o.tableName() |> sprintf "update %s set data = :data where data->>'_id' = :id"
        let command = new NpgsqlCommand(pattern, conn)
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "data", Value = JsonConvert.SerializeObject(o))) |> ignore
        command.Parameters.Add(new NpgsqlParameter(ParameterName = "id", Value = o.id())) |> ignore
        command.ExecuteNonQuery()

    let delete (o:IDocument) =
        let pattern = o.tableName() |> sprintf "delete from %s where data->>'_id' = :id"
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
                        | Insert o -> o :?> IDocument |> insert
                        | Update o -> o :?> IDocument |> update
                        | Delete o -> o :?> IDocument |> delete
                        | _ -> 0
            with
            | :? NpgsqlException -> 
                transaction.Rollback()
                reraise()
            transaction.Commit()
        finally
            conn.Close()
        ()
