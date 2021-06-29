namespace Composed.Query.Tests.Utilities
{
    using System;
    using System.Threading.Tasks;

    public static class ObservableExtensions
    {
        public static Task WaitNext<T>(this IObservable<T> observable, int timeoutMs = 5000) =>
            observable.WaitNext(TimeSpan.FromMilliseconds(timeoutMs));

        public static Task WaitNext<T>(this IObservable<T> observable, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource();
            var sub = observable.Subscribe(_ => tcs.SetResult());
            var timeoutTask = Task.Delay(timeout).ContinueWith(_ => tcs.TrySetCanceled(), TaskScheduler.Current);
            return tcs.Task;
        }
    }
}
