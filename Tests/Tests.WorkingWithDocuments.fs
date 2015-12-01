module Tests.WorkingWithDocuments
open System.Configuration
open Xunit
open FsUnit.Xunit
open SqlDoc

[<CLIMutable>]
type Person = { _id: System.Guid; age: int; name: string }
type CustomerName(firstName, middleInitial, lastName) = 
    member this.FirstName = firstName
    member this.MiddleInitial = middleInitial
    member this.LastName = lastName

let store = PostgresStore ConfigurationManager.AppSettings.["ConnString"]
let storeSql = SqlStore ConfigurationManager.AppSettings.["ConnSql"]

[<Fact>]
let ``insert (sql)`` () =
    // insert
    let id = System.Guid.NewGuid() 
    let o = { _id = id; age = 45; name = "Cecile" }
    commit storeSql [insert id  o]

[<Fact>]
let ``insert, read, update, delete a document (sql)`` () =
    // insert
    let id = System.Guid.NewGuid() 
    let o = { _id = id; age = 45; name = "Cecile" }
    commit storeSql [insert id  o]

    // read
    let read = 
        [ "id", box id ] 
        |> select<Person> storeSql @"SELECT [Data] from Person 
Where Data.value('(/FsPickler/value/instance/id)[1]', 'uniqueidentifier') = @id"
    o |> should equal read.[0]
    Array.length read |> should equal 1

    // update
    let updated = {o with age = 46 }
    commit storeSql [update o._id updated]

    // read again :{P
    let readUpdated = 
        ["id", box id] 
        |> select<Person> storeSql "SELECT [Data] from Person where Id = @id"
    updated |> should equal readUpdated.[0]
    Array.length readUpdated |> should equal 1

    // delete
    commit storeSql [delete o._id o]
    let readDeleted = 
        ["id", box id] 
        |> select<Person> storeSql "select [Data] from Person where Id = @id"
    Array.length readDeleted |> should equal 0

[<Fact>]
let ``insert, read, update, delete a document`` () =
    // insert
    let id = System.Guid.NewGuid() 
    let o = { _id = id; age = 45; name = "Cecile" }
    commit store [insert id  o]

    // read
    let read = 
        [ "id", id |> string |> box ] 
        |> select<Person> store "select data from Person where (data->>'_id') = :id"
    o |> should equal read.[0]
    Array.length read |> should equal 1

    // update
    let updated = {o with age = 46 }
    commit store [update o._id updated]

    // read again :{P
    let readUpdated = 
        ["id", string id |> box] 
        |> select<Person> store "select data from Person where data->>'_id' = :id"
    updated |> should equal readUpdated.[0]
    Array.length readUpdated |> should equal 1

    // delete
    commit store [delete o._id o]
    let readDeleted = 
        ["id", string id |> box] 
        |> select<Person> store "select data from Person where data->>'_id' = :id"
    Array.length readDeleted |> should equal 0

[<Fact>]
let ``commit a unit of work`` () =
    let julio = { _id = System.Guid.NewGuid(); age = 30; name = "Julio" }
    let timmy = { _id = System.Guid.NewGuid(); age = 3; name = "Timmy" }
    let uow = [ 
        delete timmy._id timmy
        update julio._id { julio with age = 31 };
        insert julio._id julio;
        insert timmy._id timmy;
        ]
    commit store uow
    ()

//[<Fact>]
//let ``check perf`` () =
//   let uow = [
//       for i in [1..10000] do
//           let id = System.Guid.NewGuid()
//           yield insert id { _id = id ; age = i; name = "person" + i.ToString() }
//   ]
//   commit store uow
//
//[<Fact>]
//let ``check perf (sql)`` () =
//   let uow = [
//       for i in [1..10000] do
//           let id = System.Guid.NewGuid()
//           yield insert id { _id = id ; age = i; name = "person" + i.ToString() }
//   ]
//   commit storeSql uow
