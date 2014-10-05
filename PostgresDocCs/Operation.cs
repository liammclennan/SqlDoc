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
