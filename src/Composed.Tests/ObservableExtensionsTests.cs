namespace Composed.Tests
{
    using System;
    using System.Reactive;
    using System.Reactive.Subjects;
    using Moq;
    using Shouldly;
    using Xunit;
    using ObservableExtensions = Composed.ObservableExtensions;

    public class ObservableExtensionsTests
    {
        [Fact]
        public void AsDependency_NullArguments_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => ObservableExtensions.AsDependency<Unit>(null!));
        }

        [Fact]
        public void AsDependency_ForObservable_MapsObservable()
        {
            var subject = new Subject<int>();
            var dependency = subject.AsDependency();
            var subscribeMock = new Mock<Action<Unit>>();
            using var subscription = dependency.Subscribe(subscribeMock.Object);

            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(3);

            subscribeMock.Verify(fn => fn(It.IsAny<Unit>()), Times.Exactly(3));
        }
    }
}
