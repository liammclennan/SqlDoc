module PostgresDoc.Doc

open Npgsql
open Newtonsoft.Json

type IPDDocument =
    abstract tableName: unit -> string
    abstract id: unit -> System.Guid

type Store = { connString: string }

let insert (store:Store) (os:seq<IPDDocument>) = 
    if Seq.isEmpty os then
        ()
    else
        let tableName = (Seq.head os).tableName()
        let insertValues (os:IPDDocument seq) =
            use conn = new NpgsqlConnection(store.connString)
            conn.Open()
            let data = os |> Seq.map JsonConvert.SerializeObject |> Seq.map (fun s -> new NpgsqlCommand("insert into " + tableName + " values('" + s + "')", conn))
            try
                for c in data do
                    c.ExecuteNonQuery() |> ignore
            finally
                conn.Close()
        try
            insertValues os
        with
            | :? NpgsqlException -> 
                use conn = new NpgsqlConnection(store.connString)
                conn.Open()
                try
                    let createComm = new NpgsqlCommand("create table " + tableName + " ( data json NOT NULL )", conn)
                    createComm.ExecuteNonQuery() |> ignore
                finally
                    conn.Close()
                insertValues os

let update (store:Store) (o:IPDDocument) =
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let data = JsonConvert.SerializeObject(o)
    let query = "update " + o.tableName() + " set data = '" + data + "' where data->>'_id' = '" + (o.id().ToString()) + "'"
    let command = new NpgsqlCommand(query, conn)
    try
        command.ExecuteNonQuery() |> ignore
    finally
        conn.Close()

let delete (store:Store) (o:IPDDocument) =
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let query = "delete from " + o.tableName() + " where data->>'_id' = '" + (o.id().ToString()) + "'"
    let command = new NpgsqlCommand(query, conn)
    try
        command.ExecuteNonQuery() |> ignore
    finally
        conn.Close()

let query<'a> (store:Store) select (ps:Map<string,obj>) : 'a array = 
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
