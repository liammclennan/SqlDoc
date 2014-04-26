module Tests.Migration

open System.Configuration
open Xunit
open FsUnit.Xunit
open PostgresDoc.Doc
open PostgresDoc.Migration

let store = { connString = ConfigurationManager.AppSettings.["ConnString"] }

[<Fact>]
let ``when migrating with a single step`` () =
    System.Reflection.Assembly.GetExecutingAssembly() |> migrate store

    // it should execute the script
    // it should create a version table and insert the migration log


