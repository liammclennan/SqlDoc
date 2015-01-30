using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDocCs
{
    public class UnitOfWork
    {
        public static void Commit<TKey>(string connString, Queue<Operation<TKey>> uow)
        {
            PostgresDoc.Doc.commit(PostgresDoc.Doc.Store.PostgresStore.NewPostgresStore(connString), QueueToFSharpList(uow));
        }

        private static Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation<TKey>> QueueToFSharpList<TKey>(Queue<Operation<TKey>> uow)
        {
            if (uow.Count == 0)
            {
                return Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation<TKey>>.Empty;
            }
            return new Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation<TKey>>(
                OperationToOperation(uow.Dequeue()),
                QueueToFSharpList(uow));
        }

        private static PostgresDoc.Doc.Operation<TKey> OperationToOperation<TKey>(Operation<TKey> op)
        {
            switch (op.Verb)
            {
                case Verb.Insert:
                    return PostgresDoc.Doc.Operation<TKey>.NewInsert(Tuple.Create(op.Id, op.Datum));
                case Verb.Update:
                    return PostgresDoc.Doc.Operation<TKey>.NewUpdate(Tuple.Create(op.Id, op.Datum));
                case Verb.Delete:
                    return PostgresDoc.Doc.Operation<TKey>.NewDelete(Tuple.Create(op.Id, op.Datum));
                default:
                    throw new Exception("What kind of verb is " + op.Verb + "?");
            }
        }
    }
}
