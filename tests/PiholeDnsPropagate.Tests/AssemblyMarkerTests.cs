using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using PiholeDnsPropagate;

namespace PiholeDnsPropagate.Tests;

[TestFixture]
[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Test classes are public for NUnit discovery.")]
public sealed class AssemblyMarkerTests
{
    [Test]
    public void AssemblyMarkerCanBeConstructed()
    {
        var marker = new AssemblyMarker();
        Assert.That(marker, Is.Not.Null);
    }
}
