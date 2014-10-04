using Givn;
using PostgresDocCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CSharpTests
{
    public class Person : IPDDocument
    {
        public Guid _id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] FavouriteThings { get; set; }
    }

    public class WorkingWithDocuments
    {
        private Queue<Operation> _uow;
        private Person _ernesto;
        private const string connString = "Server=127.0.0.1;Database=testo;Port=5432;User Id=liam;Password=password;";

        public WorkingWithDocuments()
        {
            _uow = new Queue<Operation>();
        }

        [Fact]
        public void UnitOfWork_JustInsert()
        {
            _ernesto = new Person
            {
                _id = Guid.NewGuid(),
                Name = "Ernesto",
                Age = 31,
                FavouriteThings = new[] { "Pistachio Ice Cream", "Postgresql", "F#" }
            };
            Giv.n(() => AnInsert(_ernesto));
            Wh.n(TheUnitOfWorkIsCommitted);
            Th.n(TheDocumentWasInserted);
        }

        private void AnInsert(Person person)
        {
            _uow.Enqueue(new Operation(Verb.Insert, person));
        }

        private void TheUnitOfWorkIsCommitted()
        {
            UnitOfWork.Commit(connString, _uow);
        }

        private void TheDocumentWasInserted()
        {
            var e = Query<Person>.For(
                connString, 
                "select data from Person where data->>'_id' = :id", 
                new Dictionary<string, object> { {"id", _ernesto._id} });
            Assert.Same(1, e.Length);
            Assert.Same("Ernesto", e.First().Name);
        }
    }
}
