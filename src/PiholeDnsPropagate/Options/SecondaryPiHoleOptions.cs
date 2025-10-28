using System;
using System.Collections.Generic;

namespace PiholeDnsPropagate.Options;

public sealed class SecondaryPiHoleOptions
{
    public IList<SecondaryPiHoleNodeOptions> Nodes { get; init; } = new List<SecondaryPiHoleNodeOptions>();
}

public sealed class SecondaryPiHoleNodeOptions
{
    public string Name { get; set; } = string.Empty;
    public Uri? BaseUrl { get; set; }
    public string Password { get; set; } = string.Empty;
}
