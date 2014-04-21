module Tests.WorkingWithDocuments
open System.Configuration
open Xunit
open FsUnit.Xunit
open PostgresDoc.Doc
open Tests.Types

[<Fact>]
let ``insert, read, update, delete a document`` () =
    let store = { connString = ConfigurationManager.AppSettings.["ConnString"] }

    // insert
    let id = System.Guid.NewGuid() 
    let o = { _id = id; age = 45; name = "Cecile" }
    Seq.singleton o |> Seq.cast |> insert store

    // read
    let read = 
        [ "id", box (id.ToString()) ] 
        |> Map.ofList
        |> query<Person> store "select data from people where data->>'_id' = :id"
    o |> should equal read.[0]
    Array.length read |> should equal 1

    // update
    let updated = {o with age = 46 }
    update store updated

    // read again :{P
    let readUpdated = 
        ["id", box (id.ToString())] 
        |> Map.ofList 
        |> query<Person> store "select data from people where data->>'_id' = :id"
    updated |> should equal readUpdated.[0]
    Array.length readUpdated |> should equal 1

    // delete
    delete store o
    let readDeleted = 
        ["id", box (id.ToString())] 
        |> Map.ofList 
        |> query<Person> store "select data from people where data->>'_id' = :id"
    Array.length readDeleted |> should equal 0