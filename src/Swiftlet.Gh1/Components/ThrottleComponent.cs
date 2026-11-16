using Grasshopper.Kernel;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ThrottleComponent : GH_Component
{
    public ThrottleComponent()
        : base(
            "Throttle",
            "THRTL",
            "This is a simple passthrough component that lets you add a delay. Useful when sending multiple calls to APIs with limitations on maximum number of requests per minute",
            ShellNaming.Category,
            ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Input Data", "I", "Data to pass through", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Delay", "D", "Delay in milliseconds", GH_ParamAccess.item, 0);
        pManager[0].Optional = true;
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Output Data", "O", "Data that passed through unchanged", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object input = null;
        int delay = 0;

        DA.GetData(0, ref input);
        DA.GetData(1, ref delay);

        Thread.Sleep(Math.Abs(delay));
        DA.SetData(0, input);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("6f4d4fde-70f7-4393-92dd-5d87fc94e1dc");
}

