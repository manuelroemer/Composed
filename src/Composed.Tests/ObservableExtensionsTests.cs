namespace Composed.Tests
{
    using System;
    using System.Reactive.Subjects;
    using Shouldly;
    using Xunit;
    using ObservableExtensions = Composed.ObservableExtensions;

    public class ObservableExtensionsTests
    {
        #region ToDependency

        [Fact]
        public void ToDependency_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => ObservableExtensions.ToDependency<int>(null!));
        }

        [Fact]
        public void ToDependency_ReturnsDependencyMirroringOnNext()
        {
            var invocations = 0;
            var subject = new Subject<int>();
            var dependency = subject.ToDependency();
            dependency.Changed.Subscribe(_ => invocations++);
            subject.OnNext(1);
            subject.OnNext(2);
            invocations.ShouldBe(2);
        }

        [Fact]
        public void ToDependency_ReturnsDependencyMirroringOnError()
        {
            var wasOnErrorCalled = false;
            var thrownError = (Exception?)null;
            var ex = new Exception();
            var subject = new Subject<int>();
            var dependency = subject.ToDependency();
            dependency.Changed.Subscribe(_ => { }, ex => (wasOnErrorCalled, thrownError) = (true, ex));
            subject.OnError(ex);
            wasOnErrorCalled.ShouldBeTrue();
            thrownError.ShouldBeSameAs(ex);
        }
        
        [Fact]
        public void ToDependency_ReturnsDependencyMirroringOnCompleted()
        {
            var wasOnCompletedCalled = false;
            var subject = new Subject<int>();
            var dependency = subject.ToDependency();
            dependency.Changed.Subscribe(_ => { }, () => wasOnCompletedCalled = true);
            subject.OnCompleted();
            wasOnCompletedCalled.ShouldBeTrue();
        }

        #endregion
    }
}
