SqlDoc
===========

SqlDoc is a unit of work + document database on Postgresql (JSON) and [Sql Server](https://github.com/liammclennan/SqlDoc/wiki/SQL-Server-Support) (XML). There are [many reasons why Postgres makes a good document store](http://withouttheloop.com/articles/2014-09-30-postgresql-nosql/) including speed, stability, ecosystem, ACID transactions, mixing with relational data and joins.

As your program runs, record a series of data updates (the unit of work). At the end of the unit of work persist all the changes in a transaction. Changes can be inserts, updates or deletes. PostgresDoc also provides a querying API. 

PostgresDoc is written in F# but provides APIs for F# and C#. The C# version simply translates to the F# API. 

Unit of Work API
----------------

### CSharp

The CSharp API uses a variation of the IDocumentSession API from RavenDB and Marten. 

	public class DocumentSessionAPITests
    {
        [Fact]
        public void ICanAddADocumentAndReadItBack()
        {
            Giv.n(IAddADocument);
            Th.n(ICanReadItBack);
        }

        [Fact]
        public void ICanAddADocumentAndDeleteItAndItsGone()
        {
            Wh.n(IAddADocument)
                .And(IDeleteTheDocument);
            Th.n(TheDocumentIsGone);
        }

        [Fact]
        public void ICanAddADocumentAndUpdateItAndTheChangesPersist()
        {
            Wh.n(IAddADocument)
                .And(IUpdateTheDocument);
            Th.n(TheChangePersists);
        }

        private void IAddADocument()
        {
            _aDocument = new PersonCs
            {
                _id = Guid.NewGuid(),
                Name = "Docsesh",
                Age = 90,
                FavouriteThings = new[] { "Golf", "Statue of liberty" }
            };
            _documentSession.Store(_aDocument._id, _aDocument);
            _documentSession.SaveChanges();
        }

        private void ICanReadItBack()
        {
            var fresh = _documentSession.Load<PersonCs>(_aDocument._id);
            Assert.True(_aDocument.Equals(fresh));
        }

        private void IUpdateTheDocument()
        {
            _aDocument.Age += 1;
            _documentSession.Update(_aDocument._id, _aDocument);
            _documentSession.SaveChanges();
        }

        private void TheChangePersists()
        {
            var fresh = _documentSession.Load<PersonCs>(_aDocument._id);
            Assert.Equal(91, fresh.Age);
        }

        private void IDeleteTheDocument()
        {
            _documentSession.Delete(_aDocument._id, _aDocument);
            _documentSession.SaveChanges();
        }

        private void TheDocumentIsGone()
        {
            var result = _documentSession.Query<PersonCs>(
                "select data from PersonCs where Data.value('(/FsPickler/value/instance/idkBackingField)[1]', 'uniqueidentifier') = @id",
                new Dictionary<string, object> { { "id", _aDocument._id } });
            Assert.Empty(result);
        }

        private IDocumentSession<Guid> _documentSession =
            new DocumentSession<Guid>(SqlConnection.From(ConfigurationManager.AppSettings["ConnSql"]));
        private PersonCs _aDocument;
    }

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
        |> select<Person> store "select data from people where data->>'age' = :age"

Expected Schema
---------------

The database table should have the same name as the type, an `id` column matching the type used for identifiers, and a json or jsonb `data` column. The table name should be lowercase. 

In the example above I have used `Guid` (`uuid`) identifiers and a type called `Person` so:
 
### Postgres

```
create table "person" ( 
	id uuid NOT NULL PRIMARY KEY,
	data json NOT NULL 
);
```

### Sql Server (xml)

```
CREATE TABLE [dbo].[person](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [xml] NOT NULL,
 CONSTRAINT [PK_person] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
```

### Sql Server (json)

```
CREATE TABLE [dbo].[person](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_person] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
```

Development Instructions
======================

1. [Install Paket](https://fsprojects.github.io/Paket/installation.html)
1. Build
1. Create Postgres, old Sql Server and Sql Server >= 2016 databases matching the connection strings in `Tests/app.config`.
1. Run the tests