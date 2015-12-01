using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlDocCs
{
    public class QueryFor<T>
    {
        public static T[] For(IConnection connection, string sql, Dictionary<string, object> parameters = null)
        {
            var store = UnitOfWork.ConnectionToStore(connection);
            return For(store, sql, parameters);
        }

        private static T[] For(SqlDoc.Store store, string sql, Dictionary<string, object> parameters = null)
        {
            parameters = parameters ?? new Dictionary<string, object>();
            return SqlDoc.select<T>(store, sql, DictionaryToListOfTuples(parameters));
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
            return Tuple.Create(kvp.Key, kvp.Value);
        }
    }
}
