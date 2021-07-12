namespace Composed.Query
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TimedQueryCacheInvalidator : QueryCacheInvalidator
    {
        private readonly TimeSpan _invalidationDelay;
        private Action? _resetCurrentInvalidationAttempt;

        public TimedQueryCacheInvalidator(TimeSpan invalidationDelay)
        {
            if (invalidationDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(invalidationDelay),
                    "The invalidation delay must be greater than (or equal to) TimeSpan.Zero."
                );
            }

            _invalidationDelay = invalidationDelay;
        }

        public override void OnQueryDeactivated(Func<bool> tryInvalidateQuery)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var cts = new CancellationTokenSource();
#pragma warning restore CA2000

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_invalidationDelay, cts.Token).ConfigureAwait(false);
                    tryInvalidateQuery();
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    // Mainly called to ensure that the CancellationTokenSource is disposed.
                    // At this point there is no running task which can throw due to the cancellation anymore.
                    _resetCurrentInvalidationAttempt?.Invoke();
                }
            });

            // This method shouldn't be called two successive times, but if that does happen,
            // reset a potentially ongoing invalidation attempt before starting the new one.
            _resetCurrentInvalidationAttempt?.Invoke();

            _resetCurrentInvalidationAttempt = () =>
            {
                cts.Cancel();
                cts.Dispose();
                _resetCurrentInvalidationAttempt = null;
            };
        }

        public override void OnQueryReactivated()
        {
            _resetCurrentInvalidationAttempt?.Invoke();
        }
    }
}
