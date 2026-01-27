using System.Threading;
using System.Threading.Tasks;
using TWL.Server.Architecture.Pipeline;
using Xunit;

namespace TWL.Tests.Architecture;

public class MediatorTests
{
    public class TestCommand : ICommand<string> { public string Input { get; set; } }
    public class TestHandler : ICommandHandler<TestCommand, string>
    {
        public Task<string> Handle(TestCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult("Handled " + command.Input);
        }
    }

    [Fact]
    public async Task Mediator_ShouldDispatchToCorrectHandler()
    {
        var mediator = new Mediator();
        mediator.Register<TestCommand, string>(new TestHandler());

        var result = await mediator.Send(new TestCommand { Input = "Test" });

        Assert.Equal("Handled Test", result);
    }
}
