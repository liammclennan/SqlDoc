using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresDocCs
{
    public class Query<T>
    {
        public static T[] For(PostgresConnection connection, string sql, Dictionary<string, object> parameters = null)
        {
            return For(PostgresDoc.Store.NewPostgresStore(connection.String), sql, parameters);
        }

        public static T[] For(SqlConnection connection, string sql, Dictionary<string, object> parameters = null)
        {
            return For(PostgresDoc.Store.NewSqlStore(connection.String), sql, parameters);
        }

        private static T[] For(PostgresDoc.Store store, string sql, Dictionary<string, object> parameters = null)
        {
            parameters = parameters ?? new Dictionary<string, object>();
            return PostgresDoc.select<T>(store, sql, DictionaryToListOfTuples(parameters));
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
