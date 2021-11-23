using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  using ElementTracking;

  //[ComponentVersion(since: )]
  class SheetByNumber_Assembly : BaseSheetByNumber<AssemblySheetHandler>
  {
    public override Guid ComponentGuid => new Guid("68ad9e6a-d39e-4cda-9e41-3eb311d0cf2b");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public SheetByNumber_Assembly() : base
    (
      name: "Add Sheet (Assembly)",
      nickname: "Sheet (A)",
      description: "Create a new assembly sheet with given number and name"
    )
    { }

    static readonly (string name, string nickname, string tip) _Sheet_
    = (name: "Sheet", nickname: "S", tip: "Output Sheet");

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Assembly to create sheet for",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sheet Number",
          NickName = "NUM",
          Description = $"{_Sheet_.name} Number"
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sheet Name",
          NickName = "N",
          Description = $"{_Sheet_.name} Name",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template sheet (only sheet parameters are copied)",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = _Sheet_.name,
          NickName = _Sheet_.nickname,
          Description = _Sheet_.tip,
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // active document
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      Params.TryGetData(DA, "Assembly", out ARDB.AssemblyInstance assembly);

      // sheet input data
      if (!Params.TryGetData(DA, "Sheet Number", out string number, x => !string.IsNullOrEmpty(x))) return;
      // Note: see notes on SheetHandler.Name parameter
      if (!Params.TryGetData(DA, "Sheet Name", out string name, x => !string.IsNullOrEmpty(x))) return;

      Params.TryGetData(DA, "Template", out ARDB.ViewSheet template);

      // find any tracked sheet
      Params.ReadTrackedElement(_Sheet_.name, doc.Value, out ARDB.ViewSheet sheet);

      // update, or create
      StartTransaction(doc.Value);
      {
        sheet = Reconstruct(sheet, doc.Value, new AssemblySheetHandler(number)
        {
          Name = name,
          Assembly = assembly,
          Template = template
        });

        Params.WriteTrackedElement(_Sheet_.name, doc.Value, sheet);
        DA.SetData(_Sheet_.name, sheet);
      }
    }
  }
}
