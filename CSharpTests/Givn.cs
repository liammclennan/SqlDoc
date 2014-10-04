using System;

namespace Givn
{
    public static class Giv
    {
        public static Ander n(Action action)
        {
            action();
            return new Ander();
        }
    }

    public class Ander
    {
        public Ander And(Action action)
        {
            action();
            return this;
        }
    }

    public static class Wh
    {
        public static Ander n(Action action)
        {
            action();
            return new Ander();
        }
    }

    public static class Th
    {
        public static Ander n(Action action)
        {
            action();
            return new Ander();
        }
    }
}
