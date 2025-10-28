using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PiholeDnsPropagate.Teleporter;

public interface ITeleporterArchiveProcessor
{
    Task<byte[]> ReplaceDnsRecordsAsync(
        Stream archiveStream,
        TeleporterDnsRecords dnsRecords,
        CancellationToken cancellationToken = default);
}
