# Teleporter Archive Test Fixtures

The unit tests under `tests/PiholeDnsPropagate.Tests/Teleporter/` use the `TeleporterArchiveBuilder` fixture to generate deterministic Teleporter archives without hitting a live Pi-hole instance. The builder produces archives that mirror the structure returned by the `/api/teleporter` endpoint (`etc/pihole/pihole.toml`, `etc/hosts`, `etc/pihole/dhcp.leases`, etc.) and preserves metadata such as last-write timestamps.

## Usage

```csharp
var archive = new TeleporterArchiveBuilder()
    .WithToml("""
[dns]
hosts = ["192.168.1.1 example.local"]
cnameRecords = ["alias.example.local,example.local"]
""")
    .WithHosts("127.0.0.1 localhost")
    .WithDhcpLeases("# sample")
    .Build();
```

The resulting `byte[]` can be passed to `TeleporterArchiveProcessor.ReplaceDnsRecordsAsync` or opened with `ZipArchive` for assertions. Additional helpers (`WithFile`, `WithHosts`, `WithDhcpLeases`) allow fine-grained control over entry contents and timestamps so tests can verify metadata preservation.

## Integration Tests

When higher-fidelity verification is required, run the sandbox-backed tests in `TeleporterSandboxTests` (marked `[Explicit]`) after starting the Pi-hole sandbox (`deploy/pihole-sandbox/sandbox.sh up`). These tests download archives from the secondary Pi-hole container, process them with the archive processor, and confirm they import successfully back into Pi-hole.
