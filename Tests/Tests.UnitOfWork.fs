module Tests.UnitOfWork
open System.Configuration
open Xunit
open FsUnit.Xunit
open PostgresDoc.Doc
open PostgresDoc.UnitOfWork
open Tests.Types

let store = { connString = ConfigurationManager.AppSettings.["ConnString"] }

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

let ``check perf`` () =
    let uow = [
        for i in [1..1000] do
            yield Insert { _id = System.Guid.NewGuid(); age = i; name = "person" + i.ToString() }
    ]
    commit store uow
