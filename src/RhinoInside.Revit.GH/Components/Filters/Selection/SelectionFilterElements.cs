using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.11")]
  public class SelectionElements : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("E90F2139-FA13-4EE2-BFD3-6642FA9053AB");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SelectionElements() : base
    (
      name: "Selection Filter Definition",
      nickname: "SeleElms",
      description: "Get-Set accessor for Selection Filter elements.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.FilterElement>("Selection Filter", "S"),
      ParamDefinition.Create<Parameters.Element>("Elements", "E", access: GH_ParamAccess.list, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.FilterElement>("Selection Filter", "S"),
      ParamDefinition.Create<Parameters.Element>("Elements", "E", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Selection Filter", out ARDB.SelectionFilterElement selection, x => x.IsValid())) return;
      else DA.SetData("Selection Filter", selection);

      if (Params.GetDataList(DA, "Elements", out IList<Types.Element> elements))
      {
        StartTransaction(selection.Document);

        var elementIds = elements?.Where(x => selection.Document.IsEquivalent(x.Document)).Select(x => x.Id).ToList();
        selection.SetElementIds(elementIds);
      }

      Params.TrySetDataList(DA, "Elements", () => selection.GetElementIds().Select(x => Types.Element.FromElementId(selection.Document, x)));
    }
  }
}
