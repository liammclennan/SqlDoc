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
    public class Person 
    {
        public Guid _id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] FavouriteThings { get; set; }
    }

    public class WorkingWithDocumentsTests
    {
        private Queue<Operation<Guid>> _uow;
        private Person _ernesto;
        private const string connString = "Server=127.0.0.1;Database=testo;Port=5432;User Id=liam;Password=password;";

        public WorkingWithDocumentsTests()
        {
            _uow = new Queue<Operation<Guid>>();
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
            Giv.n(() => AnOperation(Operation.Insert(_ernesto._id, _ernesto)));
            Wh.n(TheUnitOfWorkIsCommitted);
            Th.n(TheDocumentWasInserted);
        }

        private void AnOperation(Operation<Guid> operation)
        {
            _uow.Enqueue(operation);
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
            Assert.Equal(1, e.Length);
            Assert.Equal("Ernesto", e.First().Name);
        }
    }
}
