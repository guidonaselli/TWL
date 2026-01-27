using System.Threading;
using System.Threading.Tasks;

namespace TWL.Server.Architecture.Pipeline;

public interface IMediator
{
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
