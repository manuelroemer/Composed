//namespace Composed.Query
//{
//    using System;
//    using System.Reactive.Concurrency;
//    using System.Threading.Tasks;
//    using static Composed.Compose;

//    public static class Compose
//    {
//        public static IDisposable UseDataFetchedHandler<T>(Query<T> query, Action<T> onDataFetched) =>
//            UseDataFetchedHandler(query, scheduler: null, onDataFetched);

//        public static IDisposable UseDataFetchedHandler<T>(Query<T> query, IScheduler? scheduler, Action<T> onDataFetched)
//        {
//            _ = query ?? throw new ArgumentNullException(nameof(query));

//            return WatchEffect(() =>
//            {
//                if (!query.IsLoading.Value)
//                {
//                    onDataFetched(query.Data.Value!);
//                }
//            }, scheduler, query.IsLoading, query.Data);
//        }

//        public static IDisposable UseDataFetchedHandler<T>(Query<T> query, Func<T, Task> onDataFetched) =>
//            UseDataFetchedHandler(query, scheduler: null, onDataFetched);

//        public static IDisposable UseDataFetchedHandler<T>(Query<T> query, IScheduler? scheduler, Func<T, Task> onDataFetched)
//        {
//            _ = query ?? throw new ArgumentNullException(nameof(query));

//            return WatchEffect(async () =>
//            {
//                if (!query.IsLoading.Value)
//                {
//                    await onDataFetched(query.Data.Value!).ConfigureAwait(false);
//                }
//            }, scheduler, query.IsLoading, query.Data);
//        }

//        public static IDisposable UseErrorHandler<T>(Query<T> query, Action<Exception> onError) =>
//            UseErrorHandler(query, scheduler: null, onError);

//        public static IDisposable UseErrorHandler<T>(Query<T> query, IScheduler? scheduler, Action<Exception> onError)
//        {
//            _ = query ?? throw new ArgumentNullException(nameof(query));
//            _ = onError ?? throw new ArgumentNullException(nameof(onError));

//            return WatchEffect(() =>
//            {
//                if (query.Error.Value is not null)
//                {
//                    onError(query.Error.Value);
//                }
//            }, scheduler, query.Error);
//        }

//        public static IDisposable UseErrorHandler<T>(Query<T> query, Func<Exception, Task> onError) =>
//            UseErrorHandler(query, scheduler: null, onError);

//        public static IDisposable UseErrorHandler<T>(Query<T> query, IScheduler? scheduler, Func<Exception, Task> onError)
//        {
//            _ = query ?? throw new ArgumentNullException(nameof(query));
//            _ = onError ?? throw new ArgumentNullException(nameof(onError));

//            return WatchEffect(async () =>
//            {
//                if (query.Error.Value is not null)
//                {
//                    await onError(query.Error.Value).ConfigureAwait(false);
//                }
//            }, scheduler, query.Error);
//        }
//    }
//}
