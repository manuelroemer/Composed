namespace Composed.Query.Tests.Utilities
{
    using System;
    using System.Threading.Tasks;

    public static class ObservableExtensions
    {
        public static Task InvokeAndWaitNext<T>(this IObservable<T> observable, Action action, int timeoutMs = 5000)
        {
            var task = observable.WaitNext(timeoutMs);
            action();
            return task;
        }

        public static Task WaitNext<T>(this IObservable<T> observable, int timeoutMs = 5000) =>
            observable.WaitNext(TimeSpan.FromMilliseconds(timeoutMs));

        public static Task WaitNext<T>(this IObservable<T> observable, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource();
            var sub = observable.Subscribe(_ => tcs.TrySetResult());
            var timeoutTask = Task.Delay(timeout).ContinueWith(_ => tcs.TrySetCanceled(), TaskScheduler.Current);
            return tcs.Task;
        }
    }
}
