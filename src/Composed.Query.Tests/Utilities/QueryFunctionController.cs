namespace Composed.Query.Tests.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Composed.Query;
    using Moq;

    public sealed class QueryFunctionController<T>
    {
        private TaskCompletionSource<T> _tcs = new();

        public Mock<QueryFunction<T>> FunctionMock { get; }

        public QueryFunction<T> Function => FunctionMock.Object;

        public QueryFunctionController()
        {
            FunctionMock = new Mock<QueryFunction<T>>();
            FunctionMock.Setup(fn => fn()).Returns(() => _tcs.Task);
        }

        public Task ReturnAndWaitForStateChange(Query<T> query, T result = default!, int timeoutMs = 5000) =>
            ReturnAndWaitForStateChange(new[] { query }, result, timeoutMs);

        public Task ReturnAndWaitForStateChange(IEnumerable<Query<T>> queries, T result = default!, int timeoutMs = 5000)
        {
            var task = Task.WhenAll(queries.Select(query => query.State.WaitNext(timeoutMs)));
            Return(result);
            return task;
        }

        public Task ThrowAndWaitForStateChange(Query<T> query, Exception? ex = default!, int timeoutMs = 5000) =>
            ThrowAndWaitForStateChange(new[] { query }, ex, timeoutMs);

        public Task ThrowAndWaitForStateChange(IEnumerable<Query<T>> queries, Exception? ex = default!, int timeoutMs = 5000)
        {
            var task = Task.WhenAll(queries.Select(query => query.State.WaitNext(timeoutMs)));
            Throw(ex);
            return task;
        }

        public void Return(T result = default!) =>
            _tcs.SetResult(result);

        public void Throw(Exception? ex = null) =>
            _tcs.SetException(ex ?? new InvalidOperationException("QueryFunctionController induced exception."));

        public void Reset()
        {
            _tcs.TrySetCanceled();
            _tcs = new TaskCompletionSource<T>();
        }

        public void Verify(int queryFunctionCallCount) =>
            FunctionMock.Verify(fn => fn(), Times.Exactly(queryFunctionCallCount));
    }
}
