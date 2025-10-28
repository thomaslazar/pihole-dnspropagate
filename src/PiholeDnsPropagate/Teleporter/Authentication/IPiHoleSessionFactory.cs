using System.Threading;
using System.Threading.Tasks;
using PiholeDnsPropagate.Teleporter;

namespace PiholeDnsPropagate.Teleporter.Authentication;

internal interface IPiHoleSessionFactory
{
    Task<PiHoleSession> CreateSessionAsync(TeleporterClientOptions options, CancellationToken cancellationToken = default);
}
