namespace Composed.Query.Tests.Utilities
{
    using System;
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

        public void Return(T result = default!) =>
            _tcs.SetResult(result);

        public void Throw(Exception? ex = null) =>
            _tcs.SetException(ex ?? new InvalidOperationException("QueryFunctionController induced exception."));

        public void Reset()
        {
            _tcs.TrySetCanceled();
            _tcs = new TaskCompletionSource<T>();
        }
    }
}
