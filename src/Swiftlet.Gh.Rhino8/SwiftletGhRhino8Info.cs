using System.Drawing;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8;

public sealed class SwiftletGhRhino8Info : GH_AssemblyInfo
{
    private static string? _displayVersion;

    public override string Name => "Swiftlet";

    public override Bitmap Icon => ShellIcons.Logo24()!;

    public override string Description => "Grasshopper plugin for accessing Web APIs";

    public override Guid Id => new("DAB9AF34-1544-4374-96B2-288F6F4788DC");

    public override string AuthorName => "Sergey Pigach";

    public override string AuthorContact => string.Empty;

    public override string Version => DisplayVersion;

    public override string AssemblyVersion => DisplayVersion;

    private static string DisplayVersion
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_displayVersion))
            {
                return _displayVersion;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string? informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                string normalized = informationalVersion.Split('+')[0];
                _displayVersion = normalized;
                return normalized;
            }

            string? assemblyVersion = assembly.GetName().Version?.ToString();
            _displayVersion = string.IsNullOrWhiteSpace(assemblyVersion)
                ? "0.0.0"
                : assemblyVersion.TrimEnd('0').TrimEnd('.');

            return _displayVersion;
        }
    }
}

public sealed class SwiftletCategoryIcon : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        if (ShellIcons.Logo16() is Bitmap bitmap)
        {
            Instances.ComponentServer.AddCategoryIcon(ShellNaming.Category, bitmap);
        }

        Instances.ComponentServer.AddCategorySymbolName(ShellNaming.Category, 'S');
        return GH_LoadingInstruction.Proceed;
    }
}
