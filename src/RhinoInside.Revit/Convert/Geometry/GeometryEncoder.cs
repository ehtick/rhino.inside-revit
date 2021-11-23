using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB.Extensions;

  /// <summary>
  /// Methods in this class do a full geometry conversion.
  /// </summary>
  /// <remarks>
  /// <para>It converts geometry from Active Rhino model units to Revit internal units.</para>
  /// <para>For direct conversion methods see <see cref="Raw.RawEncoder"/> class.</para>
  /// </remarks>
  public static class GeometryEncoder
  {
    #region Context
    internal delegate void RuntimeMessage(int severity, string message, GeometryBase geometry);

    [DebuggerTypeProxy(typeof(DebugView))]
    internal sealed class Context : State<Context>
    {
      public static Context Push(ARDB.Document document)
      {
        var ctx = Push();
        if (!ctx.Document.IsEquivalent(document))
        {
          ctx.GraphicsStyleId = ARDB.ElementId.InvalidElementId;
          ctx.MaterialId = ARDB.ElementId.InvalidElementId;
          ctx.FaceMaterialId = default;
        }
        ctx.Document = document;
        ctx.Element = default;
        return ctx;
      }

      public static Context Push(ARDB.Element element)
      {
        var ctx = Push(element?.Document);
        ctx.Element = element;
        return ctx;
      }

      public ARDB.Document Document { get; private set; } = default;
      public ARDB.Element Element { get; private set; } = default;

      public ARDB.ElementId GraphicsStyleId = ARDB.ElementId.InvalidElementId;
      public ARDB.ElementId MaterialId = ARDB.ElementId.InvalidElementId;
      public IReadOnlyList<ARDB.ElementId> FaceMaterialId;
      public RuntimeMessage RuntimeMessage = NullRuntimeMessage;

      static void NullRuntimeMessage(int severity, string message, GeometryBase geometry) { }

      class DebugView
      {
        readonly Context context;
        public DebugView(Context value) => context = value;
        public ARDB.Document Document => context.Document;
        public ARDB.Element Element => context.Element;

        public ARDB.GraphicsStyle GraphicsStyle => context.Document?.GetElement(context.GraphicsStyleId) as ARDB.GraphicsStyle;
        public ARDB.Material Material => context.Document?.GetElement(context.MaterialId) as ARDB.Material;
        public IEnumerable<ARDB.Material> FaceMaterials
        {
          get
          {
            if (context.Document is null || context.FaceMaterialId is null) return default;
            return context.FaceMaterialId.Select(x => context.Document.GetElement(x) as ARDB.Material);
          }
        }
      }
    }
    #endregion

    #region Geometry values
    public static ARDB::UV ToUV(this Point2f value)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::UV(value.X * factor, value.Y * factor);
    }
    public static ARDB::UV ToUV(this Point2f value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(value.X, value.Y) :
        new ARDB::UV(value.X * factor, value.Y * factor);
    }

    public static ARDB::UV ToUV(this Point2d value)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::UV(value.X * factor, value.Y * factor);
    }
    public static ARDB::UV ToUV(this Point2d value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(value.X, value.Y) :
        new ARDB::UV(value.X * factor, value.Y * factor);
    }

    public static ARDB::UV ToUV(this Vector2f value)
    {
      return new ARDB::UV(value.X, value.Y);
    }
    public static ARDB::UV ToUV(this Vector2f value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(value.X, value.Y) :
        new ARDB::UV(value.X * factor, value.Y * factor);
    }

    public static ARDB::UV ToUV(this Vector2d value)
    {
      return new ARDB::UV(value.X, value.Y);
    }
    public static ARDB::UV ToUV(this Vector2d value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(value.X, value.Y) :
        new ARDB::UV(value.X * factor, value.Y * factor);
    }

    public static ARDB::XYZ ToXYZ(this Point3f value)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }
    public static ARDB::XYZ ToXYZ(this Point3f value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(value.X, value.Y, value.Z) :
        new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }

    public static ARDB::XYZ ToXYZ(this Point3d value)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }
    public static ARDB::XYZ ToXYZ(this Point3d value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(value.X, value.Y, value.Z) :
        new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }

    public static ARDB::XYZ ToXYZ(this Vector3f value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }
    public static ARDB::XYZ ToXYZ(this Vector3f value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(value.X, value.Y, value.Z) :
        new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }

    public static ARDB::XYZ ToXYZ(this Vector3d value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }
    public static ARDB::XYZ ToXYZ(this Vector3d value, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(value.X, value.Y, value.Z) :
        new ARDB::XYZ(value.X * factor, value.Y * factor, value.Z * factor);
    }

    public static ARDB.Plane ToPlane(this Plane value) => ToPlane(value, UnitConverter.ToHostUnits);
    public static ARDB.Plane ToPlane(this Plane value, double factor)
    {
      return ARDB.Plane.CreateByOriginAndBasis(value.Origin.ToXYZ(factor), value.XAxis.ToXYZ(), value.YAxis.ToXYZ());
    }

    public static ARDB.Transform ToTransform(this Transform value) => ToTransform(value, UnitConverter.ToHostUnits);
    public static ARDB.Transform ToTransform(this Transform value, double factor)
    {
      Debug.Assert(value.IsAffine);

      var result = factor == 1.0 ?
        ARDB.Transform.CreateTranslation(new ARDB.XYZ(value.M03, value.M13, value.M23)) :
        ARDB.Transform.CreateTranslation(new ARDB.XYZ(value.M03 * factor, value.M13 * factor, value.M23 * factor));

      result.BasisX = new ARDB.XYZ(value.M00, value.M10, value.M20);
      result.BasisY = new ARDB.XYZ(value.M01, value.M11, value.M21);
      result.BasisZ = new ARDB.XYZ(value.M02, value.M12, value.M22);
      return result;
    }

    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this BoundingBox value) => ToBoundingBoxXYZ(value, UnitConverter.ToHostUnits);
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this BoundingBox value, double factor)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Min = value.Min.ToXYZ(factor),
        Max = value.Min.ToXYZ(factor),
        Enabled = value.IsValid
      };
    }

    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this Box value) => ToBoundingBoxXYZ(value, UnitConverter.ToHostUnits);
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this Box value, double factor)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Transform = Transform.PlaneToPlane(Plane.WorldXY, value.Plane).ToTransform(factor),
        Min = new ARDB.XYZ(value.X.Min * factor, value.Y.Min * factor, value.Z.Min * factor),
        Max = new ARDB.XYZ(value.X.Max * factor, value.Y.Max * factor, value.Z.Max * factor),
        Enabled = value.IsValid
      };
    }

    public static ARDB.Outline ToOutline(this BoundingBox value) => ToOutline(value, UnitConverter.ToHostUnits);
    public static ARDB.Outline ToOutline(this BoundingBox value, double factor)
    {
      return new ARDB.Outline(value.Min.ToXYZ(factor), value.Max.ToXYZ(factor));
    }
    #endregion

    #region Curve values
    public static ARDB.Line ToLine(this Line value) => value.ToLine(UnitConverter.ToHostUnits);
    public static ARDB.Line ToLine(this Line value, double factor)
    {
      return ARDB.Line.CreateBound(value.From.ToXYZ(factor), value.To.ToXYZ(factor));
    }

    public static ARDB.Line[] ToLines(this Polyline value) => value.ToLines(UnitConverter.ToHostUnits);
    public static ARDB.Line[] ToLines(this Polyline value, double factor)
    {
      value = value.Duplicate();
      value.DeleteShortSegments(Revit.ShortCurveTolerance / factor);

      int count = value.Count;
      var list = new ARDB.Line[Math.Max(0, count - 1)];
      if (count > 1)
      {
        var point = value[0];
        ARDB.XYZ end, start = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
        for (int p = 1; p < count; start = end, ++p)
        {
          point = value[p];
          end = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
          list[p-1] = ARDB.Line.CreateBound(start, end);
        }
      }

      return list;
    }

    public static ARDB.PolyLine ToPolyLine(this Polyline value) => value.ToPolyLine(UnitConverter.ToHostUnits);
    public static ARDB.PolyLine ToPolyLine(this Polyline value, double factor)
    {
      int count = value.Count;
      var points = new ARDB.XYZ[count];

      if (factor == 1.0)
      {
        for (int p = 0; p < count; ++p)
        {
          var point = value[p];
          points[p] = new ARDB.XYZ(point.X, point.Y, point.Z);
        }
      }
      else
      {
        for (int p = 0; p < count; ++p)
        {
          var point = value[p];
          points[p] = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
        }
      }

      return ARDB.PolyLine.Create(points);
    }

    public static ARDB.Arc ToArc(this Arc value) => value.ToArc(UnitConverter.ToHostUnits);
    public static ARDB.Arc ToArc(this Arc value, double factor)
    {
      if (value.IsCircle)
        return ARDB.Arc.Create(value.Plane.ToPlane(factor), value.Radius * factor, 0.0, 2.0 * Math.PI);
      else
        return ARDB.Arc.Create(value.StartPoint.ToXYZ(factor), value.EndPoint.ToXYZ(factor), value.MidPoint.ToXYZ(factor));
    }

    public static ARDB.Arc ToArc(this Circle value) => value.ToArc(UnitConverter.ToHostUnits);
    public static ARDB.Arc ToArc(this Circle value, double factor)
    {
      return ARDB.Arc.Create(value.Plane.ToPlane(factor), value.Radius * factor, 0.0, 2.0 * Math.PI);
    }

    public static ARDB.Curve ToCurve(this Ellipse value) => value.ToCurve(new Interval(0.0, 2.0 * Math.PI), UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this Ellipse value, double factor) => value.ToCurve(new Interval(0.0, 2.0 * Math.PI), factor);
    public static ARDB.Curve ToCurve(this Ellipse value, Interval interval) => value.ToCurve(interval, UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this Ellipse value, Interval interval, double factor)
    {
#if REVIT_2018
      return ARDB.Ellipse.CreateCurve(value.Plane.Origin.ToXYZ(factor), value.Radius1 * factor, value.Radius2 * factor, value.Plane.XAxis.ToXYZ(), value.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
#else
      return DB.Ellipse.Create(value.Plane.Origin.ToXYZ(factor), value.Radius1 * factor, value.Radius2 * factor, value.Plane.XAxis.ToXYZ(), value.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
#endif
    }
    #endregion

    #region GeometryBase
    public static ARDB.Point ToPoint(this Point value) => value.ToPoint(UnitConverter.ToHostUnits);
    public static ARDB.Point ToPoint(this Point value, double factor)
    {
      return ARDB.Point.Create(value.Location.ToXYZ(factor));
    }

    public static ARDB.Point[] ToPoints(this PointCloud value) => value.ToPoints(UnitConverter.ToHostUnits);
    public static ARDB.Point[] ToPoints(this PointCloud value, double factor)
    {
      var array = new ARDB.Point[value.Count];
      int index = 0;
      if (factor == 1.0)
      {
        foreach (var point in value)
        {
          var location = point.Location;
          array[index++] = ARDB.Point.Create(new ARDB::XYZ(location.X, location.Y, location.Z));
        }
      }
      else
      {
        foreach (var point in value)
        {
          var location = point.Location;
          array[index++] = ARDB.Point.Create(new ARDB::XYZ(location.X * factor, location.Y * factor, location.Z * factor));
        }
      }

      return array;
    }

    public static ARDB.Curve ToCurve(this LineCurve value) => value.Line.ToLine(UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this LineCurve value, double factor) => value.Line.ToLine(factor);

    public static ARDB.Curve ToCurve(this PolylineCurve value) => ToCurve(value, UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this PolylineCurve value, double factor)
    {
      if (value.TryGetLine(out var line, Revit.VertexTolerance * factor))
        return line.ToLine(factor);

      throw new ConversionException("Failed to convert non G1 continuous curve.");
    }

    public static ARDB.Curve ToCurve(this ArcCurve value) => value.Arc.ToArc(UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this ArcCurve value, double factor) => value.Arc.ToArc(factor);

    public static ARDB.Curve ToCurve(this NurbsCurve value) => value.ToCurve(UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this NurbsCurve value, double factor)
    {
      if (value.TryGetEllipse(out var ellipse, out var interval, Revit.VertexTolerance * factor))
        return ellipse.ToCurve(interval, factor);

      var gap = Revit.ShortCurveTolerance * 1.01 / factor;
      if (value.IsClosed(gap))
      {
        var length = value.GetLength();
        if
        (
          length > gap &&
          value.LengthParameter((gap / 2.0), out var t0) &&
          value.LengthParameter(length - (gap / 2.0), out var t1)
        )
        {
          var segments = value.Split(new double[] { t0, t1 });
          value = segments[0] as NurbsCurve ?? value;
        }
        else throw new ConversionException($"Failed to Split closed NurbsCurve, Length = {length}");
      }

      if (value.Degree < 3 && value.SpanCount > 1)
      {
        value = value.DuplicateCurve() as NurbsCurve;
        value.IncreaseDegree(3);
      }

      return NurbsSplineEncoder.ToNurbsSpline(value, factor);
    }

    public static ARDB.Curve ToCurve(this PolyCurve value) => ToCurve(value, UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this PolyCurve value, double factor)
    {
      var curve = value.Simplify
      (
        CurveSimplifyOptions.AdjustG1 |
        CurveSimplifyOptions.Merge,
        Revit.VertexTolerance * factor,
        Revit.AngleTolerance
      )
      ?? value;

      if (curve is PolyCurve)
        return curve.ToNurbsCurve().ToCurve(factor);
      else
        return curve.ToCurve(factor);
    }

    public static ARDB.Curve ToCurve(this Curve value) => value.ToCurve(UnitConverter.ToHostUnits);
    public static ARDB.Curve ToCurve(this Curve value, double factor)
    {
      switch (value)
      {
        case LineCurve line:
          return line.Line.ToLine(factor);

        case ArcCurve arc:
          return arc.Arc.ToArc(factor);

        case PolylineCurve polyline:
          return polyline.ToCurve(factor);

        case PolyCurve polyCurve:
          return polyCurve.ToCurve(factor);

        case NurbsCurve nurbsCurve:
          return nurbsCurve.ToCurve(factor);

        default:
          return value.ToNurbsCurve().ToCurve(factor);
      }
    }

    public static ARDB.CurveLoop ToCurveLoop(this Curve value)
    {
      value = value.InOtherUnits(UnitConverter.ToHostUnits);
      value.CombineShortSegments(Revit.ShortCurveTolerance);

      return ARDB.CurveLoop.Create(value.ToCurveMany(UnitConverter.NoScale).SelectMany(x => x.ToBoundedCurves()).ToList());
    }

    public static ARDB.CurveArray ToCurveArray(this Curve value)
    {
      value = value.InOtherUnits(UnitConverter.ToHostUnits);
      value.CombineShortSegments(Revit.ShortCurveTolerance);

      return value.ToCurveMany(UnitConverter.NoScale).SelectMany(x => x.ToBoundedCurves()).ToCurveArray();
    }

    public static ARDB.CurveArrArray ToCurveArrayArray(this IList<Curve> value)
    {
      var curveArrayArray = new ARDB.CurveArrArray();
      foreach (var curve in value)
        curveArrayArray.Append(curve.ToCurveArray());

      return curveArrayArray;
    }

    #region ToSolid
    public static ARDB.Solid ToSolid(this Brep value) => BrepEncoder.ToSolid(value, UnitConverter.ToHostUnits);
    public static ARDB.Solid ToSolid(this Brep value, double factor) => BrepEncoder.ToSolid(value, factor);

    public static ARDB.Solid ToSolid(this Extrusion value) => ExtrusionEncoder.ToSolid(value, UnitConverter.ToHostUnits);
    public static ARDB.Solid ToSolid(this Extrusion value, double factor) => ExtrusionEncoder.ToSolid(value, factor);

    public static ARDB.Solid ToSolid(this SubD value) => SubDEncoder.ToSolid(value, UnitConverter.ToHostUnits);
    public static ARDB.Solid ToSolid(this SubD value, double factor) => SubDEncoder.ToSolid(value, factor);

    public static ARDB.Solid ToSolid(this Mesh value) => Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(value, UnitConverter.ToHostUnits));
    public static ARDB.Solid ToSolid(this Mesh value, double factor) => Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(value, factor));
    #endregion

    #region ToMesh
    public static ARDB.Mesh ToMesh(this Brep value) => BrepEncoder.ToMesh(value, UnitConverter.NoScale);
    public static ARDB.Mesh ToMesh(this Brep value, double factor) => BrepEncoder.ToMesh(value, factor);

    public static ARDB.Mesh ToMesh(this Extrusion value) => ExtrusionEncoder.ToMesh(value, UnitConverter.NoScale);
    public static ARDB.Mesh ToMesh(this Extrusion value, double factor) => ExtrusionEncoder.ToMesh(value, factor);

    public static ARDB.Mesh ToMesh(this SubD value) => SubDEncoder.ToMesh(value, UnitConverter.NoScale);
    public static ARDB.Mesh ToMesh(this SubD value, double factor) => SubDEncoder.ToMesh(value, factor);

    public static ARDB.Mesh ToMesh(this Mesh value) => MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(value, UnitConverter.ToHostUnits));
    public static ARDB.Mesh ToMesh(this Mesh value, double factor) => MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(value, factor));
    #endregion

    public static ARDB.GeometryObject ToGeometryObject(this GeometryBase geometry) => ToGeometryObject(geometry, UnitConverter.ToHostUnits);
    public static ARDB.GeometryObject ToGeometryObject(this GeometryBase geometry, double scaleFactor)
    {
      switch (geometry)
      {
        case Point point:         return point.ToPoint(scaleFactor);
        case Curve curve:         return curve.ToCurve(scaleFactor);
        case Brep brep:           return brep.ToSolid(scaleFactor);
        case Extrusion extrusion: return extrusion.ToSolid(scaleFactor);
        case SubD subD:           return subD.ToSolid(scaleFactor);
        case Mesh mesh:           return mesh.ToMesh(scaleFactor);

        default:
          if (geometry.HasBrepForm)
          {
            var brepForm = Brep.TryConvertBrep(geometry);
            if (BrepEncoder.EncodeRaw(ref brepForm, scaleFactor))
              return BrepEncoder.ToSolid(brepForm);
          }
          break;
      }

      throw new ConversionException($"Unable to convert {geometry} to Autodesk.Revit.DB.GeometryObject");
    }
    #endregion

    public static IEnumerable<ARDB.Point> ToPointMany(this PointCloud value) => value.ToPointMany(UnitConverter.ToHostUnits);
    public static IEnumerable<ARDB.Point> ToPointMany(this PointCloud value, double factor)
    {
      if (factor == 1.0)
      {
        foreach (var point in value)
        {
          var location = point.Location;
          yield return ARDB.Point.Create(new ARDB::XYZ(location.X, location.Y, location.Z));
        }
      }
      else
      {
        foreach (var point in value)
        {
          var location = point.Location;
          yield return ARDB.Point.Create(new ARDB::XYZ(location.X * factor, location.Y * factor, location.Z * factor));
        }
      }
    }

    public static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve value) => value.ToCurveMany(UnitConverter.ToHostUnits);
    public static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve value, double factor)
    {
      // Convert to Raw form
      value = value.DuplicateCurve() as NurbsCurve;
      if (factor != 1.0) value.Scale(factor);
      value.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      if (value.Degree == 1)
      {
        var curvePoints = value.Points;
        int pointCount = curvePoints.Count;
        if (pointCount > 1)
        {
          ARDB.XYZ end, start = curvePoints[0].Location.ToXYZ(UnitConverter.NoScale);
          for (int p = 1; p < pointCount; ++p)
          {
            end = curvePoints[p].Location.ToXYZ(UnitConverter.NoScale);
            if (end.DistanceTo(start) < Revit.ShortCurveTolerance)
              continue;

            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
      else if (value.TryGetPolyCurve(out var polyCurve, Revit.AngleTolerance))
      {
        foreach (var segment in ToCurveMany(polyCurve, UnitConverter.NoScale))
          yield return segment;

        yield break;
      }
      else if (value.Degree == 2)
      {
        if (value.IsRational && value.TryGetEllipse(out var ellipse, out var interval, Revit.VertexTolerance))
        {
          // Only degree 2 rational NurbCurves should be transferred as an Arc-Ellipse
          // to avoid unexpected Arcs-Ellipses near linear with gigantic radius.
          yield return ellipse.ToCurve(interval, UnitConverter.NoScale);
        }
        else if (value.SpanCount == 1)
        {
          yield return NurbsSplineEncoder.ToNurbsSpline(value, UnitConverter.NoScale);
        }
        else
        {
          for (int s = 0; s < value.SpanCount; ++s)
          {
            var segment = value.Trim(value.SpanDomain(s)) as NurbsCurve;
            yield return NurbsSplineEncoder.ToNurbsSpline(segment, UnitConverter.NoScale);
          }
        }
      }
      else if (value.IsClosed(Revit.ShortCurveTolerance * 1.01))
      {
        var segments = value.DuplicateSegments();
        if (segments.Length == 1)
        {
          if
          (
            value.NormalizedLengthParameter(0.5, out var mid) &&
            value.Split(mid) is Curve[] half
          )
          {
            yield return NurbsSplineEncoder.ToNurbsSpline(half[0] as NurbsCurve, UnitConverter.NoScale);
            yield return NurbsSplineEncoder.ToNurbsSpline(half[1] as NurbsCurve, UnitConverter.NoScale);
          }
          else throw new ConversionException("Failed to Split closed Edge");
        }
        else
        {
          foreach (var segment in segments)
            yield return NurbsSplineEncoder.ToNurbsSpline(segment as NurbsCurve, UnitConverter.NoScale);
        }
      }
      else
      {
        yield return NurbsSplineEncoder.ToNurbsSpline(value, UnitConverter.NoScale);
      }
    }

    public static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve value) => value.ToCurveMany(UnitConverter.ToHostUnits);
    public static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve value, double factor)
    {
      // Convert to Raw form
      value = value.DuplicateCurve() as PolylineCurve;
      if(factor != 1.0) value.Scale(factor);
      value.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      int pointCount = value.PointCount;
      if (pointCount > 1)
      {
        ARDB.XYZ end, start = value.Point(0).ToXYZ(UnitConverter.NoScale);
        for (int p = 1; p < pointCount; ++p)
        {
          end = value.Point(p).ToXYZ(UnitConverter.NoScale);
          if (start.DistanceTo(end) > Revit.ShortCurveTolerance)
          {
            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
    }

    public static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve value) => value.ToCurveMany(UnitConverter.ToHostUnits);
    public static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve value, double factor)
    {
      // Convert to Raw form
      value = value.DuplicateCurve() as PolyCurve;
      if (factor != 1.0) value.Scale(factor);
      value.RemoveNesting();
      value.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      int segmentCount = value.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        foreach (var segment in value.SegmentCurve(s).ToCurveMany(UnitConverter.NoScale))
          yield return segment;
      }
    }

    public static IEnumerable<ARDB.Curve> ToCurveMany(this Curve value) => value.ToCurveMany(UnitConverter.ToHostUnits);
    public static IEnumerable<ARDB.Curve> ToCurveMany(this Curve curve, double factor)
    {
      switch (curve)
      {
        case LineCurve lineCurve:

          yield return lineCurve.Line.ToLine(factor);
          yield break;

        case PolylineCurve polylineCurve:

          foreach (var line in polylineCurve.ToCurveMany(factor))
            yield return line;
          yield break;

        case ArcCurve arcCurve:

          yield return arcCurve.Arc.ToArc(factor);
          yield break;

        case PolyCurve poly:

          foreach (var segment in poly.ToCurveMany(factor))
            yield return segment;
          yield break;

        case NurbsCurve nurbs:

          foreach (var segment in nurbs.ToCurveMany(factor))
            yield return segment;
          yield break;

        default:
          if (curve.HasNurbsForm() != 0)
          {
            var nurbsForm = curve.ToNurbsCurve();
            foreach (var c in nurbsForm.ToCurveMany(factor))
              yield return c;
          }
          else throw new ConversionException($"Failed to convert {curve} to DB.Curve");

          yield break;
      }
    }
  }
}
