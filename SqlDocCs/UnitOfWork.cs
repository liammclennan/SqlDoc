using System;
using System.Collections.Generic;

namespace SqlDocCs
{
    public class UnitOfWork
    {
        public static void Commit<TKey>(IConnection connection, Queue<Operation<TKey>> uow)
        {
            var store = ConnectionToStore(connection);
            SqlDoc.commit(store, QueueToFSharpList(uow));
        }

        public static SqlDoc.Store ConnectionToStore(IConnection connection)
        {
            if (connection is PostgresConnection)
            {
                return SqlDoc.Store.NewPostgresStore(connection.String);
            }
            if (connection is SqlConnection)
            {
                return SqlDoc.Store.NewSqlXmlStore(connection.String);
            }
            throw new ArgumentException("Unknown IConnection type");
        }

        private static Microsoft.FSharp.Collections.FSharpList<SqlDoc.Operation<TKey>> QueueToFSharpList<TKey>(Queue<Operation<TKey>> uow)
        {
            if (uow.Count == 0)
            {
                return Microsoft.FSharp.Collections.FSharpList<SqlDoc.Operation<TKey>>.Empty;
            }
            return new Microsoft.FSharp.Collections.FSharpList<SqlDoc.Operation<TKey>>(
                OperationToOperation(uow.Dequeue()),
                QueueToFSharpList(uow));
        }

        private static SqlDoc.Operation<TKey> OperationToOperation<TKey>(Operation<TKey> op)
        {
            switch (op.Verb)
            {
                case Verb.Insert:
                    return SqlDoc.Operation<TKey>.NewInsert(Tuple.Create(op.Id, op.Datum));
                case Verb.Update:
                    return SqlDoc.Operation<TKey>.NewUpdate(Tuple.Create(op.Id, op.Datum));
                case Verb.Delete:
                    return SqlDoc.Operation<TKey>.NewDelete(Tuple.Create(op.Id, op.Datum));
                default:
                    throw new Exception("What kind of verb is " + op.Verb + "?");
            }
        }
    }
}
