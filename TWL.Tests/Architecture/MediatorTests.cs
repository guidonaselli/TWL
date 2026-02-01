using TWL.Server.Architecture.Pipeline;

namespace TWL.Tests.Architecture;

public class MediatorTests
{
    [Fact]
    public async Task Mediator_ShouldDispatchToCorrectHandler()
    {
        var mediator = new Mediator();
        mediator.Register(new TestHandler());

        var result = await mediator.Send(new TestCommand { Input = "Test" });

        Assert.Equal("Handled Test", result);
    }

    public class TestCommand : ICommand<string>
    {
        public string Input { get; set; }
    }

    public class TestHandler : ICommandHandler<TestCommand, string>
    {
        public Task<string> Handle(TestCommand command, CancellationToken cancellationToken) =>
            Task.FromResult("Handled " + command.Input);
    }
}