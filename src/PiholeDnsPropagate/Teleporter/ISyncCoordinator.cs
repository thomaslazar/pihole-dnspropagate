using System.Threading;
using System.Threading.Tasks;

namespace PiholeDnsPropagate.Teleporter;

public interface ISyncCoordinator
{
    Task<SyncResult> SynchronizeAsync(bool dryRun, CancellationToken cancellationToken = default);
}
