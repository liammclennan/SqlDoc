PostgresDoc
===========

Unit of work + document database on Postgresql

Unit of Work API
----------------

    type Person = 
        { _id: System.Guid; age: int; name: string }
        interface IDocument with
            member x.tableName() = "People"
            member x.id() = x._id

    let store = { connString = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=*****;Database=testo;" }

    let julio = { _id = System.Guid.NewGuid(); age = 30; name = "Julio" }
    let timmy = { _id = System.Guid.NewGuid(); age = 3; name = "Timmy" }
    
    // list is backwards so operations can be consed
    let uow = [ 
        Delete timmy
        Update { julio with age = 31 };
        Insert julio;
        Insert timmy;
        ]
    commit store uow

Document API
------------

### Query

    let peopleWhoAreThirty = 
        [ "age", box (30) ] 
        |> Map.ofList
        |> query<Person> store "select data from people where data->>'age' = :age"
