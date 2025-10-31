using System;
using System.Threading;
using System.Threading.Tasks;
using PiholeDnsPropagate.Teleporter;

namespace PiholeDnsPropagate.Tests.Teleporter.Fixtures;

internal sealed class FakeTeleporterClient : ITeleporterClient, IDisposable
{
    private readonly byte[] _downloadPayload;
    private readonly bool _throwOnUpload;

    public byte[]? UploadedArchive { get; private set; }
    public bool UploadInvoked => UploadedArchive is not null;

    public FakeTeleporterClient(byte[] downloadPayload, bool throwOnUpload = false)
    {
        _downloadPayload = downloadPayload;
        _throwOnUpload = throwOnUpload;
    }

    public Task<byte[]> DownloadArchiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_downloadPayload);

    public Task UploadArchiveAsync(byte[] archiveContent, CancellationToken cancellationToken = default)
    {
        if (_throwOnUpload)
        {
            throw new InvalidOperationException("Upload failure");
        }

        UploadedArchive = archiveContent;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
