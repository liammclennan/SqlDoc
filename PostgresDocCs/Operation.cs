using DesignByContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDocCs
{
    public enum Verb {
        Insert,Update,Delete
    }

    public class Operation
    {
        public Verb Verb { get; private set; }
        public object Datum { get; private set; }

        public Operation(Verb verb, object datum)
        {
            Dbc.Requires(datum != null);
            Verb = verb;
            Datum = datum;
        }
    }

    public class UnitOfWork
    {
        public static void Commit(string connString, Queue<Operation> uow)
        {
            PostgresDoc.Doc.commit(new PostgresDoc.Doc.Store(connString), QueueToFSharpList(uow));
        }

        private static Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation> QueueToFSharpList(Queue<Operation> uow)
        {
            if (uow.Count == 0) {
                return Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation>.Empty;
            }
            return new Microsoft.FSharp.Collections.FSharpList<PostgresDoc.Doc.Operation>(
                OperationToOperation(uow.Dequeue()), 
                QueueToFSharpList(uow));
        }

        private static PostgresDoc.Doc.Operation OperationToOperation(Operation op)
        {
            switch (op.Verb) {
                case Verb.Insert:
                    return PostgresDoc.Doc.Operation.NewInsert(op.Datum);
                case Verb.Update:
                    return PostgresDoc.Doc.Operation.NewUpdate(op.Datum);
                case Verb.Delete:
                    return PostgresDoc.Doc.Operation.NewDelete(op.Datum);
                default:
                    throw new Exception("What kind of verb is " + op.Verb + "?");
            }
        }

    }

    public class Query<T>
    {
        public static T[] For(string connString, string sql, Dictionary<string, object> parameters)
        {
            return PostgresDoc.Doc.query<T>(new PostgresDoc.Doc.Store(connString), sql, DictionaryToListOfTuples(parameters));
        }

        private static Microsoft.FSharp.Collections.FSharpList<Tuple<string, object>> DictionaryToListOfTuples(Dictionary<string, object> parameters)
        {
            if (parameters.Count == 0) return Microsoft.FSharp.Collections.FSharpList<Tuple<string, object>>.Empty;
            return new Microsoft.FSharp.Collections.FSharpList<Tuple<string, object>>(
                KvpToTuple(parameters.First()),
                DictionaryToListOfTuples(parameters.Skip(1).ToDictionary(k => k.Key, k => k.Value)));
        }

        private static Tuple<string, object> KvpToTuple(KeyValuePair<string, object> kvp)
        {
            return Tuple.Create<string, object>(kvp.Key, kvp.Value);
        }
    }
}
