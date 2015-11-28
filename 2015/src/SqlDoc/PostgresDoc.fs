module PostgresDoc
open Npgsql
open System.Data.SqlClient
open System.Data.Common
open Newtonsoft.Json
open System.IO

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

let private getCommand (store:Store) pattern (conn:DbConnection) (transaction:DbTransaction) : DbCommand =
    match store with 
        | SqlStore cs -> new SqlCommand(pattern, conn :?> SqlConnection, transaction :?> SqlTransaction) :> DbCommand
        | PostgresStore cs -> new NpgsqlCommand(pattern, conn :?> NpgsqlConnection, transaction :?> NpgsqlTransaction) :> DbCommand

let private getParameter (store:Store) conn k v =
    match store with
        | SqlStore cs -> new SqlParameter(ParameterName = k, Value = v) :> DbParameter
        | PostgresStore cs -> new NpgsqlParameter(ParameterName = k, Value = v) :> DbParameter

let private serialize o = function
    | SqlStore cs -> 
        SharpXml.XmlSerializer.SerializeToString o
    | PostgresStore cs ->
        JsonConvert.SerializeObject(o)

let private deserialize<'a> s = function
    | SqlStore cs ->
        SharpXml.XmlSerializer.DeserializeFromString<'a>(s)
    | PostgresStore cs ->
        JsonConvert.DeserializeObject<'a>(s)

let private parameterPrefix = function
    | SqlStore _ -> "@"        
    | PostgresStore _ -> ":"

let commit (store:Store) (uow:UnitOfWork<'a>) = 
    use conn = getConnection store
    conn.Open()
    use transaction = conn.BeginTransaction()
    
    let insertUpdate id o (p:string) =
        let pattern = System.String.Format(p, o |> tableName, parameterPrefix store)
        let command = getCommand store pattern conn transaction
        command.Parameters.Add(getParameter store conn "id" id) |> ignore
        command.Parameters.Add(getParameter store conn "data" (serialize o store)) |> ignore
        command.ExecuteNonQuery()

    let insert (id:'a) (o:obj) = insertUpdate id o @"insert into ""{0}"" (id, data) values({1}id, {1}data)"
    let update (id:'a) (o:obj) = insertUpdate id o @"update ""{0}"" set data = {1}data where id = {1}id"

    let delete id o =
        let pattern = System.String.Format(@"delete from ""{0}"" where id = {1}id", o |> tableName, parameterPrefix store)
        let command = getCommand store pattern conn transaction
        command.Parameters.Add(getParameter store conn "id" id) |> ignore
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
            | _ -> 
                transaction.Rollback()
                reraise()
            transaction.Commit()
        finally
            conn.Close()
        ()

let select<'a> (store:Store) select (m:(string * 'c) list) : 'a array = 
    let ps = Map.ofList m
    use conn = getConnection store
    conn.Open()
    use transaction = conn.BeginTransaction()
    use command = getCommand store select conn transaction
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

let runScript (store:Store) (script:string) =
    use conn = getConnection store
    conn.Open()
    use transaction = conn.BeginTransaction()
    use command = getCommand store script conn transaction
    try
        try
            command.ExecuteNonQuery() |> ignore
        with
        | _ -> 
            transaction.Rollback()
            reraise()
        transaction.Commit()
    finally
        conn.Close()
    ()

let createTable (store:Store) (tableName:string) = 
    runScript store (System.String.Format("
IF (not EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' 
                 AND  TABLE_NAME = '{0}'))
BEGIN
CREATE TABLE [dbo].[{0}](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [xml] NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ( [Id] ASC )
)
END
", tableName))