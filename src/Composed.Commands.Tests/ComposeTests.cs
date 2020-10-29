namespace Composed.Commands.Tests
{
    using System;
    using Shouldly;
    using Xunit;
    using static Composed.Compose;
    using static Composed.Commands.Compose;
    using System.Windows.Input;

    public class ComposeTests
    {
        [Fact]
        public void UseCommand_Sync_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => UseCommand((ExecuteAction)null!));
            Should.Throw<ArgumentNullException>(() => UseCommand((ExecuteAction)null!, () => true));
            Should.Throw<ArgumentNullException>(() => UseCommand(() => { }, canExecute: null!));
            Should.Throw<ArgumentNullException>(() => UseCommand<object>((ExecuteAction<object>)null!));
            Should.Throw<ArgumentNullException>(() => UseCommand<object>((ExecuteAction<object>)null!, _ => true));
            Should.Throw<ArgumentNullException>(() => UseCommand<object>(_ => { }, canExecute: null!));
        }

        public static TheoryData<IComposedCommand<object?>> NonExecutableCommands => new()
        {
            UseCommand(() => { }, () => false),
            UseCommand<object?>(_ => { }, _ => false),
        };

        [Theory, MemberData(nameof(NonExecutableCommands))]
        public void UseCommand_Sync_ThrowsInvalidOperationExceptionWhenExecutionIsBlocked(IComposedCommand<object?> command)
        {
            Should.Throw<InvalidOperationException>(() => command.Execute());
            Should.Throw<InvalidOperationException>(() => command.Execute(null));
            Should.Throw<InvalidOperationException>(() => ((ICommand)command).Execute(null));
        }

        [Fact]
        public void UseCommand_Sync_RaisesCanExecuteChangedWhenDependencyChanges()
        {
            var wasCanExecuteChangedRaised = false;
            var canExecuteRef = Ref(false);
            var command = UseCommand(() => { }, () => canExecuteRef.Value, canExecuteRef);
            command.CanExecuteChanged += (s, e) => wasCanExecuteChangedRaised = true;
            canExecuteRef.Value = true;
            wasCanExecuteChangedRaised.ShouldBeTrue();
        }
    }
}
