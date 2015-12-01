using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlDocCs
{
    public interface IQuerySession<TKey>
    {
        TVal Load<TVal>(TKey id) where TVal : class;
        IEnumerable<TVal> Query<TVal>(string sql, Dictionary<string, object> parameters = null);
    }

    /// <summary>
    ///     Interface for document session
    /// </summary>
    public interface IDocumentSession<TKey> : IQuerySession<TKey>
    {
        void Delete(TKey id, object datum);
        void SaveChanges();
        void Store<TVal>(TKey id, TVal entity) where TVal : class;
        void Update<TVal>(TKey id, TVal entity) where TVal : class;
    }

    public class DocumentSession<TKey> : IDocumentSession<TKey>
    {
        private IConnection _connection;
        private Queue<Operation<TKey>> _uow = new Queue<Operation<TKey>>();

        public DocumentSession(IConnection connection) 
        {
            _connection = connection;
        }

        public void Delete(TKey id, object datum)
        {
            _uow.Enqueue(Operation.Delete(id, datum));
        }

        public TVal Load<TVal>(TKey id) where TVal : class
        {
            var result = QueryFor<TVal>.For(_connection,
                string.Format("SELECT [Data] from {0} where Id = @id", SqlDoc.tableName<TVal>()),
                new Dictionary<string, object> { { "id", id } });
            return result.First();
        }

        public IEnumerable<TVal> Query<TVal>(string sql, Dictionary<string, object> parameters = null)
        {
            return QueryFor<TVal>.For(_connection, sql, parameters);
        }

        public void SaveChanges()
        {
            UnitOfWork.Commit(_connection, _uow);
        }

        public void Store<TVal>(TKey id, TVal entity) where TVal : class
        {
            _uow.Enqueue(Operation.Insert(id, entity));
        }

        public void Update<TVal>(TKey id, TVal entity) where TVal : class
        {
            _uow.Enqueue(Operation.Update(id, entity));
        }
    }
}
