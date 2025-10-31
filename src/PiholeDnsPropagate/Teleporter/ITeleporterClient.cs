using System.Threading;
using System.Threading.Tasks;

namespace PiholeDnsPropagate.Teleporter;

public interface ITeleporterClient
{
    Task<byte[]> DownloadArchiveAsync(CancellationToken cancellationToken = default);

    Task UploadArchiveAsync(byte[] archiveContent, CancellationToken cancellationToken = default);
}
