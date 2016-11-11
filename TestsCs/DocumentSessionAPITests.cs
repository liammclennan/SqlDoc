using Givn;
using SqlDocCs;
using System;
using System.Collections.Generic;
using System.Configuration;
using Xunit;

namespace TestsCs
{
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
            new DocumentSession<Guid>(SqlConnection.From(ConfigurationManager.AppSettings["ConnSqlXml"]));
        private PersonCs _aDocument;
    }
}
