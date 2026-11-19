using Grasshopper2.UI.Icon;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2;

// Rhino and GH2 discover the same assembly through their respective entry points.
public sealed class SwiftletRhinoPlugin : Rhino.PlugIns.PlugIn { }

public sealed class SwiftletPlugin : Grasshopper2.Framework.Plugin
{
    public override IIcon Icon => SwiftletIcons.For(GetType()) ?? base.Icon;

    public override string LicenceDescription => "MIT";
    public override string LicenceAgreement => "https://opensource.org/license/mit";
}
