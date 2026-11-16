namespace Swiftlet.Gh2;

// Rhino and GH2 discover the same assembly through their respective entry points.
public sealed class SwiftletRhinoPlugin : Rhino.PlugIns.PlugIn { }

public sealed class SwiftletPlugin : Grasshopper2.Framework.Plugin
{
    public override string LicenceDescription => "MIT";
    public override string LicenceAgreement => "https://opensource.org/license/mit";
}
