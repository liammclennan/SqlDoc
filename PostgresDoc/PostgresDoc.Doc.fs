module PostgresDoc.Doc

open Npgsql
open System.Data.SqlClient
open System.Data.Common
open Newtonsoft.Json
open System.Xml.Serialization
open System.IO
open System.Xml

type Store = SqlStore of connString:string | PostgresStore of connString:string

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

let private getConnection = function
    | SqlStore conn -> new SqlConnection(conn) :> DbConnection
    | PostgresStore conn -> new NpgsqlConnection(conn) :> DbConnection

let private getCommand (store:Store) pattern (conn:DbConnection) : DbCommand =
    match store with 
        | SqlStore cs -> new SqlCommand(pattern, conn :?> SqlConnection) :> DbCommand
        | PostgresStore cs -> new NpgsqlCommand(pattern, conn :?> NpgsqlConnection) :> DbCommand

let private getParameter (store:Store) conn k v =
    match store with
        | SqlStore cs -> new SqlParameter(ParameterName = k, Value = v) :> DbParameter
        | PostgresStore cs -> new NpgsqlParameter(ParameterName = k, Value = v) :> DbParameter

let private serialize o = function
    | SqlStore cs -> 
        let serializer, writer = new XmlSerializer(o.GetType()), new StringWriter()
        serializer.Serialize(writer, o)
        writer.ToString()
    | PostgresStore cs ->
        JsonConvert.SerializeObject(o)

let private deserialize<'a> s = function
    | SqlStore cs ->
        let serializer = new XmlSerializer(typedefof<'a>)
        serializer.Deserialize(XmlReader.Create(new StringReader(s))) :?> 'a
    | PostgresStore cs ->
        JsonConvert.DeserializeObject<'a>(s)

let commit (store:Store) (uow:UnitOfWork<'a>) = 
    use conn = getConnection store
    
    let insertUpdate id o pattern =
        let pattern = o |> tableName |> sprintf pattern
        let command = getCommand store pattern conn
        command.Parameters.Add(getParameter store conn "id" id) |> ignore
        command.Parameters.Add(getParameter store conn "data" (serialize o store)) |> ignore
        command.ExecuteNonQuery()

    let insert (id:'a) (o:obj) = insertUpdate id o @"insert into ""%s"" (id, data) values(:id, :data)"
    let update (id:'a) (o:obj) = insertUpdate id o @"update ""%s"" set data = :data where id = :id"

    let delete id o =
        let pattern = o |> tableName |> sprintf @"delete from ""%s"" where id = :id"
        let command = getCommand store pattern conn
        command.Parameters.Add(getParameter store conn "id" id) |> ignore
        command.ExecuteNonQuery()

    conn.Open()
    use transaction = conn.BeginTransaction()

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
            | _ -> 
                transaction.Rollback()
                reraise()
            transaction.Commit()
        finally
            conn.Close()
        ()

let query<'a> (store:Store) select (m:(string * 'c) list) : 'a array = 
    let ps = Map.ofList m
    use conn = getConnection store
    conn.Open()
    use command = getCommand store select conn
    let parameters = 
        ps 
        |> Map.map (fun k v -> getParameter store conn k v) 
        |> Map.toArray 
        |> Array.map (fun (k,v) -> v)
    command.Parameters.AddRange(parameters)
    try
        use dr = command.ExecuteReader()
        [|
            while dr.Read() do
                let data = dr.[0] :?> string
                yield deserialize<'a> data store
        |]
    finally
        conn.Close()


