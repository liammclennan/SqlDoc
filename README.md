SqlDoc
===========

SqlDoc is a unit of work + document database on Postgresql (JSON) and [Sql Server](https://github.com/liammclennan/SqlDoc/wiki/SQL-Server-Support) (XML). There are [many reasons why Postgres makes a good document store](http://withouttheloop.com/articles/2014-09-30-postgresql-nosql/) including speed, stability, ecosystem, ACID transactions, mixing with relational data and joins.

As your program runs, record a series of data updates (the unit of work). At the end of the unit of work persist all the changes in a transaction. Changes can be inserts, updates or deletes. PostgresDoc also provides a querying API. 

PostgresDoc is written in F# but provides APIs for F# and C#. The C# version simply translates to the F# API. 

Unit of Work API
----------------

### CSharp

	public class Person 
    {
        public Guid _id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] FavouriteThings { get; set; }
    }

	var ernesto = new Person
            {
                _id = Guid.NewGuid(),
                Name = "Ernesto",
                Age = 31,
                FavouriteThings = new[] { "Pistachio Ice Cream", "Postgresql", "F#" }
            };
	var connString = "Server=127.0.0.1;Port=5432;User Id=******;Password=*****;Database=testo;";

	var unitOfWork = new Queue<Operation<Guid>>();
	
	// insert a document
	unitOfWork.Enqueue(Operation.Insert(ernesto._id, ernesto));
	
	// modify a document
	ernesto.Age = 32;
	unitOfWork.Enqueue(Do.Update(ernesto._id, ernesto));
	
	// persist the changes in a transaction
	UnitOfWork.Commit(connString, unitOfWork)

#### Querying

	var ernesto = Query<Person>.For(
                connString, 
                "select data from Person where id = :id", 
                new Dictionary<string, object> { {"id", ernesto._id} });

### FSharp

    type Person = 
        { _id: System.Guid; age: int; name: string }

    let store = { connString = "Server=127.0.0.1;Port=5432;User Id=*******;Password=*****;Database=testo;" }

    let julio = { _id = System.Guid.NewGuid(); age = 30; name = "Julio" }
    let timmy = { _id = System.Guid.NewGuid(); age = 3; name = "Timmy" }
    
	// newer operations are prepended
    let uow = [ 
        delete timmy._id timmy;
        update julio._id { julio with age = 31 };
        insert julio._id julio;
        insert timmy._id timmy;
        ]
    commit store uow

#### Querying

    let peopleWhoAreThirty = 
        [ "age", box (30) ] 
        |> query<Person> store "select data from people where data->>'age' = :age"

Expected Schema
---------------

The database table should have the same name as the type, an `id` column matching the type used for identifiers, and a json or jsonb `data` column. The table name should be lowercase. 

In the example above I have used `Guid` (`uuid`) identifiers and a type called `Person` so:
 
	create table "person" ( 
		id uuid NOT NULL PRIMARY KEY,
		data json NOT NULL 
	);
