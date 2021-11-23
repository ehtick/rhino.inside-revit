using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  public class ProjectParameter : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("1CC5AF70-8C5A-4E4A-9D5F-52925D6D4A61");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var doc = activeApp.ActiveUIDocument?.Document;
      if (doc is null) return;

      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectParameters);
      Menu_AppendItem
      (
        menu, $"Open Project Parameters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        !doc.IsFamilyDocument && activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public ProjectParameter() : base
    (
      name: "Project Parameter",
      nickname: "Categories",
      description: "Gives acces to project parameter settings",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = _Parameter_,
          NickName = "P",
          Description = "A project parameter"
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Varies",
          NickName = "V",
          Description = "Varies across groups",
          Optional = true,
        },
        ParamRelevance.Primary
      ),
      //new ParamDefinition
      //(
      //  new Parameters.Param_Enum<Types.ParameterBinding>()
      //  {
      //    Name = "Binding",
      //    NickName = "B",
      //    Description = "Parameter binding",
      //    Optional = true
      //  },
      //  ParamRelevance.Primary
      //),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Categories",
          NickName = "C",
          Description = "Categories to bind",
          Access = GH_ParamAccess.list,
          Optional = true,
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = _Parameter_,
          NickName = "P",
          Description = "The project parameter"
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Varies",
          NickName = "V",
          Description = "Varies across groups",
        },
        ParamRelevance.Primary
      ),
      //new ParamDefinition
      //(
      //  new Parameters.Param_Enum<Types.ParameterBinding>()
      //  {
      //    Name = "Binding",
      //    NickName = "B",
      //    Description = "Parameter binding",
      //  },
      //  ParamRelevance.Primary
      //),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Categories",
          NickName = "C",
          Description = "Categories to bind",
          Access = GH_ParamAccess.list,
        },
        ParamRelevance.Primary
      ),
    };

    const string _Parameter_ = "Parameter";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.ParameterKey.GetProjectParameter(this, DA, _Parameter_, out var key)) return;
      if (!Params.TryGetData(DA, "Varies", out bool? varies)) return;
      if (!Params.TryGetData(DA, "Binding", out Types.ParameterBinding binding, x => x.IsValid && x.Value != ERDB.ParameterBinding.Unknown)) return;
      if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return;

      var doc = key.Document;

      var definition = key.Value?.GetDefinition();
      if (binding is object || varies is object || categories is object)
      {
        if (key.Id.IsBuiltInId() || key.Value is ARDB.GlobalParameter || doc?.IsFamilyDocument == true)
          throw new InvalidOperationException("This operation is only supported on project parameters");

        StartTransaction(doc);

        if (varies != default && definition.VariesAcrossGroups != varies.Value)
          definition.SetAllowVaryBetweenGroups(doc, varies.Value);

        var parameterBinding = binding?.Value ?? ERDB.ParameterBinding.Unknown;
        var parameterCategories = categories?.Select(x => x.APIObject).OfType<ARDB.Category>().ToList();

        UpdateBinding
        (
          key.Value,
          definition,
          parameterBinding,
          parameterCategories
        );
      }

      DA.SetData(_Parameter_, key);
      if (definition is object)
      {
        Params.TrySetData(DA, "Varies", () => definition.VariesAcrossGroups);
        Params.TrySetData(DA, "Binding", () => definition.GetParameterBinding(key.Document));
        Params.TrySetDataList(DA, "Categories", () =>
          (key.Document.ParameterBindings.get_Item(definition) as ARDB.ElementBinding)?.Categories.Cast<ARDB.Category>());
      }
    }

    ERDB.ParameterBinding ToParameterBinding(ARDB.Binding binding)
    {
      switch (binding)
      {
        case ARDB.InstanceBinding _: return ERDB.ParameterBinding.Instance;
        case ARDB.TypeBinding _: return ERDB.ParameterBinding.Type;
        default: return ERDB.ParameterBinding.Unknown;
      }
    }

    ARDB.Binding CreateBinding(ERDB.ParameterBinding binding, ARDB.CategorySet categorySet)
    {
      switch (binding)
      {
        case ERDB.ParameterBinding.Instance: return new ARDB.InstanceBinding(categorySet);
        case ERDB.ParameterBinding.Type: return new ARDB.TypeBinding(categorySet);
      }
      return default;
    }

    bool UpdateBinding
    (
      ARDB.ParameterElement parameter,
      ARDB.InternalDefinition definition,
      ERDB.ParameterBinding parameterBinding,
      IList<ARDB.Category> categories
    )
    {
      bool reinsert = false;
      var doc = parameter.Document;
      var binding = doc.ParameterBindings.get_Item(definition);
      var categorySet = (binding as ARDB.ElementBinding)?.Categories ?? new ARDB.CategorySet();
      var bindingType = ToParameterBinding(binding);

      if (parameterBinding != ERDB.ParameterBinding.Unknown && bindingType != parameterBinding)
      {
        reinsert = true;
        binding = CreateBinding(parameterBinding, categorySet);
      }

      if (categories?.Count == 0)
        categories = doc.GetBuiltInCategoriesWithParameters().Select(x => doc.GetCategory(x)).ToList();

      if (categories is object)
      {
        if (categories.Count != categorySet.Size || categories.Any(x => !categorySet.Contains(x)))
        {
          reinsert = true;
          categorySet.Clear();

          foreach (var category in categories)
            categorySet.Insert(category);
        }
      }

      if (reinsert)
      {
        if (!doc.ParameterBindings.ReInsert(definition, binding, definition.GetGroupType()))
        {
          if (parameter is ARDB.SharedParameterElement)
            throw new InvalidOperationException("Failed editing the parameter binding.");
          else
            throw new InvalidOperationException($"Categories rebinding is only supported on shared parameters.");
        }
      }

      return reinsert;
    }
  }
}
