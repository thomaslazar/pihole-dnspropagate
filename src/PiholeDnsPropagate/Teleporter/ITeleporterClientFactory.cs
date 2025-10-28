using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Teleporter;

public interface ITeleporterClientFactory
{
    ITeleporterClient CreateForPrimary(PrimaryPiHoleOptions options);

    ITeleporterClient CreateForSecondary(SecondaryPiHoleNodeOptions node);
}
