namespace Composed.Query
{
    using System;

    public abstract class QueryCacheInvalidator
    {
        public abstract void OnQueryDeactivated(Func<bool> tryInvalidateQuery);

        public abstract void OnQueryReactivated();
    }
}
