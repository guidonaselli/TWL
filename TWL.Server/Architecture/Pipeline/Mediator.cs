using System.Collections.Concurrent;

namespace TWL.Server.Architecture.Pipeline;

public class Mediator : IMediator
{
    private readonly ConcurrentDictionary<Type, object> _handlers = new();

    public async Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var type = command.GetType();
        if (_handlers.TryGetValue(type, out var handlerObj))
        {
            // Use dynamic dispatch to invoke the generic Handle method
            return await ((dynamic)handlerObj).Handle((dynamic)command, cancellationToken);
        }

        throw new InvalidOperationException($"No handler registered for {type.Name}");
    }

    public void Register<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler)
        where TCommand : ICommand<TResult> =>
        _handlers[typeof(TCommand)] = handler;
}