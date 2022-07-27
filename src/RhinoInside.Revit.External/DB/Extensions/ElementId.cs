using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class ElementIdComparer
  {
    public static readonly IComparer<ElementId> Ascending = default(AscendingComparer);
    public static readonly IComparer<ElementId> Descending = default(DescendingComparer);
    public static readonly IComparer<ElementId> NoNullsAscending = default(NoNullsAscendingComparer);
    public static readonly IComparer<ElementId> NoNullsDescending = default(NoNullsDescendingComparer);

    struct AscendingComparer : IComparer<ElementId>
    {
      int IComparer<ElementId>.Compare(ElementId x, ElementId y)
      {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null && y is object) return -1;
        if (x is object && y is null) return +1;
        return x.Compare(y);
      }
    }

    struct DescendingComparer : IComparer<ElementId>
    {
      int IComparer<ElementId>.Compare(ElementId x, ElementId y)
      {
        if (ReferenceEquals(y, x)) return 0;
        if (y is null && x is object) return -1;
        if (y is object && x is null) return +1;
        return y.Compare(x);
      }
    }

    struct NoNullsAscendingComparer : IComparer<ElementId>
    {
      int IComparer<ElementId>.Compare(ElementId x, ElementId y) => x.Compare(y);
    }

    struct NoNullsDescendingComparer : IComparer<ElementId>
    {
      int IComparer<ElementId>.Compare(ElementId x, ElementId y) => y.Compare(x);
    }
  }

  internal struct ElementIdEqualityComparer : IEqualityComparer<ElementId>
  {
    bool IEqualityComparer<ElementId>.Equals(ElementId x, ElementId y) => ReferenceEquals(x, y) || x?.ToValue() == y?.ToValue();
    int IEqualityComparer<ElementId>.GetHashCode(ElementId obj) => obj?.GetHashCode() ?? 0;
  }

  public static class ElementIdExtension
  {
    public static bool IsValid(this ElementId id) => id is object && id != ElementId.InvalidElementId;
    public static bool IsBuiltInId(this ElementId id) => id is object && id <= ElementId.InvalidElementId;
    public static int ToValue(this ElementId id) => id.IntegerValue;

    #region Parameters
    public static BuiltInParameter ToBuiltInParameter(this ElementId id) => TryGetBuiltInParameter(id, out var value) ? value : BuiltInParameter.INVALID;

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.Parameter"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsParameterId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Parameter
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        try { return doc.GetElement(id) is ParameterElement; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return ((BuiltInParameter) id.IntegerValue).IsValid();
    }

    /// <summary>
    /// Checks if id corresponds to a valid <see cref="Autodesk.Revit.DB.BuiltInParameter"/> id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInParameter"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInParameter(this ElementId id, out BuiltInParameter builtInParameter)
    {
      builtInParameter = (BuiltInParameter) id.IntegerValue;
      if (builtInParameter.IsValid())
        return true;

      builtInParameter = BuiltInParameter.INVALID;
      return false;
    }
    #endregion

    #region Categories
    public static BuiltInCategory ToBuiltInCategory(this ElementId id) => TryGetBuiltInCategory(id, out var value) ? value : BuiltInCategory.INVALID;

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.Category"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsCategoryId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Category
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        // 1. We try with the regular way calling Category.GetCategory
        try { return Category.GetCategory(doc, id) is object; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        // 2. Try looking for any GraphicsStyle that points to the Category we are looking for.
        if (doc.GetElement(id) is Element element && element.GetType() == typeof(Element))
        {
          if (element.GetFirstDependent<GraphicsStyle>() is GraphicsStyle style)
            return style.GraphicsStyleCategory.Id == id;
        }
      }

      return ((BuiltInCategory) id.IntegerValue).IsValid();
    }

    /// <summary>
    /// Checks if id corresponds to a valid <see cref="Autodesk.Revit.DB.BuiltInCategory"/> id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInCategory"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInCategory(this ElementId id, out BuiltInCategory builtInCategory)
    {
      builtInCategory = (BuiltInCategory) id.IntegerValue;
      if (builtInCategory.IsValid())
        return true;

      builtInCategory = BuiltInCategory.INVALID;
      return false;
    }
    #endregion

    #region LinePattern
    public static BuiltInLinePattern ToBuiltInLinePattern(this ElementId id) => TryGetBuiltInLinePattern(id, out var value) ? value : BuiltInLinePattern.INVALID;

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.LinePatternElement"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsLinePatternId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Line Pattern
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        try { return doc.GetElement(id) is LinePatternElement; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return ((BuiltInLinePattern) id.IntegerValue).IsValid();
    }

    /// <summary>
    /// Checks if id corresponds to a valid <see cref="RhinoInside.Revit.External.DB.BuiltInLinePattern"/> id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInPattern"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInLinePattern(this ElementId id, out BuiltInLinePattern builtInPattern)
    {
      builtInPattern = (BuiltInLinePattern) id.IntegerValue;
      if (builtInPattern.IsValid())
        return true;

      builtInPattern = BuiltInLinePattern.INVALID;
      return false;
    }
    #endregion

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to an <see cref="Autodesk.Revit.DB.ElementType"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsElementTypeId(this ElementId id, Document doc)
    {
      using (var filter = new ElementIsElementTypeFilter())
      {
        return filter.PassesFilter(doc, id);
      }
    }
  }
}
