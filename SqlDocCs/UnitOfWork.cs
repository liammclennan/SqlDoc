using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDocCs
{
    public class UnitOfWork
    {
        public static void Commit<TKey>(PostgresConnection connection, Queue<Operation<TKey>> uow)
        {
            PostgresDoc.commit(PostgresDoc.Store.NewPostgresStore(connection.String), QueueToFSharpList(uow));
        }

        public static void CommitSql<TKey>(SqlConnection connection, Queue<Operation<TKey>> uow)
        {
            PostgresDoc.commit(PostgresDoc.Store.NewSqlStore(connection.String), QueueToFSharpList(uow));
        }

        private static Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Operation<TKey>> QueueToFSharpList<TKey>(Queue<Operation<TKey>> uow)
        {
            if (uow.Count == 0)
            {
                return Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Operation<TKey>>.Empty;
            }
            return new Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Operation<TKey>>(
                OperationToOperation(uow.Dequeue()),
                QueueToFSharpList(uow));
        }

        private static PostgresDoc.Operation<TKey> OperationToOperation<TKey>(Operation<TKey> op)
        {
            switch (op.Verb)
            {
                case Verb.Insert:
                    return PostgresDoc.Operation<TKey>.NewInsert(Tuple.Create(op.Id, op.Datum));
                case Verb.Update:
                    return PostgresDoc.Operation<TKey>.NewUpdate(Tuple.Create(op.Id, op.Datum));
                case Verb.Delete:
                    return PostgresDoc.Operation<TKey>.NewDelete(Tuple.Create(op.Id, op.Datum));
                default:
                    throw new Exception("What kind of verb is " + op.Verb + "?");
            }
        }
    }
}
