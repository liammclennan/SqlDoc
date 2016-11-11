using Givn;
using SqlDocCs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Xunit;

namespace TestsCs
{
    [Serializable]
    public class PersonCs 
    {
        public Guid _id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] FavouriteThings { get; set; }
        public override bool Equals(object obj)
        {
            var other = obj as PersonCs;
            return _id == other._id && Name == other.Name && Age == other.Age && FavouriteThings.Length == other.FavouriteThings.Length; // ignores FavouriteThings
        }
    }

    public class WorkingWithDocumentsTests
    {
        private Queue<Operation<Guid>> _uow;
        private PersonCs _ernesto;
        private IConnection connection = SqlConnection.From(ConfigurationManager.AppSettings["ConnSqlXml"]);

        public WorkingWithDocumentsTests()
        {
            _uow = new Queue<Operation<Guid>>();
        }

        [Fact]
        public void UnitOfWork_JustInsert()
        {
            _ernesto = new PersonCs
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

        [Fact]
        public void CanQueryAll() {
            var e = QueryFor<PersonCs>.For(
                connection,
                "select data from PersonCs");
            Assert.NotNull(e);
        }

        private void AnOperation(Operation<Guid> operation)
        {
            _uow.Enqueue(operation);
        }

        private void TheUnitOfWorkIsCommitted()
        {
            UnitOfWork.Commit(connection, _uow);
        }

        private void TheDocumentWasInserted()
        {
            var e = QueryFor<PersonCs>.For(
                connection, 
                "select data from PersonCs where Data.value('(/FsPickler/value/instance/idkBackingField)[1]', 'uniqueidentifier') = @id",
                new Dictionary<string, object> { {"id", _ernesto._id} });
            Assert.Equal(1, e.Length);
            Assert.Equal("Ernesto", e.First().Name);
        }
    }
}
