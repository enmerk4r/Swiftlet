using GH_IO.Serialization;
using Grasshopper.Kernel;
using System.IO;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class SaveWebResponseComponent : GH_Component
{
    private bool _textChecked;
    private bool _binaryChecked;

    public SaveWebResponseComponent()
        : base("Save Web Response", "SWR", "Save Web Response to disk", ShellNaming.Category, ShellNaming.Utilities)
    {
        _binaryChecked = true;
        UpdateMessage();
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override bool Read(GH_IReader reader)
    {
        _textChecked = reader.GetBoolean(nameof(_textChecked));
        _binaryChecked = reader.GetBoolean(nameof(_binaryChecked));
        UpdateMessage();
        return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.SetBoolean(nameof(_textChecked), _textChecked);
        writer.SetBoolean(nameof(_binaryChecked), _binaryChecked);
        return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new HttpResponseDataParam(), "Response", "R", "Full Http response object (with metadata)", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Path to file", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Bytes", "B", "Size of saved file in bytes", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        HttpResponseDataGoo? goo = null;
        string path = string.Empty;

        DA.GetData(0, ref goo);
        DA.GetData(1, ref path);

        if (goo?.Value is null)
        {
            return;
        }

        if (_binaryChecked)
        {
            using (BinaryWriter writer = new(new FileStream(path, FileMode.Create)))
            {
                writer.Write(goo.Value.Bytes);
            }
        }
        else if (_textChecked)
        {
            using (StreamWriter writer = new(path))
            {
                writer.Write(goo.Value.Content);
            }
        }

        long length = new FileInfo(path).Length;
        DA.SetData(0, length);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E978195C-B6DC-4BE5-BF98-0246BBACEC7C");

    private void UpdateMessage()
    {
        if (_textChecked)
        {
            Message = "Text";
        }
        else if (_binaryChecked)
        {
            Message = "Binary";
        }
    }

    private void UncheckAll()
    {
        _textChecked = false;
        _binaryChecked = false;
    }
}
