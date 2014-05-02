module PostgresDoc.Migration

open System.IO
open Npgsql
open PostgresDoc.Doc

let private runMigration store (name, migration) =
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let transaction = conn.BeginTransaction()
    let migrationCommand = new NpgsqlCommand(migration, conn)
    let saveVersionCommand = new NpgsqlCommand("insert into schema_versions values (:name,:whn)", conn)
    saveVersionCommand.Parameters.Add(new NpgsqlParameter(ParameterName = "name", Value = name)) |> ignore
    saveVersionCommand.Parameters.Add(new NpgsqlParameter(ParameterName = "whn", Value = System.DateTime.Now)) |> ignore
    try
        try
            migrationCommand.ExecuteNonQuery() |> ignore
            saveVersionCommand.ExecuteNonQuery() |> ignore
        with
        | :? NpgsqlException -> 
            transaction.Rollback()
            reraise()
        transaction.Commit()
    finally
        conn.Close()

let private migrateGreaterThan store (assemblyContainingMigrations:System.Reflection.Assembly) latest =
    let migrationNames = assemblyContainingMigrations.GetManifestResourceNames()
                            |> Array.filter (fun name -> name.EndsWith(".sql"))
                            |> Array.sortBy (fun name -> name)
    let migrations = migrationNames 
                        |> Array.map (fun name -> name, assemblyContainingMigrations.GetManifestResourceStream(name))
                        |> Array.map (fun (name, stream) -> name, (new StreamReader(stream)).ReadToEnd())
                        |> Array.filter (fun (name,stream) -> name > latest)
    for (n,m) in migrations do
        runMigration store (n,m)
    ()

let migrate store (assemblyContainingMigrations:System.Reflection.Assembly) =
    use conn = new NpgsqlConnection(store.connString)
    conn.Open()
    let initCommand = new NpgsqlCommand("create table schema_versions (
                        migration varchar(500),
                        whn timestamp
                      );", conn);
    try
        try
            initCommand.ExecuteNonQuery() |> ignore
        with
            | :? NpgsqlException -> ()
    finally
        conn.Close()

    use selectConn = new NpgsqlConnection(store.connString)
    selectConn.Open()
    let command = new NpgsqlCommand("select migration from schema_versions order by migration desc limit 1", selectConn)
    let latestVersion = 
        try
            command.ExecuteScalar() :?> string
        finally
            selectConn.Close()
    migrateGreaterThan store assemblyContainingMigrations latestVersion