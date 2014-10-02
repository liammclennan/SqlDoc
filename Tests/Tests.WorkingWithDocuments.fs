module Tests.WorkingWithDocuments
open System.Configuration
open Xunit
open FsUnit.Xunit
open PostgresDoc.Doc
open Tests.Types

let store = { connString = ConfigurationManager.AppSettings.["ConnString"] }

[<Fact>]
let ``insert, read, update, delete a document`` () =
    // insert
    let id = System.Guid.NewGuid() 
    let o = { _id = id; age = 45; name = "Cecile" }
    commit store [Insert o]

    // read
    let read = 
        [ "id", box (id.ToString()) ] 
        |> query<Person> store "select data from Person where data->>'_id' = :id"
    o |> should equal read.[0]
    Array.length read |> should equal 1

    // update
    let updated = {o with age = 46 }
    commit store [Update updated]

    // read again :{P
    let readUpdated = 
        ["id", box (id.ToString())] 
        |> query<Person> store "select data from Person where data->>'_id' = :id"
    updated |> should equal readUpdated.[0]
    Array.length readUpdated |> should equal 1

    // delete
    commit store [Delete o]
    let readDeleted = 
        ["id", box (id.ToString())] 
        |> query<Person> store "select data from Person where data->>'_id' = :id"
    Array.length readDeleted |> should equal 0

[<Fact>]
let ``commit a unit of work`` () =
    let julio = { _id = System.Guid.NewGuid(); age = 30; name = "Julio" }
    let timmy = { _id = System.Guid.NewGuid(); age = 3; name = "Timmy" }
    let uow = [ 
        Delete timmy
        Update { julio with age = 31 };
        Insert julio;
        Insert timmy;
        ]
    commit store uow
    ()

[<Fact>]
let ``check perf`` () =
   let uow = [
       for i in [1..100000] do
           yield Insert { _id = System.Guid.NewGuid(); age = i; name = "person" + i.ToString() }
   ]
   commit store uow
