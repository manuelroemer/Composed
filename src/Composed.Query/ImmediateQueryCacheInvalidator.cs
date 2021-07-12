namespace Composed.Query
{
    using System;

    public sealed class ImmediateQueryCacheInvalidator : QueryCacheInvalidator
    {
        public override void OnQueryDeactivated(Func<bool> tryInvalidateQuery)
        {
            _ = tryInvalidateQuery ?? throw new ArgumentNullException(nameof(tryInvalidateQuery));
            tryInvalidateQuery();
        }

        public override void OnQueryReactivated()
        {
        }
    }
}
