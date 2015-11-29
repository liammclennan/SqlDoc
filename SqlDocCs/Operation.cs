using DesignByContract;

namespace PostgresDocCs
{
    public enum Verb {
        Insert,Update,Delete
    }

    public interface IConnection
    {
        string String { get; }
    }

    public class SqlConnection : IConnection
    {
        public string String { get; private set; }

        public static SqlConnection From(string connString)
        {
            return new SqlConnection() { String = connString };
        }
    }

    public class PostgresConnection : IConnection
    {
        public string String { get; private set; }

        public static PostgresConnection From(string connString)
        {
            return new PostgresConnection() { String = connString };
        }
    }

    public class Operation<TKey>
    {
        public TKey Id { get; private set; }
        public Verb Verb { get; private set; }
        public object Datum { get; private set; }

        public Operation(TKey id, Verb verb, object datum)
        {
            Dbc.Requires(datum != null);
            Id = id;
            Verb = verb;
            Datum = datum;
        }
    }

    public static class Operation
    {
        public static Operation<TKey> Insert<TKey>(TKey id, object datum)
        {
            return new Operation<TKey>(id, Verb.Insert, datum);
        }

        public static Operation<TKey> Update<TKey>(TKey id, object datum)
        {
            return new Operation<TKey>(id, Verb.Update, datum);
        }

        public static Operation<TKey> Delete<TKey>(TKey id, object datum)
        {
            return new Operation<TKey>(id, Verb.Delete, datum);
        }
    }
}
