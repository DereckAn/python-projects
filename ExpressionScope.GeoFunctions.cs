using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapLarge.Common.GeoNumerics.Crs;
using MapLarge.Common.QueryEngine.Hash;
using MapLarge.Common.IO;
using MapLarge.Engine.Database;
using MapLarge.Engine.Database.Column.DataTypes;
using MapLarge.Engine.Geo;
using MapLarge.Engine.Geo.MartinezClipping;
using MapLarge.Engine.Import;
using MapLarge.Engine.Layer.Raster;
using MapLarge.Engine.Layer.UnifiedRendering;
using MapLarge.Engine.Logging;
using MapLarge.GeoNumerics.Primitives;
using MapLarge.Engine.Query.Expressions;
using MapLarge.Engine.Query.Expressions.Plan;
using MapLarge.Engine.Query.Expressions.Plan.JoinMethods;
using MapLarge.Engine.Query.GeomGroupBy;
using MapLarge.Engine.QueryEngine.Queryable;
using MapLarge.Engine.ValueTest.GeoTests;
using MapLarge.PluginDependencies.Api.DataTypes;
using MathNet.Numerics.LinearAlgebra;
using MapLarge.GeoNumerics.Crs;
using MapLarge.GeoNumerics.DataTypes;
using MapLarge.GeoNumerics.Geo;
using MapLarge.GeoNumerics.Crs.Reference;
using MapLarge.GeoNumerics.Geo.DelaunatorSharp;
using MapLarge.GeoNumerics.GridSystem;
using ClipLib = MapLarge.GeoNumerics.Clipping.ClipperLib;
using MapLarge.IO.CCFF;
using MapLarge.Common.ArrayPooling;
using MapLarge.GeoNumerics.Graphics;
using MapLarge.GeoNumerics.Text;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Global

namespace MapLarge.Engine.QueryEngine.Expressions;

public partial class ExpressionScope {
	private const string _toleranceDescription = "Amount of tolerance, between 0.0 and 500.0 in the unit of the column's CRS. This tolerance is multiplied by 1,000. Higher values will remove more points";

	[MethodDesc("Geo", "Create bounded box for polygon.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateBoundedBox(Nullable<ShapeSetDouble> polygon) {
		if (!polygon.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();
		return new(CreateBoundedBoxImpl(polygon.Value, polygon.Value.shapes));
	}
	[MethodDesc("Geo", "Create bounded box for polygon.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[AggregateFunction]
	public Nullable<ShapeSetDouble> CreateBoundedBox(IList<int> multiRow, ExpressionScopeDelegate<ShapeSetDouble> shapeFunc) {
		var shapes = multiRow.Select(row => shapeFunc(this, AggregatesQueryableIndex.GetRowStart(row)))
			.Where(ss => ss.Assigned && ss.Value.shapes.Length > 0)
			.Select(ss => ss.Value)
			.ToArray();
		return new(CreateBoundedBoxImpl(shapes, ss => ss.shapes));
	}
		
		[MethodDesc("Geo", "Provides the result set of points found inside a polygon subquery.")]
		public bool OverlapsAny(GeoPointDouble xy, IPlannerResult subquery) {
			var shapeSets =  (ShapeSetDouble[])subquery.TableData.Values.First(_ => true);
			return shapeSets.Any(shapeSet => shapeSet.InPoly(xy.X, xy.Y));
		}

		[MethodDesc("Geo", "Provides the result set of lines found inside a polygon subquery.")]
		public bool OverlapsAny(LineSetDouble lineSet, IPlannerResult subquery) {
			var shapeSets = (ShapeSetDouble[])subquery.TableData.Values.First();
			return shapeSets.Any(shapeSet => shapeSet.OverlapLine(lineSet));
		}

		[MethodDesc("Geo", "Provides the result set of polygons found inside a polygon subquery.")]
		public bool OverlapsAny(ShapeSetDouble shapeSet, IPlannerResult subquery) {
			return ((ShapeSetDouble[])subquery.TableData.Values.First()).Any(s => s.OverlapPoly(shapeSet));
		}
		
		[MethodDesc("Geo", "Create bounded box for lines.")]
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<ShapeSetDouble> CreateBoundedBox(Nullable<LineSetDouble> lineSet) {
			if (!lineSet.Assigned)
				return Nullable<ShapeSetDouble>.CreateNull();
			return new(CreateBoundedBoxImpl(lineSet.Value, lineSet.Value.lines));
		}
		
	[MethodDesc("Geo", "Create bounded box for lines.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateBoundedBox(Nullable<MultipointDouble> multipoint) {
		if (!multipoint.Assigned)
			return default;
		return new(CreateBoundedBoxImpl(multipoint.Value, new[] {multipoint.Value}));
	}
		
		
	[MethodDesc("Geo", "Create bounded box for lines.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[AggregateFunction]
	public Nullable<ShapeSetDouble> CreateBoundedBox(IList<int> multiRow, ExpressionScopeDelegate<LineSetDouble> lineFunc) {
		var lines = multiRow.Select(row => lineFunc(this, AggregatesQueryableIndex.GetRowStart(row)))
			.Where(ls => ls.Assigned && ls.Value.lines.Length > 0)
			.Select(ls => ls.Value)
			.ToArray();
		return new(CreateBoundedBoxImpl(lines, ls => ls.lines));
	}

	private ShapeSetDouble CreateBoundedBoxImpl<T, TSubType>(T geoData, TSubType[] subItems)
		where T : IGeoBounded
		where TSubType : IGeoBounded {
		if (geoData == null || !(subItems?.Length > 0))
			return ShapeSetDouble.Empty;

		double worldX = GoogleGeo.geo.getLngFromXPixel(GoogleGeo.geo.MAX_PIXELS_AT_ZOOM[Core.BaseZoomSetting], Core.BaseZoomSetting);
		double halfWorldX = GoogleGeo.geo.getLngFromXPixel(GoogleGeo.geo.MAX_PIXELS_AT_ZOOM[Core.BaseZoomSetting] / 2, Core.BaseZoomSetting);
		if (geoData.MaxX - geoData.MaxY >= halfWorldX) {
			var over = new List<TSubType>();
			var within = new List<TSubType>();
			//We have a bounded box that goes over the dateline
			foreach (var shape in subItems) {
				var bb = CreateBoundedBox(shape.MinX, shape.MaxX, shape.MinY, shape.MaxY);
				if (bb.MaxX - bb.MaxY >= halfWorldX)
					over.Add(shape);
				else
					within.Add(shape);
			}

			if (within.Any()) {
				var withinMinY = within.Min(s => s.MinY);
				var withinMaxY = within.Max(s => s.MaxY);

				var overBoundedBox = CreateBoundedBox(over.Min(s => s.MinX), worldX, withinMinY, withinMaxY);
				var withinBoundedBox = CreateBoundedBox(0, within.Max(s => s.MaxX), withinMinY, withinMaxY);

				var shapes = new List<ShapeDouble>();
				shapes.AddRange(overBoundedBox.shapes);
				shapes.AddRange(withinBoundedBox.shapes);
				return new(shapes.ToArray());
			}
		}

		return CreateBoundedBox(geoData.MinX, geoData.MaxX, geoData.MinY, geoData.MaxY);
	}
	private ShapeSetDouble CreateBoundedBoxImpl<T, TSubType>(IList<T> geoData, Func<T, TSubType[]> shapeFunc)
		where T : IGeoBounded
		where TSubType : IGeoBounded {
		if (geoData == null || !geoData.Any())
			return ShapeSetDouble.Empty;

		double worldX = GoogleGeo.geo.getLngFromXPixel(GoogleGeo.geo.MAX_PIXELS_AT_ZOOM[Core.BaseZoomSetting], Core.BaseZoomSetting);
		double halfWorldX = GoogleGeo.geo.getLngFromXPixel(GoogleGeo.geo.MAX_PIXELS_AT_ZOOM[Core.BaseZoomSetting] / 2, Core.BaseZoomSetting);
		if (geoData.Max(t => t.MaxX) - geoData.Max(t => t.MaxY) >= halfWorldX) {
			var over = new List<TSubType>();
			var within = new List<TSubType>();
			//We have a bounded box that goes over the dateline
			foreach (T item in geoData) {
				var subItems = shapeFunc(item);
				foreach (var shape in subItems) {
					var bb = CreateBoundedBox(shape.MinX, shape.MaxX, shape.MinY, shape.MaxY);
					if (bb.MaxX - bb.MaxY >= halfWorldX)
						over.Add(shape);
					else
						within.Add(shape);
				}
			}

			if (within.Any()) {
				var withinMinY = within.Min(s => s.MinY);
				var withinMaxY = within.Max(s => s.MaxY);

				var overBoundedBox = CreateBoundedBox(over.Min(s => s.MinX), worldX, withinMinY, withinMaxY);
				var withinBoundedBox = CreateBoundedBox(0, within.Max(s => s.MaxX), withinMinY, withinMaxY);

				var shapes = new List<ShapeDouble>();
				shapes.AddRange(overBoundedBox.shapes);
				shapes.AddRange(withinBoundedBox.shapes);
				return new(shapes.ToArray());
			}
		}

		return CreateBoundedBox(geoData.Min(t => t.MinX), geoData.Max(t => t.MaxX), geoData.Min(t => t.MinY), geoData.Max(t => t.MaxY));
	}

	public ShapeSetDouble CreateBoundedBox(double minX, double maxX, double minY, double maxY) => CreateBoundedBox(minX, maxX, minY, maxY, false);

	public ShapeSetDouble CreateBoundedBox(double minX, double maxX, double minY, double maxY, bool closed) {
		GeoPointDouble[] points = closed ? new GeoPointDouble[5] : new GeoPointDouble[4];
		points[0] = new(minX, maxY);
		points[1] = new(maxX, maxY);
		points[2] = new(maxX, minY);
		points[3] = new(minX, minY);
		if (closed)
			points[4] = new(minX, maxY);
		return new(new ShapeDouble[] {
			new(points)
		});
	}

	[MethodDesc("Geo", "Create a circle.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateCircle(
		[ParamDesc("Center point")] Nullable<GeoPointDouble> centerPoint,
		[ParamDesc("Diameter")] double diameter,
		[ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!centerPoint.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		diameter = ConvertUnits(diameter, unit, "m");
		return new Nullable<ShapeSetDouble>(PlotEllipse(centerPoint.Value, diameter, diameter, 0));
	}

	[MethodDesc("Geo", "Create a circle.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateCircleWithRadius(
		[ParamDesc("Center point")] Nullable<GeoPointDouble> centerPoint,
		[ParamDesc("Radius")] double radius,
		[ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {

		return CreateCircle(centerPoint, radius * 2, unit);
	}

	[MethodDesc("Geo", "Create an ellipse.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateEllipse(
		[ParamDesc("Center point")] Nullable<GeoPointDouble> centerPoint,
		[ParamDesc("Major axis length (width in meters)")] double majorAxisLength,
		[ParamDesc("Minor axis length (height in meters)")] double minorAxisLength,
		[ParamDesc("Degree of rotation (0-180)")] double orientation,
		[ParamDesc("Granularity of points on the ellipse (in degrees between points)")] double steps = 4,
		[ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!centerPoint.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		majorAxisLength = ConvertUnits(majorAxisLength, unit, "m");
		minorAxisLength = ConvertUnits(minorAxisLength, unit, "m");

		return new Nullable<ShapeSetDouble>(PlotEllipse(centerPoint.Value, majorAxisLength, minorAxisLength, orientation,steps));
	}

	[MethodDesc("Geo", "Create a circle sector.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateSector([ParamDesc("Center point")] Nullable<GeoPointDouble> centerPoint,
		[ParamDesc("Diameter (in meters)")] Nullable<double> diameter,
		[ParamDesc("Degree of rotation (0-360)")] Nullable<double> orientation,
		[ParamDesc("Angle (in degrees)")] Nullable<double> angle,
		[ParamDesc("Number of degrees per step")] double steps = 4,
		[ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m",
		[ParamDesc("Great circle resolution (0 to disable)")] int greatCircleResolution = 0) {
		if (!centerPoint.Assigned || !diameter.Assigned || !orientation.Assigned || !angle.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		var d = ConvertUnits(diameter.Value, unit, "m");
		return new(PlotEllipse(centerPoint.Value, d, d, orientation.Value, steps, angle.Value, greatCircleResolution));
	}

	private ShapeSetDouble PlotEllipse(GeoPointDouble centerPoint, double majorAxisLengthMeters, double minorAxisLengthMeters, double orientation, double steps = 4, double sectorAngle = 360, int greatCircleSectorResolution = 0) {
		// This can be an very hot allocation path, the seeming verbostiy of this code is to carefully control when we allocate (and pool as many allocations as possible)
		WrappedArray<PointDouble> array = null;
		var pointCount = EllipseParser.ConvertToPolygon(
			size => array = EllipseParser.PointDoublePool.Rent("PlotEllipse", size),
			new GeoPointDouble(centerPoint.X, centerPoint.Y), 
			majorAxisLengthMeters, 
			minorAxisLengthMeters, 
			orientation, 
			steps, 
			sectorAngle,
			greatCircleSectorResolution);

		bool didNormalize = GeoFactory.NormalizeShapeIfNeeded(array, pointCount, ProjectionMode.EPSG_4326, false, true, false, out var shapes);
		if (didNormalize) {
			var sd = new ShapeDouble[shapes.Count];
			for (int i = 0; i < sd.Length; i++) {
				sd[i] = ShapeDouble.PopulateShape(shapes[i].Select(p => new GeoPointDouble(p.X, p.Y)).ToArray(), false, null, false, centerPoint.X, centerPoint.Y);
			}
			array.Dispose();
			return new ShapeSetDouble(sd);
		} else {
			var sd = new ShapeDouble[1];
			var gpd = new GeoPointDouble[pointCount];
			for (int i = 0; i < gpd.Length; i++) {
				gpd[i] = new GeoPointDouble(array[i].X, array[i].Y);
			}
			sd[0] = ShapeDouble.PopulateShape(gpd, false, null, false, centerPoint.X, centerPoint.Y);
			array.Dispose();
			return new ShapeSetDouble(sd);
		}
	}

	[MethodDesc("Geo", "Calculates the distance between two points. By default the result is in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> DistanceBetween([ParamDesc("First point")] Nullable<GeoPointDouble> p1, [ParamDesc("Second point")] Nullable<GeoPointDouble> p2, [ParamDesc("Unit of measurement: M (Meters), K (Kilometers), or N (Nautical miles)")] string unit = "K") {
		if (!p1.Assigned || !p2.Assigned) return default;
		return new Nullable<double>(Measure.Distance(p1.Value.Y, p1.Value.X, p2.Value.Y, p2.Value.X, unit[0]));
	}


	[MethodDesc("Geo", "Calculates the minimum distance between two multipoints. By default the result is in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> DistanceBetween([ParamDesc("First multipoint")]Nullable<MultipointDouble> mp1, [ParamDesc("Second multipoint")]Nullable<MultipointDouble> mp2, 
		[ParamDesc("Unit of measurement: M (Meters), K (Kilometers), or N (Nautical miles)")]string unit = "K") {
		if (!mp1.Assigned || !mp2.Assigned) return default;

		double minimumDistance = double.MaxValue;
		foreach (GeoPointDouble p1 in mp1.Value.Points) {
			foreach (GeoPointDouble p2 in mp2.Value.Points) {
				double distance = Measure.Distance(p1.Y, p1.X, p2.Y, p2.X, unit[0]);
				if (distance < minimumDistance) {
					minimumDistance = distance;
				}
			}
		}

		return new Nullable<double>(minimumDistance);
	}

	[MethodDesc("Geo", "Calculates the minimum distance between a multipoint and a point. By default the result is in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> DistanceBetween([ParamDesc("Multipoint")]Nullable<MultipointDouble> mp, [ParamDesc("Point")]Nullable<GeoPointDouble> p, 
		[ParamDesc("Unit of measurement: M (Meters), K (Kilometers), or N (Nautical miles)")]string unit = "K") {
		if (!p.Assigned || !mp.Assigned) return default;

		double minimumDistance = double.MaxValue;
		GeoPointDouble p1 = p.Value;
		foreach (GeoPointDouble p2 in mp.Value.Points) {
			double distance = Measure.Distance(p1.Y, p1.X, p2.Y, p2.X, unit[0]);
			if (distance < minimumDistance) {
				minimumDistance = distance;
			}
		}

		return new Nullable<double>(minimumDistance);
	}

	/// <summary>
	/// Same as the previous method, but the order of the first two parameters are swapped
	/// </summary>
	/// <param name="p"></param>
	/// <param name="mp"></param>
	/// <param name="unit"></param>
	/// <returns></returns>
	[MethodDesc("Geo", "Calculates the minimum distance between a point and a multipoint. By default the result is in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> DistanceBetween([ParamDesc("Point")]Nullable<GeoPointDouble> p, [ParamDesc("Multipoint")]Nullable<MultipointDouble> mp, 
		[ParamDesc("Unit of measurement: M (Meters), K (Kilometers), or N (Nautical miles)")]string unit = "K") {
		return DistanceBetween(mp, p, unit);
	}

	[MethodDesc("Geo", "Determines if a GeoPoint is within a polygon.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public bool InPoly([ParamDesc("First point")] Nullable<GeoPointDouble> p1, [ParamDesc("Second point")] ShapeSetDouble p2, [ParamDesc("Unit of measurement: M (Meters), K (Kilometers), or N (Nautical miles)")] string unit = "K") {
		if (!p1.Assigned || p2 == null) return default;
		return p2.InPoly(null, -1, p1.Value.X, p1.Value.Y);
	}
		
	private ShapeSetDouble AddDistanceToCircle(string shapeDefinition, string project, string originalProjection, int distance) {
		string[] circlePieces = shapeDefinition.Trim().Split(',');
		string[] pointPieces = circlePieces[0].Trim().Split(' ');
		double lat = Double.Parse(pointPieces[1]);
		double lng = Double.Parse(pointPieces[0]);
		double radius = Double.Parse(circlePieces[1]) + distance;

		bool crossedNPole, crossedSPole;
		double boundaryY;
		List<PointDouble[]> circlePoints1;

		var projection = project switch {
			"REPROJECT" => ProjectionMode.REPROJECT,
			_ => ProjectionMode.EPSG_4326
		};

		var projectionData = string.IsNullOrWhiteSpace(originalProjection)
			? IsoProjectionData.GetWgs84Data()
			: IsoProjectionData.GetData(_core?.ProjFacade.ProjPaths, originalProjection);

		if (!projectionData.IsWgs84 && projection == ProjectionMode.REPROJECT) {
			circlePoints1 = GeoFactory.GetPolypointsFromMercatorCircle(_core, lat, lng, radius, projectionData.Key, out crossedNPole, out crossedSPole, out boundaryY);
		} else {
			circlePoints1 = GeoFactory.GetPolypointsFromCircle(lat, lng, radius, out crossedNPole, out crossedSPole, out boundaryY);
		}
		var shapePointsCircle = projection == ProjectionMode.REPROJECT ? circlePoints1 : GeoFactory.NormalizeShape(circlePoints1, projection, isLine: false, needsSplit: false);
		var hats = GeoFactory.MakePolarHatsCircle(new PointF((float)lng, (float)lat), boundaryY, crossedNPole, crossedSPole);

		var shapes1 = shapePointsCircle.Select(p => new ShapeDouble(p.ToList()))
			.Concat(hats.Select(h => new ShapeDouble(h))).ToArray();
		var shapeSetDouble = new ShapeSetDouble(shapes1);

		return shapeSetDouble;
	}
	[MethodDesc("Geo", "Based on the required projection, converts the coordinates to WGS84 in order to properly add the distance, then converts back before returning.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble AddDistanceToPolygon(string shapeDoubles, string project, string originalProjection, double distanceInMeters, string shapeType) {
		List<Shape> bufferShapes = new List<Shape>();
		List<ShapeDouble> bufferShapesDouble = new List<ShapeDouble>();
		PointDouble[] linePoints;
		IEnumerable<List<PointDouble>> hats;
		shapeType = shapeType.ToLower();

		PointF previousPoint = new PointF { X = -1, Y = -1 };
		bool omitEndcap = shapeType != "linestring";

		var projection = project switch {
			"REPROJECT" => ProjectionMode.REPROJECT,
			_ => ProjectionMode.EPSG_4326,
		};

		var projectionData = string.IsNullOrWhiteSpace(originalProjection)
			? IsoProjectionData.GetWgs84Data()
			: IsoProjectionData.GetData(_core?.ProjFacade.ProjPaths, originalProjection);

		string[] lineDoublets = shapeDoubles.Split(',');
		linePoints = new PointDouble[lineDoublets.Length];

		for (int i = 0; i < lineDoublets.Length; i++) {
			string[] coordChunks = lineDoublets[i].Trim().Split(' ');
			PointDouble linePtDouble = new PointDouble { X = Double.Parse(coordChunks[0]), Y = Double.Parse(coordChunks[1]) };
			linePoints[i] = linePtDouble;

			PointF linePt = new PointF { X = (float)linePtDouble.X, Y = (float)linePtDouble.Y };

			if (previousPoint.X != -1) {
				bool crossedSPole;
				var crossedNPole = crossedSPole = false;

				PointF[][] segmentBufferShapes;

				if (projection == ProjectionMode.REPROJECT && !projectionData.IsWgs84) {
					segmentBufferShapes = Geo.Buffer.BufferUtil.CalculateMercatorBuffer(this._core, previousPoint, linePt, distanceInMeters, omitEndcap, false,
						projection, out crossedNPole, out crossedSPole, projectionData.Key);
				} else {
					segmentBufferShapes = Geo.Buffer.BufferUtil.CalculateBuffer(previousPoint, linePt, distanceInMeters, omitEndcap, false,
						projection, out crossedNPole, out crossedSPole);
				}

				for (int j = 0; j < segmentBufferShapes.Length; j++) {
					PointF[] singleBufferShape = segmentBufferShapes[j];
					PointDouble[] bufferPoints = new PointDouble[singleBufferShape.Length];
					for (int k = 0; k < singleBufferShape.Length; k++) {
						bufferPoints[k] = new PointDouble { X = singleBufferShape[k].X, Y = singleBufferShape[k].Y };
					}

					List<PointDouble[]> normShapes = null;
					if (projection == ProjectionMode.REPROJECT) {
						normShapes = new List<PointDouble[]> { bufferPoints };
					} else {
						normShapes = GeoFactory.NormalizeShape(bufferPoints, projection, isLine: false, needsSplit: true);
					}

					foreach (var shapePoints in normShapes) {
						bufferPoints = shapePoints;
						if (projection == ProjectionMode.EPSG_4326 || projection == ProjectionMode.REPROJECT) {
							//bufferPoints = InterpolateLLPoints(shapePoints, shapeType == "polygon");
							bufferShapesDouble.Add(new ShapeDouble(bufferPoints.ToList()));
						} else {
							bufferShapes.Add(new Shape(bufferPoints.ToList(), Core.BaseZoomSetting));
						}
					}

					if (crossedNPole || crossedSPole) {
						hats = GeoFactory.MakePolarHats(linePt, distanceInMeters, crossedNPole, crossedSPole)
							.Concat(GeoFactory.MakePolarHats(previousPoint, distanceInMeters, crossedNPole, crossedSPole));

						foreach (var hat in hats) {
							if (projection == ProjectionMode.EPSG_4326 || projection == ProjectionMode.REPROJECT)
								bufferShapesDouble.Add(new ShapeDouble(hat));
							else
								bufferShapes.Add(new Shape(hat, Core.BaseZoomSetting));
						}
					}

					omitEndcap = true;
				}
			}
			previousPoint = linePt;
		}
		// test contains inside of the polygon itself too. since, if it's inside, then it's within.
		if (shapeType == "polygon") {
			List<PointDouble[]> normShapes = null;
			if (projection == ProjectionMode.REPROJECT) {
				normShapes = new() { linePoints };
			} else {
				normShapes = GeoFactory.NormalizeShape(linePoints, projection, isLine: false, needsSplit: true);
			}
			foreach (var shapePoints in normShapes) {
				linePoints = shapePoints;
				if (projection == ProjectionMode.EPSG_4326 || projection == ProjectionMode.REPROJECT) {
					//linePoints = InterpolateLLPoints(shapePoints, true);
					bufferShapesDouble.Add(new ShapeDouble(linePoints.ToList()));
				} else {
					bufferShapes.Add(new Shape(linePoints.ToList(), Core.BaseZoomSetting));
				}
			}
		}

		return new ShapeSetDouble(bufferShapesDouble.ToArray(), HoleMode.None, false);
	}
	public PointF[][] CalculateBuffer(PointF endpoint1, PointF endpoint2, double distanceInMeters, bool omitFirstEndcap,
		bool clampLng, ProjectionMode projection, out bool crossedNPole, out bool crossedSPole) {
		// grows as square of radius because when we get to the poles, inaccuracy becomes visible very quickly
		int numPoints = Math.Max(18, (int)(distanceInMeters * distanceInMeters / 1e11));

		Angle course = GeometryUtil.CalculateCourse2D(endpoint1, endpoint2, projection);

		PointF[] boundingRect = new PointF[4];
		PointF[] boundingCircle1 = new PointF[numPoints];
		PointF[] boundingCircle2 = new PointF[numPoints];

		var distNPole = Measure.HaversineDistance(endpoint1.Y, endpoint1.X, 90, 0); // N pole
		var distSPole = Measure.HaversineDistance(endpoint1.Y, endpoint1.X, -90, 0); // S pole
		crossedNPole = distNPole < distanceInMeters;
		crossedSPole = distSPole < distanceInMeters;
		if (!crossedNPole) {
			distNPole = Measure.HaversineDistance(endpoint2.Y, endpoint2.X, 90, 0); // N pole
			crossedNPole = distNPole < distanceInMeters;
		}
		if (!crossedSPole) {
			distSPole = Measure.HaversineDistance(endpoint2.Y, endpoint2.X, -90, 0); // S pole
			crossedSPole = distSPole < distanceInMeters;
		}

		boundingRect[0] = GeometryUtil.CalculateNewLatLng(endpoint1, course, distanceInMeters, clampLng);
		boundingRect[1] = GeometryUtil.CalculateNewLatLng(endpoint1, course + Angle.DEGREES_180, distanceInMeters, clampLng);
		boundingRect[2] = GeometryUtil.CalculateNewLatLng(endpoint2, course + Angle.DEGREES_180, distanceInMeters, clampLng);
		boundingRect[3] = GeometryUtil.CalculateNewLatLng(endpoint2, course, distanceInMeters, clampLng);

		// calc two circles
		for (int i = 0; i < numPoints; i++) {
			Angle angle = Angle.FromDegrees(i * 360.0 / numPoints);
			boundingCircle1[i] = GeometryUtil.CalculateNewLatLng(endpoint1, angle, distanceInMeters, clampLng);
			boundingCircle2[i] = GeometryUtil.CalculateNewLatLng(endpoint2, angle, distanceInMeters, clampLng);
		}

		if (omitFirstEndcap) {
			return new PointF[2][] { boundingRect, boundingCircle2 };
		}
		return new PointF[3][] { boundingCircle1, boundingRect, boundingCircle2 };
	}
	[MethodDesc("Geo", "Offset the specified point by the specified distance and heading.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> Offset([ParamDesc("Starting point")] Nullable<GeoPointDouble> point, [ParamDesc("Distance in meters")] double distance, [ParamDesc("Heading in degrees")] double heading) {
		if (!point.Assigned)
			return Nullable<GeoPointDouble>.CreateNull();

		return new Nullable<GeoPointDouble>(Offset(point.Value, distance, Angle.FromDegrees(heading)));
	}

	[MethodDesc("Geo", "Offset the specified shape by the specified distance and heading.")]
	public Nullable<ShapeSetDouble> Offset([ParamDesc("Starting shape")] Nullable<ShapeSetDouble> shape, [ParamDesc("Distance in meters")] double distance, [ParamDesc("Heading in degrees")] double heading) {
		if (!shape.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		var shapeValue = shape.Value;
		var headingAngle = Angle.FromDegrees(heading);
		var newShapes = new ShapeDouble[shapeValue.shapes.Length];
		for (int i = 0; i < newShapes.Length; i++) {
			var oldShapePoints = shapeValue.shapes[i].Points;
			var newPoints = new GeoPointDouble[oldShapePoints.Length];
			for (int j = 0; j < newPoints.Length; j++) {
				newPoints[j] = Offset(oldShapePoints[j], distance, headingAngle);
			}
			newShapes[i] = new ShapeDouble(newPoints) {
				isHole = shapeValue.shapes[i].isHole
			};
		}
		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(newShapes));
	}

	[MethodDesc("Geo", "Returns the area of the shape, in km².")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public double Area(ShapeSetDouble geoObj) {
		var kmArea = geoObj.GetAreaInKmSquared();
		return kmArea;
	}

	[MethodDesc("Geo", "Buffers line by the given distance.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public Nullable<ShapeSetDouble> Buffer(
		[ParamDesc("Line to buffer")] Nullable<LineSetDouble> line,
		[ParamDesc("Distance to buffer, in meters")] Nullable<double> radius,
		[ParamDesc("Use a pixel-based buffer method (optional, default false)")] bool forceRaster = false
	) {
		if (!line.Assigned || !radius.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();
		var points = line.Value.lines.Select(p => p.Points);
        var ssd = Geo.Buffer.BufferUtil.BufferLine(points, radius.Value, true);
        return new Nullable<ShapeSetDouble>(ssd);
	}

	[MethodDesc("Geo", "Buffers shape by the given distance.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> Buffer(
		[ParamDesc("Shape to buffer")] Nullable<ShapeSetDouble> shape,
		[ParamDesc("Distance to buffer, in meters")] Nullable<double> radius,
		[ParamDesc("Use a pixel-based buffer method (optional, default false)")] bool forceRaster = false
	) {
		if (!shape.Assigned || !radius.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();
		var points = shape.Value.shapes.Select(s => s.Points);
		var ssd = Geo.Buffer.BufferUtil.BufferShapes(points, radius.Value, shape.Value, forceRaster, true);
		return new Nullable<ShapeSetDouble>(ssd);
	}

	[MethodDesc("Geo", "Buffers line by the given distance, without any end caps extending past the first and last points of the line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> BufferWithoutEndcaps([ParamDesc("Line to buffer")] Nullable<LineSetDouble> line, [ParamDesc("Distance to buffer, in meters")] Nullable<double> radius) {
        if (!line.Assigned || !radius.Assigned)
            return Nullable<ShapeSetDouble>.CreateNull();
        var points = line.Value.lines.Select(p => p.Points);
		var ssd = Geo.Buffer.BufferUtil.BufferLine(points, radius.Value, false);
		return new Nullable<ShapeSetDouble>(ssd);
	}

	[MethodDesc("Geo", "Buffers shape by the given distance.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble BufferApprox(
		[ParamDesc("Shape to buffer")] ShapeSetDouble shapeSet,
		[ParamDesc("Distance to buffer, in meters")] double radius
	) {
		List<ShapeDouble> returnShapes = new List<ShapeDouble>();

		foreach (var oneshape in shapeSet.shapes) {
			try {
				if (oneshape.isHole)
					continue;

				var shape = new ShapeSetDouble(new[] { oneshape.DeepClone() });
				double[] x, y;
				var centroid = shape.GetCentroidLL();

				var area = shape.GetAreaInKmSquared();
				if (area < 0.01) {
					var centerPt = new GeoPointDouble(centroid.X, centroid.Y);
					var circle = CreateCircle(new Nullable<GeoPointDouble>(centerPt), radius * 2);
					returnShapes.Add(circle.Value.shapes[0]);
					continue;
				}

				int shift = 2 * (HashSetMorton.START_ZOOM_SEARCH + 1);
				var hashSetFactory = new HashSetMortonFactory(true);
				var bbox = new BoundingBoxDouble(shape).Expand(radius);
				var regions = new Stack<(int, IEnumerable<HashSetMorton.MortonWithOverlap>)>();
				int targetCells = area < 1 ? 10_000 : 30_000;

				do {
					shift -= 2;
					var shiftRegions = shape.GetRegionMorton(hashSetFactory, bbox, shift, radius);
					regions.Push((shift, shiftRegions));
				} while (regions.Peek().Item2.Count() < targetCells && shift > 6);

				hashSetFactory = null;
				if (regions.Peek().Item2.Count() > 100_000)
					regions.Pop();

				// put points on region
				var region = regions.Pop();
				regions = null;

				var gps = new ConcurrentBag<GeoPointDouble>();
				foreach (var morton in region.Item2) {
					if (morton.Overlap == OverlapValue.In) {
						MortonFixedPoint.Corners(morton.Morton, region.Item1, out x, out y);
						double xmid = x[0] / 2 + x[2] / 2;
						double ymid = y[0] / 2 + y[2] / 2;
						var gp = new GeoPointDouble(xmid, ymid);
						gps.Add(gp);
					}
				}

				System.Threading.Tasks.Parallel.ForEach(region.Item2, Util.ParallelOpt(), (morton) => {
					if (morton.Overlap == OverlapValue.Indeterminate) {
						MortonFixedPoint.Corners(morton.Morton, region.Item1, out x, out y);
						double xmid = x[0] / 2 + x[2] / 2;
						double ymid = y[0] / 2 + y[2] / 2;
						if (shape.DistanceToShapeSetWithin(xmid, ymid, radius))
							gps.Add(new GeoPointDouble(xmid, ymid));
					}
				});

				// compute alpha size
				ulong center = (MortonFixedPoint.InterleaveMagicNumber(centroid.X, centroid.Y) >> region.Item1) << region.Item1;
				MortonFixedPoint.Corners(center, region.Item1, out x, out y);
				// 3x is a fudge factor that makes the resulting shape look smoother
				var alpha_size = 3 * Measure.HaversineDistance(y[0], x[0], y[2], x[2]);

				// convert points to hull
				var xydc = new XYDataColumn(gps.ToArray());
				xydc.SetValidVersions(new RowVersions());
				var inGeom = JShape.JShape.LoadTablePoints((XYColumn)(xydc.CreateVersioned(null)));
				JShape.JShape hullGen = new JShape.JShape();

				Delaunator delaunator = null;
					
				Shape[] shapes = new Shape[0];
				if (inGeom.Count() >= 3) {
					try {
						delaunator = new Delaunator(inGeom.ToArray());
					} catch (Exception) {
						// if all the points are co-linear, Triangle.NET throws
						// skip the shape
					}
				} else {
					// skip the shape
				}

				var hull = JShape.JShape.ApplyAlphaShapes(delaunator, alpha_size);
				var hullShapes = hullGen.HullToShapeSet(hull);
				returnShapes.AddRange(hullShapes.shapes);
			} catch (Exception ex) {
				_core.Log.Log(LoggableModule.expression, "BufferApprox exception", ex);
			}
		}

		if (returnShapes.Count > 1) {
			// union all the individually buffered shapes
			var clipper = new ClipLib.Clipper();
			List<ClipLib.GeoPoint> list = new List<ClipLib.GeoPoint>();
			int k = 0;
			foreach (var s in returnShapes) {
				list.Clear();
				foreach(var pt in s.Points) {
					var cpt = new ClipLib.GeoPoint(pt.X * 1_000_000_000, pt.Y * 1_000_000_000);
					list.Add(cpt);
				}
				var polyType = k == 0 ? ClipLib.PolyType.ptSubject : ClipLib.PolyType.ptClip;
				clipper.AddPath(list, polyType, true);
				k++;
			}

			var solution = new List<List<ClipLib.GeoPoint>>();
			clipper.Execute(ClipLib.ClipType.ctUnion, solution, ClipLib.PolyFillType.pftNonZero, ClipLib.PolyFillType.pftNonZero);
			var shapeArray = new ShapeDouble[solution.Count];
			for (int i = 0; i < solution.Count; i++) {
				var pts = solution[i].Select(ipt => new GeoPointDouble(ipt.x / 1_000_000_000.0, ipt.y / 1_000_000_000.0)).ToArray();
				var s = ShapeDouble.PopulateShape(pts);
				shapeArray[i] = s;
			}
			return new ShapeSetDouble(shapeArray);
		} else {
			return new ShapeSetDouble(returnShapes.ToArray());
		}
	}

	private GeoPointDouble Offset(GeoPointDouble point, double distance, Angle heading) {
		GeometryUtil.CalculateNewLatLng(point.Y, point.X, heading, distance, true, out var latEnd, out var lngEnd);
		return new GeoPointDouble(lngEnd, latEnd);
	}

	[MethodDesc("Geo", "Offset the specified line by the specified distance and heading (applied to each point on the line).")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> Offset([ParamDesc("Starting point")] Nullable<LineSetDouble> line, [ParamDesc("Distance in meters")] double distance, [ParamDesc("Heading in degrees")] double heading) {
		if (!line.Assigned)
			return Nullable<LineSetDouble>.CreateNull();

		var lineValue = line.Value;
		var headingAngle = Angle.FromDegrees(heading);
		var newLines = new LineDouble[lineValue.lines.Length];
		for (int i = 0; i < newLines.Length; i++) {
			var oldLinePoints = lineValue.lines[i].Points;
			var newPoints = new GeoPointDouble[oldLinePoints!.Length];
			for (int j = 0; j < newPoints.Length; j++) {
				newPoints[j] = Offset(oldLinePoints[j], distance, headingAngle);
			}
			newLines[i] = new LineDouble(newPoints);
		}
		return new Nullable<LineSetDouble>(new LineSetDouble(newLines));
	}

	[MethodDesc("Geo", "Returns true if the two points fall within the given radius of each other.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin([ParamDesc("First point")] Nullable<GeoPointDouble> point1, [ParamDesc("Second point")] Nullable<GeoPointDouble> point2, [ParamDesc("Radius size, in meters")] double radius) {
		Nullable<double> d = DistanceBetween(point1, point2, "M");
		return !d.Assigned ? default : new Nullable<bool>(d.Value <= radius);
	}

	[MethodDesc("Geo", "Returns true if the two points fall within the given radius of each other.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin([ParamDesc("First multipoint")] Nullable<MultipointDouble> multipoint1, [ParamDesc("Second multipoint")] Nullable<MultipointDouble> multipoint2, [ParamDesc("Radius size, in meters")] double radius) {
		Nullable<double> d = DistanceBetween(multipoint1, multipoint2, "M");
		return !d.Assigned ? default : new Nullable<bool>(d.Value <= radius);
	}

	[MethodDesc("Geo", "Returns true if the two points fall within the given radius of each other.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin([ParamDesc("Point")] Nullable<GeoPointDouble> point, [ParamDesc("Multipoint")] Nullable<MultipointDouble> multipoint, [ParamDesc("Radius size, in meters")] double radius) {
		Nullable<double> d = DistanceBetween(multipoint, point, "M");
		return !d.Assigned ? default : new Nullable<bool>(d.Value <= radius);
	}

	[MethodDesc("Geo", "Returns true if the two points fall within the given radius of each other.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin([ParamDesc("Multipoint")] Nullable<MultipointDouble> multipoint, [ParamDesc("Point")] Nullable<GeoPointDouble> point, [ParamDesc("Radius size, in meters")] double radius) {
		Nullable<double> d = DistanceBetween(multipoint, point, "M");
		return !d.Assigned ? default : new Nullable<bool>(d.Value <= radius);
	}

	[MethodDesc("Geo", "Returns true if the point falls within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsNotWithin(Nullable<ShapeSetDouble> shape, Nullable<GeoPointDouble> point) {
		if (!shape.Assigned || !point.Assigned)
			return default;
		return new Nullable<bool>(!shape.Value.InPoly(point.Value.X, point.Value.Y));
	}

	[MethodDesc("Geo", "Returns true if the multipoint does not fall completely within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsNotWithin(Nullable<ShapeSetDouble> shape, Nullable<MultipointDouble> multipoint) {
		if (!shape.Assigned || !multipoint.Assigned)
			return default;

		foreach (GeoPointDouble point in multipoint.Value.Points) {
			if (!shape.Value.InPoly(point.X, point.Y)) return new Nullable<bool>(true);
		}
		return new Nullable<bool>(false);
	}
		
	[MethodDesc("Geo", "Returns true if the point falls within a provided distance from the provided shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<ShapeSetDouble> shape, Nullable<GeoPointDouble> point, [ParamDesc("Buffer size, in meters")]Nullable<double> buffer) {
		if (!shape.Assigned || !point.Assigned)
			return default;
		return new Nullable<bool>(shape.Value.DistanceFromShapeSet(point.Value) <= buffer.Value);
	}
		
	[MethodDesc("Geo", "Returns true if the point falls within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<ShapeSet> shape, Nullable<GeoPoint> point) {
		if (!shape.Assigned || !point.Assigned)
			return default;
		if (shape.Value.InPoly(point.Value.X, point.Value.Y)) {
			return new Nullable<bool>(true);
		} else {
			return new Nullable<bool>(false);
		}
	}
	
	[MethodDesc("Geo", "Returns true if the point falls within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin([Projected]Nullable<ShapeSetDouble> shape, [Projected]Nullable<GeoPointDouble> point) {
		if (!shape.Assigned || !point.Assigned)
			return default;
		if (shape.Value.InPoly(point.Value.X, point.Value.Y)) {
			return new Nullable<bool>(true);
		} else {
			return new Nullable<bool>(false);
		}
	}	
		
	[MethodDesc("Geo", "Returns true if the shape overlaps the provided lines.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<ShapeSetDouble> drawnPoly, Nullable<LineSetDouble> columnLines) {
		if (!drawnPoly.Assigned || !columnLines.Assigned)
			return default;
		if (columnLines.Value.Lines.Any(line => line.Points!.Any(t => !drawnPoly.Value.InPoly(t.X, t.Y)))) {
			return new Nullable<bool>(false);
		}
		return new Nullable<bool>(!drawnPoly.Value.OverlapLine(columnLines.Value, false)); // check edge overlap only
	}
		
	[MethodDesc("Geo", "Returns true if the column shapes fall within the drawn polygon.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<ShapeSetDouble> drawnPoly, Nullable<ShapeSetDouble> columnShapes) {
		if (!drawnPoly.Assigned || !columnShapes.Assigned)
			return default;
			
		if( drawnPoly.Value.InPoly(columnShapes.Value.MinX, columnShapes.Value.MinY) &&
			drawnPoly.Value.InPoly(columnShapes.Value.MaxX, columnShapes.Value.MinY) &&
			drawnPoly.Value.InPoly(columnShapes.Value.MaxX, columnShapes.Value.MaxY) &&
			drawnPoly.Value.InPoly(columnShapes.Value.MinX, columnShapes.Value.MaxY) &&
			!BoundingBoxOverlap(drawnPoly.Value, columnShapes.Value)) {
			return new Nullable<bool>(true);
		}

		return columnShapes.Value.Shapes.Any(shape => shape.Points!.Any(point => !drawnPoly.Value.InPoly(point.X, point.Y))) ? 
			new Nullable<bool>(false) : 
			new Nullable<bool>(!drawnPoly.Value.OverlapPoly(columnShapes.Value, false));
	}
		
	private bool BoundingBoxOverlap(ShapeSetDouble drawnPoly, IGeoBounded columnShapes) {			
		return drawnPoly.OverlapLine(columnShapes.MinX, columnShapes.MinY, columnShapes.MaxX, columnShapes.MinY) ||
			   drawnPoly.OverlapLine(columnShapes.MaxX, columnShapes.MinY, columnShapes.MaxX, columnShapes.MaxY) ||
			   drawnPoly.OverlapLine(columnShapes.MaxX, columnShapes.MaxY, columnShapes.MinX, columnShapes.MaxY) ||
			   drawnPoly.OverlapLine(columnShapes.MinX, columnShapes.MaxY, columnShapes.MinX, columnShapes.MinY);
	}

	[MethodDesc("Geo", "Returns true if the lines do not fall within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsNotWithin(Nullable<ShapeSetDouble> drawnPoly, Nullable<LineSetDouble> lines) {
		if (!drawnPoly.Assigned || !lines.Assigned)
			return default;
		if (drawnPoly.Value.InPoly(lines.Value.MinX, lines.Value.MinY) &&
			drawnPoly.Value.InPoly(lines.Value.MaxX, lines.Value.MinY) &&
			drawnPoly.Value.InPoly(lines.Value.MaxX, lines.Value.MaxY) &&
			drawnPoly.Value.InPoly(lines.Value.MinX, lines.Value.MaxY) &&
			!BoundingBoxOverlap(drawnPoly.Value, lines.Value)) {
			return new Nullable<bool>(false);
		}
		return new Nullable<bool>(true);
	}
		
	[MethodDesc("Geo", "Returns true if the column shapes do not fall within the drawn polygon.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsNotWithin(Nullable<ShapeSetDouble> drawnPoly, Nullable<ShapeSetDouble> columnShapes) {
		if (!drawnPoly.Assigned || !columnShapes.Assigned)
			return default;
			
		return new Nullable<bool>(!IsWithin(drawnPoly, columnShapes).Value);
	}

	[MethodDesc("Geo", "Returns true if the multipoint completely falls within the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<ShapeSetDouble> shape, Nullable<MultipointDouble> multipoint) {
		if (!shape.Assigned || !multipoint.Assigned)
			return Nullable<bool>.CreateNull();

		foreach (GeoPointDouble point in multipoint.Value.Points) {
			if (!shape.Value.InPoly(point.X, point.Y)) return new Nullable<bool>(false);
		}
		return new Nullable<bool>(true);
	}

	#region travel paths
	[MethodDesc("Geo", "Returns the perimeter length of the shape or the length of the line, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public double Length(IGeoType geoObj) {
		var kmLength = geoObj.GetLength();

		return kmLength;
	}

	[MethodDesc("Geo", "Returns the completed distance along the specified path.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> CompletedDistanceAlongPath(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> line) {
		if (!line.Assigned || !point.Assigned)
			return default;
		var progress = GetProgressAlongLine(line.Value, point.Value);
		return new Nullable<double>(progress.traveledDistance);
	}

	[MethodDesc("Geo", "Returns the remaining distance along the specified path.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> RemainingDistanceAlongPath(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> line) {
		if (!line.Assigned || !point.Assigned)
			return default;
		var progress = GetProgressAlongLine(line.Value, point.Value);
		return new Nullable<double>(progress.totalDistance - progress.traveledDistance);
	}

	[MethodDesc("Geo", "Returns the fractional distance completed along the specified path.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> CompletedFractionAlongPath(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> line) {
		if (!line.Assigned || !point.Assigned) return default;
		var progress = GetProgressAlongLine(line.Value, point.Value);
		return new Nullable<double>(progress.percentage);
	}

	[MethodDesc("Geo", "Returns the expected time to reach the end of the path.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> ExpectedTimeRemainingAlongPath(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> line, int expectedTotalTime) {
		if (!line.Assigned || !point.Assigned) return default;
		var progress = GetProgressAlongLine(line.Value, point.Value);
		return new Nullable<double>(expectedTotalTime - (expectedTotalTime * progress.percentage));
	}

	[MethodDesc("Geo", "Returns the difference between the expected time at this point and the actual time at this point (how far behind or ahead we are).")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> ElapsedTimeDifferenceAlongPath(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> line, int expectedTotalTime, DateTimeOffset startTime, DateTimeOffset currentTime) {
		if (!line.Assigned || !point.Assigned) return default;
		var progress = GetProgressAlongLine(line.Value, point.Value);
		var elapsed = (currentTime - startTime).TotalSeconds;
		return new Nullable<double>(expectedTotalTime * progress.percentage - elapsed);
	}

	// This seems like a plausible but hacky optimization for sql selects that contain multiple travel path expressions on the same row / columns
	// it depends on two main assumptions:
	//   (1) Expressions are executed on a row sequentially in the same thread
	//   (2) line and point are basically immutable
	// A better approach might be to make GetProgressAlongLine an expression function, and have the query pipeline support sub-expression factoring
	[ThreadStatic] private static LineSetDouble _lastTravelPathLineSet;
	[ThreadStatic] private static GeoPointDouble _lastTravelPathPoint;
	[ThreadStatic] private static LineSet.LineProgress _lastProgress;
	private static LineSet.LineProgress GetProgressAlongLine(LineSetDouble line, GeoPointDouble point) {
		if (ReferenceEquals(line, _lastTravelPathLineSet) && Equals(point, _lastTravelPathPoint))
			return _lastProgress;

		_lastTravelPathPoint = point;
		_lastTravelPathLineSet = line;
		_lastProgress = line.ProgressAlongLine(point.X, point.Y);

		return _lastProgress;
	}
	#endregion

	[MethodDesc("Geo", "Returns the width of the shape/line, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public double Width(IGeoType geoObj) {
		var kmWidth = geoObj.GetWidth();
		return kmWidth;
	}

	[MethodDesc("Geo", "Returns the width of the shape, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Width(Nullable<ShapeSetDouble> ssd) {
		if (!ssd.Assigned)
			return default;
		return Width(ssd.Value);
	}

	[MethodDesc("Geo", "Returns the width of the line, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Width(Nullable<LineSetDouble> lsd) {
		if (!lsd.Assigned)
			return default;
		return Width(lsd.Value);
	}

	private Nullable<double> Width(IGeoBounded geoObj) {
		var centerY = (geoObj.MaxY + geoObj.MinY) / 2;
		var kmWidth = Measure.Distance(centerY, geoObj.MinX, centerY, geoObj.MaxX, 'K');
		return new Nullable<double>(kmWidth);
	}

	[MethodDesc("Geo", "Returns the height of the shape/line, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public double Height(IGeoType geoObj) {
		var kmHeight = geoObj.GetHeight();
		return kmHeight;
	}

	[MethodDesc("Geo", "Returns the height of the shape, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Height(Nullable<ShapeSetDouble> ssd) {
		if (!ssd.Assigned)
			return default;
		return Width(ssd.Value);
	}

	[MethodDesc("Geo", "Returns the height of the line, in kilometers.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Height(Nullable<LineSetDouble> lsd) {
		if (!lsd.Assigned)
			return default;
		return Width(lsd.Value);
	}

	private Nullable<double> Height(IGeoBounded geoObj) {
		var kmWidth = Measure.Distance(geoObj.MinY, geoObj.MinX, geoObj.MaxY, geoObj.MinX, 'K');
		return new Nullable<double>(kmWidth);
	}
	[MethodDesc("Geo", "Returns an XY that corresponds to the center of the shape/line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> Centroid(Nullable<GeoPointDouble> geoObj) {
		if (!geoObj.Assigned) return default;
		var point = geoObj.Value.GetCentroid();
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}
		
	[MethodDesc("Geo", "Returns an XY that corresponds to the center of the shape/line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[AggregateFunction]
	public Nullable<GeoPointDouble> Centroid(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func) {
		var inputPoints = multiRow
			.Select(idx => func(this, AggregatesQueryableIndex.GetRowStart(idx)))
			.Where(n => n.Assigned).Select(n => n.Value)
			.ToArray();
			
		return new Nullable<GeoPointDouble>(GetGeoPointDoubleGroupCenter(inputPoints));
	}

	private GeoPointDouble GetGeoPointDoubleGroupCenter(GeoPointDouble[] geoPointDoubles) {
		double x = 0, y = 0;
		int div = 0;
		if (geoPointDoubles == null) return GeoPointDouble.Empty;

		foreach (var point in geoPointDoubles) {
			x += point.X;
			y += point.Y;
			div++;
		}
			
		if (div == 0) return GeoPointDouble.Empty;
		x = (((double)x) / div);
		y = (((double)y) / div);
		return new GeoPointDouble (x, y);
	}
	[MethodDesc("Geo", "Returns an XY that corresponds to the center of the multipoint.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public GeoPointDouble Centroid(Nullable<MultipointDouble> multipoint) {
		if (!multipoint.Assigned) return default;
		PointDouble point = multipoint.Value.Centroid;
		return new GeoPointDouble(point.X, point.Y);
	}
		
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> Centroid(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		var point = shape.Value.GetCentroid();
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}

	[MethodDesc("Geo", "Returns an XY that corresponds to the center of the shape/line using the classic centroid algorithm.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> ClassicCentroid(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		var point = shape.Value.GetClassicCentroid();
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}

	[MethodDesc("Geo", "Returns an XY that corresponds to the center of the shape/line using the bounding box.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> SimpleBoundsCentroid(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		var point = shape.Value.GetSimpleBoundsCentroid();
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> Centroid(Nullable<LineSetDouble> lineSet) {
		if (!lineSet.Assigned) return default;
		var point = lineSet.Value.GetCentroidLL();
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}
	[MethodDesc("Geo", "Returns a line that corresponds to the requested segment. Segment index is 1-based.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> Segment(Nullable<ShapeSetDouble> shapeObj, int segmentIndex) {
		if (!shapeObj.Assigned)
			return default;

		if (segmentIndex <= 0)
			return new Nullable<LineSetDouble>(new LineSetDouble(new LineDouble[] { }));


		var shapeIndex = 0;
		var shape = shapeObj.Value;
		while (shapeIndex < shape.shapes.Length && segmentIndex >= shape.shapes[shapeIndex].Points.Length) {
			segmentIndex -= shape.shapes[shapeIndex].Points.Length;
			shapeIndex++;
		}

		if (shapeIndex == shape.shapes.Length)
			return new Nullable<LineSetDouble>(new LineSetDouble(new LineDouble[] { }));


		var p1 = shape.shapes[shapeIndex].Points[segmentIndex - 1];
		var p2 = shape.shapes[shapeIndex].Points[segmentIndex];

		return new Nullable<LineSetDouble>(new LineSetDouble(new[] { new LineDouble(new[] { p1, p2 }) }));
	}

	[MethodDesc("Geo", "Returns the latitude value from a point.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Latitude(Nullable<GeoPointDouble> point) {
		if (!point.Assigned) return default;
		return new Nullable<double>(point.Value.Y);
	}

	[MethodDesc("Geo", "Returns the number of holes in the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<int> NHoles(Nullable<ShapeSetDouble> ssd) {
		if (!ssd.Assigned)
			return default;

		return new Nullable<int>(ssd.Value.HoleCount());
	}

	// this will convert input to base unit m/s, then convert to output unit.  so conversion factor is key converted to m/s.
	private readonly Dictionary<string, double> _conversionFactorsPerBaseUnit = new Dictionary<string, double>() {
		{ "m/s", 1 },
		{ "kph", 3.6 },
		{ "mph", 2.23694 },
		{ "knots", 1.94384 }
	};

	[MethodDesc("Geo", "Convert velocity units.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> ConvertVelocity(
		[ParamDesc("Value to convert")] Nullable<double> value,
		[ParamDesc("Starting unit: mph, kph, m/s, or knots")] string startUnit,
		[ParamDesc("Ending unit: mph, kph, m/s, or knots")] string endUnit) {

		if (!value.Assigned) return default;

		if (!_conversionFactorsPerBaseUnit.ContainsKey(startUnit))
			throw new ArgumentException("Invalid starting unit " + startUnit);
		if (!_conversionFactorsPerBaseUnit.ContainsKey(endUnit))
			throw new ArgumentException("Invalid ending unit " + endUnit);

		return new Nullable<double>(value.Value * _conversionFactorsPerBaseUnit[endUnit] / _conversionFactorsPerBaseUnit[startUnit]);
	}

	[MethodDesc("Geo", "Returns the longitude value from a point.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> Longitude(Nullable<GeoPointDouble> point) {
		if (!point.Assigned) return default;
		return new Nullable<double>(point.Value.X);
	}

		[MethodDesc("Geo", "Returns the length of the line, in kilometers.")]
		[ExecutionOptions(ParallelExecution.Yes, true)]
		public Nullable<double> Length(Nullable<LineSetDouble> lineSet) {
			if (!lineSet.Assigned) return default;
			return new Nullable<double>(lineSet.Value.GetLength('K'));
		}

	[MethodDesc("Geo", "Returns the perimeter length of the shape in kilometers.")]
	[ExecutionOptions(ParallelExecution.Yes, true)]
	public Nullable<double> Length(Nullable<ShapeSetDouble> shapeSet) {
		if (!shapeSet.Assigned) return default;


		double distance = 0;
		var shapes = shapeSet.Value.shapes;

		for (int shapeIndex = 0; shapeIndex < shapes.Length; shapeIndex++) {
			var shape = shapes[shapeIndex];
			for (int pointIndex = 1; pointIndex < shape.Points.Length; pointIndex++) {
				var p = shape.Points[pointIndex - 1];

				double x1 = p.X;
				double y1 = p.Y;

				p = shape.Points[pointIndex];

				double x2 = p.X;
				double y2 = p.Y;

				distance += Measure.Distance(y1, x1, y2, x2, 'K');
			}
		}

		return new Nullable<double>(distance);
	}


	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> STBuffer(Nullable<ShapeSetDouble> shapeSet, Nullable<GeoPointDouble> point, Nullable<double> distanceInMeters) {
		if (!shapeSet.Assigned || !point.Assigned || !distanceInMeters.Assigned) return default;

		return new Nullable<bool>(shapeSet.Value.DistanceToShapeSetWithin(point.Value, distanceInMeters.Value));
	}

	[MethodDesc("Geo", "Creates a line from two points.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> CreateLine(Nullable<GeoPointDouble> from, Nullable<GeoPointDouble> to, bool splitAtDateLine = true) {
		if (!from.Assigned || !to.Assigned) return default;

		var linePoints = new GeoPointDouble[2];
		linePoints[0] = new GeoPointDouble(from.Value.X, from.Value.Y);
		linePoints[1] = new GeoPointDouble(to.Value.X, to.Value.Y);


		var lineset = new LineSetDouble(new[] { new LineDouble(linePoints) });
		if (splitAtDateLine)
			lineset.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineset);
	}

	[MethodDesc("Geo", "Creates a line from two points.")]
	public Nullable<LineSetDouble> CreateLineFrom(Nullable<GeoPointDouble> from, Nullable<GeoPointDouble> to, bool splitAtDateLine = true) {
		if (!from.Assigned || !to.Assigned) return default;

		var linePoints = new GeoPointDouble[2];
		linePoints[0] = new GeoPointDouble(from.Value.X, from.Value.Y);
		linePoints[1] = new GeoPointDouble(to.Value.X, to.Value.Y);


		var lineset = new LineSetDouble(new[] { new LineDouble(linePoints) });
		if (splitAtDateLine)
			lineset.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineset);
	}

	[AggregateFunction]
	[MethodDesc("Spatial", "Creates a convex hull polygon from a given set of points.")]
	public Nullable<ShapeSetDouble> ConvexHull(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func) {
		var inputPoints = multiRow
			.Select(idx => func(this, AggregatesQueryableIndex.GetRowStart(idx)))
			.Where(n => n.Assigned).Select(n => n.Value)
			.ToArray();
		var outputPoints = ConvexHullGroupBy.ConvexHull(inputPoints);
		ShapeDouble[] shapes;
		if (outputPoints == null || outputPoints.Length == 0) {
			shapes = new ShapeDouble[0];
		} else {
			var shape = ShapeDouble.PopulateShape(outputPoints, true);
			shapes = new[] { shape };
		}
		var shapeSet = new ShapeSetDouble(shapes, HoleMode.None, false);
		return new Nullable<ShapeSetDouble>(shapeSet);
	}

	[MethodDesc("Geo", "Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to the distance limit specified.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine<T>(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> pointsFunc, ExpressionScopeDelegate<T> orderByFunc
		, double distanceLimit, string unit = "K") where T : IComparable<T> {
		Nullable<GeoPointDouble> currPoint;
		var lastPoint = Nullable<GeoPointDouble>.CreateNull();
		double segmentLength = 0;
		Nullable<double> currDistance;
		var linePoints = new List<GeoPointDouble>();
		var lines = new List<LineDouble>();
		foreach (var index in multiRow.OrderBy(t => orderByFunc(this, AggregatesQueryableIndex.GetRowStart(t)))) {
			currPoint = pointsFunc(this, AggregatesQueryableIndex.GetRowStart(index));
			if (!currPoint.Assigned)
				return Nullable<LineSetDouble>.CreateNull();

			if (!lastPoint.Assigned) {
				linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
			} else {
				currDistance = DistanceBetween(lastPoint, currPoint, unit);
				if (!currDistance.Assigned)
					return Nullable<LineSetDouble>.CreateNull();
				if (segmentLength + currDistance.Value > distanceLimit) {
					//close off the line
					lines.Add(new LineDouble(linePoints.ToArray()));
					linePoints.Clear();
					segmentLength = 0;
					linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
				} else {
					linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
					segmentLength += currDistance.Value;
				}
			}

			lastPoint = currPoint;
		}

		//close off the last line
		lines.Add(new LineDouble(linePoints.ToArray()));
		linePoints.Clear();

		var lineSet = new LineSetDouble(lines.ToArray());
		lineSet.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineSet);
	}

	[MethodDesc("Geo", "Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> pointsFunc
		, ExpressionScopeDelegate<DateTimeOffset> orderByFunc, string timeSpan, double distanceLimit, string unit = "K") {
		var linePoints = new List<GeoPointDouble>();
		var lines = new List<LineDouble>();
		Nullable<GeoPointDouble> currPoint;
		Nullable<GeoPointDouble> lastPoint = default;
		Nullable<double> currDistance;
		var threshold = TimeSpan.Parse(timeSpan);
		Nullable<DateTimeOffset> lastDate = default;
		double segmentDistance = 0;
		foreach (var item in multiRow.Select(i => (index: i, orderBy: orderByFunc(this, AggregatesQueryableIndex.GetRowStart(i)))).OrderBy(t => t.orderBy)) {
			currPoint = pointsFunc(this, AggregatesQueryableIndex.GetRowStart(item.index));
			if (!lastPoint.Assigned)
				linePoints.Add(new(currPoint.Value.X, currPoint.Value.Y));
			else {
				currDistance = DistanceBetween(lastPoint, currPoint, unit);
				if ((item.orderBy.Value - lastDate.Value > threshold)
					|| (currDistance.Assigned && segmentDistance + currDistance.Value > distanceLimit)) {
					//close off the line
					lines.Add(new(linePoints.ToArray()));
					linePoints.Clear();
					segmentDistance = 0;
				} else
					segmentDistance += currDistance.Value;

				linePoints.Add(new(currPoint.Value.X, currPoint.Value.Y));
			}
			lastPoint = currPoint;
			lastDate = item.orderBy;
		}

		//close off the last line
		lines.Add(new LineDouble(linePoints.ToArray()));
		linePoints.Clear();

		var lineSet = new LineSetDouble(lines.ToArray());
		lineSet.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineSet);
	}

	[MethodDesc("Geo", "Creates a lines from the set of points, after sorting the points by the values in the second column and splitting according to the time span specified.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> pointsFunc, ExpressionScopeDelegate<DateTimeOffset> orderByFunc, string timeSpan) {
		Nullable<GeoPointDouble> currPoint;
		var threshold = TimeSpan.Parse(timeSpan);

		var lastDate = Nullable<DateTimeOffset>.CreateNull();
		var linePoints = new List<GeoPointDouble>();
		var lines = new List<LineDouble>();
		foreach (var item in multiRow.Select(i => (index: i, orderBy: orderByFunc(this, AggregatesQueryableIndex.GetRowStart(i)))).OrderBy(t => t.Item2)) {
			currPoint = pointsFunc(this, AggregatesQueryableIndex.GetRowStart(item.index));
			if (!currPoint.Assigned || !item.orderBy.Assigned)
				return Nullable<LineSetDouble>.CreateNull();

			if (lastDate.Assigned && (item.orderBy.Value - lastDate.Value) > threshold) {
				//close off the lline
				lines.Add(new LineDouble(linePoints.ToArray()));
				linePoints.Clear();
			}

			linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
			lastDate = item.orderBy;
		}

		//close off the last line
		lines.Add(new LineDouble(linePoints.ToArray()));
		linePoints.Clear();

		var lineSet = new LineSetDouble(lines.ToArray());
		lineSet.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineSet);
	}

	[MethodDesc("Geo", "Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> pointsFunc, ExpressionScopeDelegate<DateTimeOffset> orderByFunc, string timeSpan
		, double distanceLimit, string distanceThreshold = "cumulative", string unit = "K") {
		Nullable<GeoPointDouble> currPoint;
		var threshold = TimeSpan.Parse(timeSpan);

		var lastPoint = Nullable<GeoPointDouble>.CreateNull();
		var lastDate = Nullable<DateTimeOffset>.CreateNull();
		var linePoints = new List<GeoPointDouble>();
		var lines = new List<LineDouble>();
		double segmentLength = 0;
		Nullable<double> currDistance;
		foreach (var item in multiRow.Select(i => (index: i, orderBy: orderByFunc(this, AggregatesQueryableIndex.GetRowStart(i)))).OrderBy(t => t.Item2)) {
			currPoint = pointsFunc(this, AggregatesQueryableIndex.GetRowStart(item.index));
			if (!currPoint.Assigned || !item.orderBy.Assigned)
				return Nullable<LineSetDouble>.CreateNull();

			if (!lastPoint.Assigned) {
				linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
			} else {
				currDistance = DistanceBetween(lastPoint, currPoint, unit);
				if (!currDistance.Assigned)
					return Nullable<LineSetDouble>.CreateNull();

				var totalDist = currDistance.Value;
				if (distanceThreshold == "cumulative")
					totalDist += segmentLength;
				if ((item.orderBy.Value - lastDate.Value) > threshold || totalDist > distanceLimit) {
					//close off the line
					lines.Add(new LineDouble(linePoints.ToArray()));
					linePoints.Clear();
					segmentLength = 0;
				} else {
					segmentLength += currDistance.Value;
				}

				linePoints.Add(new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y));
			}
			lastPoint = currPoint;
			lastDate = item.orderBy;
		}

		//close off the last line
		lines.Add(new LineDouble(linePoints.ToArray()));
		linePoints.Clear();

		var lineSet = new LineSetDouble(lines.ToArray());
		lineSet.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineSet);
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> STOverlaps(Nullable<ShapeSetDouble> shapeSet, Nullable<GeoPointDouble> point) {
		if (!shapeSet.Assigned || !point.Assigned) return default;
		return new Nullable<bool>(shapeSet.Value.InPoly(point.Value.X, point.Value.Y));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> STWithin(Nullable<GeoPointDouble> point, Nullable<ShapeSetDouble> shapeSet) {
		if (!shapeSet.Assigned || !point.Assigned) return default;
		return new Nullable<bool>(shapeSet.Value.InPoly(point.Value.X, point.Value.Y));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> STDistance(Nullable<GeoPointDouble> xy1, Nullable<GeoPointDouble> xy2) {
		if (!xy1.Assigned || !xy2.Assigned) return default;
		return new Nullable<double>(Measure.HaversineDistance(xy1.Value.X, xy1.Value.Y, xy2.Value.X, xy2.Value.Y));
	}


	[MethodDesc("Geo", "Creates a closed polygon from the set of points")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<ShapeSetDouble> CreateShape(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func) {
		var inputPoints = multiRow
			.Select(idx => func(this, AggregatesQueryableIndex.GetRowStart(idx)))
			.Where(n => n.Assigned).Select(n => n.Value)
			.ToArray();

		if (inputPoints.Length < 3) {
			return Nullable<ShapeSetDouble>.CreateNull();
		}

		var s = ShapeDouble.PopulateShape(inputPoints);
		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(new ShapeDouble[] { s }));
	}

	[MethodDesc("Geo", "Creates a closed polygon from the linesets")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public Nullable<ShapeSetDouble> CreateShape(Nullable<LineSetDouble> lineSet) {
		if (!lineSet.IsAssigned()) {
			return Nullable<ShapeSetDouble>.CreateNull();
		}

		var lines = lineSet.Value.lines;
		var shapes = new ShapeDouble[lines.Length];

		for (var i = 0; i < lines.Length; i++) {
			var p = lines[i].Points.ToArray();
			shapes[i] = ShapeDouble.PopulateShape(p);
		}

		//NOT splitting at date line because we're assuming the same points as the original lineset!
		var shapeSet = ShapeSetDouble.PopulateShapeSet(shapes);
		return new Nullable<ShapeSetDouble>(shapeSet);
	}

	[MethodDesc("Geo", "Creates the great circle curve between two points.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> CreateGreatCircle(Nullable<GeoPointDouble> from, Nullable<GeoPointDouble> to, int resolution = 20) {
		if (!from.Assigned || !to.Assigned) return default;

		var linePoints = GeometryUtil.CalculateGreatCircle(from.Value, to.Value, resolution);

		var lineset = new LineSetDouble(new[] { new LineDouble(linePoints) });

		lineset.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineset);
	}

	[MethodDesc("Geo", "Creates the great circle curve between two points.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> CreateGreatCircle(Nullable<LineSetDouble> inLineSet, int resolution = 20) {
		if (!inLineSet.Assigned) return default;
		var greatCircleLines = new List<LineDouble>();
		foreach (var line in inLineSet.Value.Lines) {
			for (var i = 0; i < line.Points.Length - 1; i++) {
				var a = line.Points[i];
				var aXY = new GeoPointDouble(a.X, a.Y);

				var b = line.Points[i + 1];
				var bXY = new GeoPointDouble(b.X, b.Y);

				var greatCircleLineSet = CreateGreatCircle(new Nullable<GeoPointDouble>(aXY), new Nullable<GeoPointDouble>(bXY), resolution);
				greatCircleLines.AddRange((IEnumerable<LineDouble>)greatCircleLineSet.Value.Lines);
			}
		}
		var outLineSet = new LineSetDouble(greatCircleLines.ToArray());
		return new Nullable<LineSetDouble>(outLineSet);
	}

	[MethodDesc("Geo", "Returns the minimal area oriented bounding box surrounding the lineset.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateMinimumAreaOrientedBoundingBox(Nullable<LineSetDouble> lineSet) {
		if (!lineSet.Assigned) return default;

		var ls = lineSet.Value;
		var len = ls.lines.Length;
		var shapes = new ShapeDouble[len];
		for (var i = 0; i < len; i++) {
			shapes[i] = CreateMinAreaOBB(ls.lines[i]);
		}

		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(shapes));
	}

	[MethodDesc("Geo", "Returns the minimal area oriented bounding box surrounding the shapeset.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateMinimumAreaOrientedBoundingBox(Nullable<ShapeSetDouble> shapeSet) {
		if (!shapeSet.Assigned) return default;

		var ss = shapeSet.Value;
		var len = ss.shapes.Length;
		var shapes = new ShapeDouble[len];
		for (var i = 0; i < len; i++) {
			shapes[i] = CreateMinAreaOBB(ss.shapes[i]);
		}

		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(shapes));
	}

	[MethodDesc("Geo", "Returns the minimal area oriented bounding box surrounding the multipoint.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateMinimumAreaOrientedBoundingBox(Nullable<MultipointDouble> multipoint) {
		if (!multipoint.Assigned) return default;

		var mp = multipoint.Value;
		if (mp.Points.Length < 2) {
			return default;
		}

		var shapes = new ShapeDouble[1];
		shapes[0] = CreateMinAreaOBB(mp.Points, mp.Length);

		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(shapes));
	}

	private ShapeDouble CreateMinAreaOBB(IGeoItem<GeoPointDouble> shape) {
		return CreateMinAreaOBB(shape.Points);
	}
	private ShapeDouble CreateMinAreaOBB(PointSet<GeoPointDouble> points) {
		return CreateMinAreaOBB(points.AsReadOnlySpan(), points.Length);
	}
	private ShapeDouble CreateMinAreaOBB(ReadOnlySpan<GeoPointDouble> points, int length) {
		MinimumAreaOrientedBoundingBox.OrientedBox result;
		(_, result, _) = MinimumAreaOrientedBoundingBox.Calculate(points, length);

		var boxPoints = result.GetOrderedVertices().Select(o => new GeoPointDouble(o.X, o.Y)).ToArray();
		return new ShapeDouble(boxPoints);
	}

	[MethodDesc("Geo", "Returns the minimal area oriented bounding box surrounding the ellipse generated by the provided inputs.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> CreateMinimumAreaOrientedBoundingBox(
		[ParamDesc("Center point")] Nullable<GeoPointDouble> centerPoint,
		[ParamDesc("Major axis length (width; default in meters)")] double majorAxisLength,
		[ParamDesc("Minor axis length (height; default in meters)")] double minorAxisLength,
		[ParamDesc("Degree of rotation (0-180)")] double orientation,
		[ParamDesc("Granularity of points on the ellipse (in degrees between points)")] int steps = 4,
		[ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!centerPoint.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		var ellipse = CreateEllipse(centerPoint, majorAxisLength, minorAxisLength, orientation, steps, unit);
		if (!ellipse.Assigned)
			return Nullable<ShapeSetDouble>.CreateNull();

		return CreateMinimumAreaOrientedBoundingBox(ellipse);
	}

	[MethodDesc("Geo", "Returns true if the two lines overlap.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<LineSetDouble> line1, Nullable<LineSetDouble> line2) {
		if (!line1.Assigned || !line2.Assigned) return default;
		return new Nullable<bool>(line1.Value.OverlapLine(line2.Value));
	}

	[MethodDesc("Geo", "Returns true if the line and the shape overlap.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape, Nullable<LineSetDouble> line) {
		if (!line.Assigned || !shape.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapLine(line.Value));
	}

	[MethodDesc("Geo", "Returns true if the line and the shape overlap.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<LineSetDouble> line, Nullable<ShapeSetDouble> shape) {
		if (!line.Assigned || !shape.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapLine(line.Value));
	}

	[MethodDesc("Geo", "Returns true if the two shapes overlap.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape1, Nullable<ShapeSetDouble> shape2) {
		if (!shape1.Assigned || !shape2.Assigned) return default;
		return new Nullable<bool>(shape1.Value.OverlapPoly(shape2.Value));
	}
		
	[MethodDesc("Geo", "Returns true if the two points overlap (equal).")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<GeoPointDouble> point1, Nullable<GeoPointDouble> point2) {
		if (!point1.Assigned || !point2.Assigned) return default;
		return new Nullable<bool>(point1.Value.Equals(point2.Value));
	}

	[MethodDesc("Geo", "Returns true if the point falls within the shape.")]
	[MethodOptimization(typeof(InsideJoinOptimizer))]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape, Nullable<GeoPointDouble> point) {
		if (!shape.Assigned || !point.Assigned) return default;
		return new Nullable<bool>(shape.Value.InPoly(point.Value.X, point.Value.Y));
	}

	[MethodDesc("Geo", "Returns true if the multipoint falls within the shape.")]
	[MethodOptimization(typeof(InsideJoinOptimizer))]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape, Nullable<MultipointDouble> multipoint) {
		if (!shape.Assigned || !multipoint.Assigned) return default;
		foreach (GeoPointDouble point in multipoint.Value.Points) {
			if (shape.Value.InPoly(point.X, point.Y)) return new Nullable<bool>(true);
		}
		return new Nullable<bool>(false);
	}

	[MethodDesc("Geo", "Returns true if the point falls within the shape.")]
	[MethodOptimization(typeof(InsideJoinOptimizer))]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<GeoPointDouble> point, Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned || !point.Assigned) return default;
		return new Nullable<bool>(shape.Value.InPoly(point.Value.X, point.Value.Y));
	}

	[MethodDesc("Geo", "Returns true if the shape overlaps with the raster.")]
	[MethodOptimization(typeof(OverlapJoinOptimizer))]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape, Nullable<RasterBinaryWrapper> raster) {
		if (!shape.Assigned || !raster.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapPoly(raster.Value.GetBoundingBox()));
	}

	[MethodDesc("Geo", "Returns true if the shape overlaps with the raster.")]
	[MethodOptimization(typeof(OverlapJoinOptimizer))]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<RasterBinaryWrapper> raster, Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned || !raster.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapPoly(raster.Value.GetBoundingBox()));
	}

	[MethodDesc("Geo", "Returns true if the nerf overlaps the shape.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<NERFWrapper> nerf, Nullable<ShapeSetDouble> shape) {
		return IsOverlap(cacheIndex, shape, nerf);
	}

	[MethodDesc("Geo", "Returns true if the shape overlaps the nerf.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<ShapeSetDouble> shape, Nullable<NERFWrapper> nerf) {
		if (!shape.Assigned || !nerf.Assigned) return default;
		var ibr = GetNerfBlobReader(cacheIndex, nerf.Value, "IsOverlap(shape, nerf)");
		var overlaps = nerf.Value.OverlapsShape(_core, shape.Value, ibr);
		return new Nullable<bool>(overlaps);
	}

	[MethodDesc("Geo", "Returns true if the nerf overlaps the line.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<NERFWrapper> nerf, Nullable<LineSetDouble> line) {
		return IsOverlap(cacheIndex, line, nerf);
	}

	[MethodDesc("Geo", "Returns true if the line overlaps the nerf.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<LineSetDouble> line, Nullable<NERFWrapper> nerf) {
		if (!line.Assigned || !nerf.Assigned) return default;
		var ibr = GetNerfBlobReader(cacheIndex, nerf.Value, "IsOverlap(line, nerf)");
		var overlaps = nerf.Value.OverlapsLine(_core, line.Value, 0, ibr);
		return new Nullable<bool>(overlaps);
	}

	[MethodDesc("Geo", "Returns true if the nerf overlaps the point.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<NERFWrapper> nerf, Nullable<GeoPointDouble> point) {
		return IsOverlap(cacheIndex, point, nerf);
	}

	[MethodDesc("Geo", "Returns true if the point overlaps the nerf.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<GeoPointDouble> point, Nullable<NERFWrapper> nerf) {
		if (!point.Assigned || !nerf.Assigned) return default;
		var ibr = GetNerfBlobReader(cacheIndex, nerf.Value, "IsOverlap(point, nerf)");
		var overlaps = nerf.Value.OverlapsXY(_core, point.Value, 0, ibr);
		return new Nullable<bool>(overlaps);
	}

	[MethodDesc("Geo", "Returns true if the nerf overlaps the multipoint.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<NERFWrapper> nerf, Nullable<MultipointDouble> multipoint) {
		return IsOverlap(cacheIndex, multipoint, nerf);
	}

	[MethodDesc("Geo", "Returns true if the multipoint overlaps the nerf.")]
	public Nullable<bool> IsOverlap([CacheIndex] int cacheIndex, Nullable<MultipointDouble> multipoint, Nullable<NERFWrapper> nerf) {
		if (!multipoint.Assigned || !nerf.Assigned) return default;
		var ibr = GetNerfBlobReader(cacheIndex, nerf.Value, "IsOverlap(multipoint, nerf)");
		var overlaps = nerf.Value.OverlapsMultipoint(_core, multipoint.Value, 0, ibr);
		return new Nullable<bool>(overlaps);
	}

	private IBlobReader GetNerfBlobReader(int cacheIndex, NERFWrapper nerf, string name) {
		var ibr = Cache.GetOrAdd(cacheIndex, () => {
			var ibr = nerf.CreateBlobReader(_core);
			QueryContext.RegisterAfterQueryAction("IsOverlap(Shape, NERF)", () => {
				ibr?.Dispose();
			});
			return ibr;
		});
		return ibr;
	}

	[MethodDesc("Geo", "Returns true if the shape overlaps with the image.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ShapeSetDouble> shape, Nullable<ImageBinaryWrapper> ibw) {
		if (!shape.Assigned || !ibw.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapPoly(ibw.Value.GetBoundingBox()));
	}
		
	[MethodDesc("Geo", "Returns true if the point overlaps with the image.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<GeoPointDouble> point, Nullable<ImageBinaryWrapper> ibw) {
		if (!point.Assigned || !ibw.Assigned) return default;
		return new Nullable<bool>(ibw.Value.GetBoundingBox().InPoly(point.Value.X, point.Value.Y));
	}

	[MethodDesc("Geo", "Returns true if the point overlaps with the image.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ImageBinaryWrapper> ibw, Nullable<GeoPointDouble> point) {
		if (!point.Assigned || !ibw.Assigned) return default;
		return new Nullable<bool>(ibw.Value.GetBoundingBox().InPoly(point.Value.X, point.Value.Y));
	}

	[MethodDesc("Geo", "Returns true if the shape overlaps with the image.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsOverlap(Nullable<ImageBinaryWrapper> ibw, Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned || !ibw.Assigned) return default;
		return new Nullable<bool>(shape.Value.OverlapPoly(ibw.Value.GetBoundingBox()));
	}

	[MethodDesc("Geo", "Creates a line from the set of points.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func) {
		var linePoints = new GeoPointDouble[multiRow.Count];

		for (var i = 0; i < multiRow.Count; i++) {
			var point = func(this, AggregatesQueryableIndex.GetRowStart(multiRow[i]));
			if (!point.Assigned) {
				return Nullable<LineSetDouble>.CreateNull();
			}
			linePoints[i] = new GeoPointDouble(point.Value.X, point.Value.Y);
		}

		var lineset = new LineSetDouble(new LineDouble[] { new LineDouble(linePoints) });
		lineset.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineset);
	}

	[MethodDesc("Geo", "Creates a line from the set of points, after sorting the points by the values in the second column.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public Nullable<LineSetDouble> CreateLine<T>(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> pointsFunc, ExpressionScopeDelegate<T> orderByFunc) where T : IComparable<T> {
		var linePoints = new GeoPointDouble[multiRow.Count];
		var orderBy = new T[multiRow.Count];

		for (int i = 0; i < multiRow.Count; i++) {
			var currPoint = pointsFunc(this, AggregatesQueryableIndex.GetRowStart(multiRow[i]));
			if (!currPoint.Assigned) {
				return Nullable<LineSetDouble>.CreateNull();
			}

			var currSort = orderByFunc(this, AggregatesQueryableIndex.GetRowStart(multiRow[i]));
			if (!currSort.Assigned) {
				return Nullable<LineSetDouble>.CreateNull();
			}

			linePoints[i] = new GeoPointDouble(currPoint.Value.X, currPoint.Value.Y);
			orderBy[i] = currSort.Value;
		}

		Array.Sort(orderBy, linePoints);
		var lineset = new LineSetDouble(new LineDouble[] { new LineDouble(linePoints) });
		lineset.SplitLinesAtDateline();

		return new Nullable<LineSetDouble>(lineset);
	}

	[MethodDesc("Geo", "Returns true if the two lines are similar.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> LineSimilarity(Nullable<LineSetDouble> line1, Nullable<LineSetDouble> line2,
		[ParamDesc("Radius of search, in meters")] double radiusInMeters) {
		if (!line1.Assigned || !line2.Assigned) {
			return default;
		}
		return new Nullable<double>(IsSimilarLineJoinMethod.ComputeSimilarity(line1.Value, line2.Value, radiusInMeters));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> FirstPoint(Nullable<LineSetDouble> line) {
		if (!line.Assigned || line.Value.lines.Length == 0)
			return default;

		return new Nullable<GeoPointDouble>(new GeoPointDouble(line.Value.lines[0].Points[0].X, line.Value.lines[0].Points[0].Y));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> LastPoint(Nullable<LineSetDouble> line) {
		if (!line.Assigned || line.Value.lines.Length == 0)
			return default;

		var lineValue = line.Value;
		var point = lineValue.lines[^1].Points[^1];

		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}
	private static List<MapLarge.Engine.Query.Expressions.ExpressionScope.LineIntersectResult> CalculateOrderedIntersections(LineSet ls1, MultiRowData<LineSet> lsGroup, int metersTolerance = 0) {

		object[] targets = lsGroup.GetValues();
		var intersections = new List<MapLarge.Engine.Query.Expressions.ExpressionScope.LineIntersectResult>();
		double curDistanceTraveled = 0;
		for (int i = 0; i < ls1.lines.Length; i++) {
			Line curLine = ls1.lines[i];
			GeoPoint prevPoint = curLine.Points[0];
			for (int j = 1; j < curLine.Points.Length; j++) {
				GeoPoint curPoint = curLine.Points[j];
				double dx = curPoint.x - prevPoint.x;
				double dy = curPoint.y - prevPoint.y;
				double curLen = Math.Sqrt(dx * dx + dy * dy);
				for (int k = 0; k < targets.Length; k++) {
					LineSet curTarget = (LineSet)targets[k];
					for (int l = 0; l < curTarget.lines.Length; l++) {
						Line curTargetLine = curTarget.lines[l];
						GeoPoint prevTargetPoint = curTargetLine.Points[0];
						for (int m = 0; m < curTargetLine.Points.Length; m++) {
							GeoPoint curTargetPoint = curTargetLine.Points[m];

							// then do the line intersection
							if (GeometryUtil.LineLineIntersection(prevPoint.x, prevPoint.y, curPoint.x, curPoint.y, prevTargetPoint.x, prevTargetPoint.y, curTargetPoint.x, curTargetPoint.y, out double intersectX, out double intersectY)) {
								int pixelTolerance = Measure.ConvertMetersToPixels(metersTolerance, GoogleGeo.geo.getLatFromPixel((int)intersectY, Core.BaseZoomSetting));
								GetBoundsFromPoints(prevPoint, curPoint, out int sourceMinX, out int sourceMinY, out int sourceMaxX, out int sourceMaxY, pixelTolerance);
								GetBoundsFromPoints(prevTargetPoint, curTargetPoint, out int targetMinX, out int targetMinY, out int targetMaxX, out int targetMaxY, pixelTolerance);
								if (intersectX >= sourceMinX && intersectX <= sourceMaxX
															 && intersectY >= sourceMinY && intersectY <= sourceMaxY
															 && intersectX >= targetMinX && intersectX <= targetMaxX
															 && intersectY >= targetMinY && intersectY <= targetMaxY) {

									double partialDist;
									if (dx > dy)
										partialDist = ((intersectX - prevPoint.x) / dx) * curLen;
									else
										partialDist = ((intersectY - prevPoint.y) / dy) * curLen;
									intersections.Add(new MapLarge.Engine.Query.Expressions.ExpressionScope.LineIntersectResult { distanceTraveled = curDistanceTraveled + partialDist, index = k, intersectPoint = new GeoPoint((int)Math.Round(intersectX), (int)Math.Round(intersectY)) });
								} // end if intersect within segments
							} // end if lines intersect

							prevTargetPoint = curTargetPoint;
						}
					}
				}
				curDistanceTraveled += curLen;
				prevPoint = curPoint;
			} // end for source points
		} // end for source lines

		intersections.Sort();
		return intersections;
	}

	private static void GetBoundsFromPoints(GeoPoint p1, GeoPoint p2, out int minX, out int minY, out int maxX, out int maxY, int pixelsTolerance = 0) {
		if (p1.x < p2.x) {
			minX = p1.x - pixelsTolerance;
			maxX = p2.x + pixelsTolerance;
		} else {
			minX = p2.x - pixelsTolerance;
			maxX = p1.x + pixelsTolerance;
		}

		if (p1.y < p2.y) {
			minY = p1.y - pixelsTolerance;
			maxY = p2.y + pixelsTolerance;
		} else {
			minY = p2.y - pixelsTolerance;
			maxY = p1.y + pixelsTolerance;
		}
	}
	private static void GetBoundsFromPoints(GeoPointDouble p1, GeoPointDouble p2, out double minX, out double minY, out double maxX, out double maxY, double metersTolerance = 0) {
		if(p1.X < p2.X) {
			minX = p1.X - metersTolerance;
			maxX = p2.X + metersTolerance;
		} else {
			minX = p2.X - metersTolerance;
			maxX = p1.X + metersTolerance;
		}

		if(p1.Y < p2.Y) {
			minY = p1.Y - metersTolerance;
			maxY = p2.Y + metersTolerance;
		} else {
			minY = p2.Y - metersTolerance;
			maxY = p1.Y + metersTolerance;
		}
	}

	[MethodDesc("Geo", "Returns the point at the specified index from the geometry.")]
	public Nullable<GeoPointDouble> NthPoint(Nullable<LineSetDouble> geo, Nullable<int> index) {
		if (!geo.Assigned || !index.Assigned || geo.Value.lines == null || geo.Value.lines.Length == 0)
			return default;

		int idx = index.Value;
		if (idx < 0)
			return default;

		foreach(var l in geo.Value.lines) {
			if (idx >= l.Points.Length)
				idx -= l.Points.Length;
			else if(idx >= 0)
				return new(l.Points[idx]);
		}

		return default;
	}
	[MethodDesc("Geo", "Returns the point at the specified index from the geometry.")]
	public Nullable<GeoPointDouble> NthPoint(Nullable<MultipointDouble> geo, Nullable<int> index) {
		if (!geo.Assigned || !index.Assigned || geo.Value.Points == null || geo.Value.Points.Length == 0)
			return default;

		if (index.Value < 0 || index.Value >= geo.Value.Points.Length)
			return default;
		return new(geo.Value.Points[index.Value]);
	}
	[MethodDesc("Geo", "Returns the point at the specified index from the geometry.")]
	public Nullable<GeoPointDouble> NthPoint(Nullable<ShapeSetDouble> geo, Nullable<int> index) {
		if (!geo.Assigned || !index.Assigned || geo.Value.shapes == null || geo.Value.shapes.Length == 0)
			return default;

		int idx = index.Value;
		if (idx < 0)
			return default;

		foreach (var s in geo.Value.shapes) {
			if (idx >= s.Points.Length)
				idx -= s.Points.Length;
			else if (idx >= 0)
				return new(s.Points[idx]);
		}

		return default;
	}

	[MethodDesc("Geo", "Get the WKT representation of the shape or line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	// ReSharper disable once InconsistentNaming
	public string ToWKT(IGeoType geo) {
		return geo.GetWKT();
	}

	[MethodDesc("Geo", "Get the WKT representation of the WGS84 point.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ToWKT(GeoPointDouble point) {
		return point.GetWKT();
	}

	[MethodDesc("Geo", "Get the WKT representation of the WGS84 line set.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ToWKT(LineSetDouble point) {
		return point.GetWKT();
	}

	[MethodDesc("Geo", "Get the WKT representation of the WGS84 shape set.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ToWKT(ShapeSetDouble point) {
		return point.GetWKT();
	}

	[MethodDesc("Geo", "Get the 3D WKT representation of the point with the specified altitude.")]
	public string ToWKT3D<T>(GeoPointDouble xy, T altitude, string altitudeUnits = "m") {
		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		var alt = Convert.ToDouble(altitude);
		alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);

		var pt3d = CreateXYDouble3D();
		return pt3d.GetWKT();

		XYDouble3D CreateXYDouble3D() {
			return new XYDouble3D(xy.X, xy.Y, alt);
		}
	}

	[MethodDesc("Geo", "Get the 3D WKT representation of the shape with the specified altitude.")]
	public string ToWKT3D<T>(ShapeSetDouble shapeDouble, T altitude, string altitudeUnits = "m") {
		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		var alt = Convert.ToDouble(altitude);
		alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);

		var shapeSetDoubleFlat3d = CreateShapeSetDoubleFlat3D();
		return shapeSetDoubleFlat3d.GetWKT();

		ShapeSetDoubleFlat3D CreateShapeSetDoubleFlat3D() {
			var shapes2d = shapeDouble.shapes;
			var shapes3d = new ShapeDoubleFlat3D[shapes2d.Length];
			for (var i = 0; i < shapes2d.Length; i++) {
				var s = shapes2d[i];
				var pts2d = s.Points;
				var pts3d = new GeoPointDouble3D[pts2d.Length];
				for (var j = 0; j < pts2d.Length; j++) {
					var p = pts2d[j];
					pts3d[j] = new GeoPointDouble3D(p.X, p.Y, alt);
				}
				shapes3d[i] = new ShapeDoubleFlat3D(pts3d) {
					isHole = s.isHole
				};
			}
			return new ShapeSetDoubleFlat3D(shapes3d);
		}
	}

	[MethodDesc("Geo", "Get the 3D WKT representation of the line with the specified altitude.")]
	public string ToWKT3D<T>(LineSetDouble line, T altitude, string altitudeUnits = "m") {
		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		var alt = Convert.ToDouble(altitude);
		alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);

		var lineSetDouble3d = CreateLineSetDouble3D();
		return lineSetDouble3d.GetWKT();

		LineSetDouble3D CreateLineSetDouble3D() {
			var lines2D = line.lines;
			var lines3D = new LineDouble3D[lines2D.Length];
			for (var i = 0; i < lines2D.Length; i++) {
				var l = lines2D[i];
				var pts2d = l.Points;
				var pts3d = new GeoPointDouble3D[pts2d.Length];
				for (var j = 0; j < pts2d.Length; j++) {
					var p = pts2d[j];
					pts3d[j] = CreateGeoPointDouble3D(p.X, p.Y);
				}
				lines3D[i] = new LineDouble3D(pts3d);
			}
			return new LineSetDouble3D(lines3D);
		}
		GeoPointDouble3D CreateGeoPointDouble3D(double x, double y) {
			return new GeoPointDouble3D(x, y, alt);
		}
	}

	[MethodDesc("Geo", "Creates the WKT of 3D points from the set of points and altitudes, after sorting the points by the values in the third column, and returns the WKT with the height in meters.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public string ToPointWKT3D<T, U>(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> points, ExpressionScopeDelegate<U> altitude, ExpressionScopeDelegate<T> orderBy, string altitudeUnits = "m") 
		where U : IComparable<U> where T : IComparable<T> {
		var data = new List<GeoPointDouble>();
		var altitudeData = new List<U>();
		var sortData = new List<T>();
		foreach (var item in multiRow.Select(row => new { 
					 pt = points(this, AggregatesQueryableIndex.GetRowStart(row)),
					 alt = altitude(this, AggregatesQueryableIndex.GetRowStart(row)),
					 sort = orderBy(this, AggregatesQueryableIndex.GetRowStart(row)),
				 })) {
			if (item.pt.Assigned) {
				data.Add(item.pt.Value);
				altitudeData.Add(item.alt.Value);
				sortData.Add(item.sort.Value);
			}
		}

		var indexes = new int[data.Count];
		for (var i = 0; i < data.Count; i++)
			indexes[i] = i;

		var sortArray = sortData.ToArray();
		Array.Sort(sortArray, indexes);

		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		XYDouble3D[] xyzPoints;
		xyzPoints = new XYDouble3D[data.Count];
		for (var i = 0; i < data.Count; i++) {
			xyzPoints[i] = CreateXYDouble3D((GeoPointDouble)data[indexes[i]], Convert.ToDouble(altitudeData[indexes[i]]));
		}

		if (xyzPoints.Length == 0) {
			return "POINT ()";
		} else if (xyzPoints.Length == 1) {
			return xyzPoints[0].GetWKT();
		} else {
			return XYDouble3D.GetMultiPointWKT(xyzPoints);
		}

		XYDouble3D CreateXYDouble3D(GeoPointDouble xy, double alt) {
			alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);
			return new XYDouble3D(xy.X, xy.Y, alt);
		}
	}

	[MethodDesc("Geo", "Creates the WKT of 3D points from the set of shapes and altitudes, after sorting the points by the values in the third column, and returns the WKT with the height in meters.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public string ToShapeWKT3D<T, U>(IList<int> multiRow, ExpressionScopeDelegate<ShapeSetDouble> shapes, ExpressionScopeDelegate<U> altitude, ExpressionScopeDelegate<T> orderBy, string altitudeUnits = "m")
		where U : IComparable<U> where T : IComparable<T> {
		var data = new List<ShapeSetDouble>();
		var altitudeData = new List<U>();
		var sortData = new List<T>();
		foreach (var item in multiRow.Select(row => new {
					 pt = shapes(this, AggregatesQueryableIndex.GetRowStart(row)),
					 alt = altitude(this, AggregatesQueryableIndex.GetRowStart(row)),
					 sort = orderBy(this, AggregatesQueryableIndex.GetRowStart(row)),
				 })) {
			if (item.pt.Assigned) {
				data.Add(item.pt.Value);
				altitudeData.Add(item.alt.Value);
				sortData.Add(item.sort.Value);
			}
		}

		var indexes = new int[data.Count];
		for (var i = 0; i < data.Count; i++)
			indexes[i] = i;

		var sortArray = sortData.ToArray();
		Array.Sort(sortArray, indexes);

		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		var shapeSets3d = new ShapeSetDoubleFlat3D[data.Count];
		for (var i = 0; i < data.Count; i++) {
			var shapeSetDouble = (ShapeSetDouble)data[indexes[i]];
			var alt = Convert.ToDouble(altitudeData[indexes[i]]);
			alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);
			shapeSets3d[i] = CreateShapeSetDoubleFlat3D(shapeSetDouble, alt);
		}

		return ShapeSetDoubleFlat3D.GetMultiPolygonWKT(shapeSets3d, true);

		ShapeSetDoubleFlat3D CreateShapeSetDoubleFlat3D(ShapeSetDouble s, double alt) {
			var shapes2d = s.shapes;
			var shapes3d = new ShapeDoubleFlat3D[shapes2d.Length];
			for (var i = 0; i < shapes2d.Length; i++) {
				shapes3d[i] = CreateShapeDoubleFlat3D(shapes2d[i], alt);
			}
			return new ShapeSetDoubleFlat3D(shapes3d);
		}
		ShapeDoubleFlat3D CreateShapeDoubleFlat3D(ShapeDouble s, double alt) {
			var pts2d = s.Points;
			var pts3d = new GeoPointDouble3D[pts2d.Length];
			for (var i = 0; i < pts2d.Length; i++) {
				var p = pts2d[i];
				pts3d[i] = new GeoPointDouble3D(p.X, p.Y, alt);
			}
			return new ShapeDoubleFlat3D(pts3d) {
				isHole = s.isHole
			};
		}
	}

	[MethodDesc("Geo", "Creates the WKT of 3D lines from the set of points and altitudes, after sorting the points by the values in the third column, and returns the WKT with the height in meters.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	[AggregateFunction]
	public string ToLineWKT3D<T, U>(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> points, ExpressionScopeDelegate<U> altitude, ExpressionScopeDelegate<T> orderBy, string altitudeUnits = "m", bool firstLastOnly = false)
		where U : IComparable<U> where T : IComparable<T> {
		var data = new List<GeoPointDouble>();
		var altitudeData = new List<U>();
		var sortData = new List<T>();
		foreach (var item in multiRow.Select(row => new {
					 pt = points(this, AggregatesQueryableIndex.GetRowStart(row)),
					 alt = altitude(this, AggregatesQueryableIndex.GetRowStart(row)),
					 sort = orderBy(this, AggregatesQueryableIndex.GetRowStart(row)),
				 })) {
			if (item.pt.Assigned) {
				data.Add(item.pt.Value);
				altitudeData.Add(item.alt.Value);
				sortData.Add(item.sort.Value);
			}
		}

		var indexes = new int[data.Count];
		for (var i = 0; i < data.Count; i++)
			indexes[i] = i;

		var sortArray = sortData.ToArray();
		Array.Sort(sortArray, indexes);			

		var measurementType = MLUtil.Measurement.GetMeasurementType(altitudeUnits);
		if (!measurementType.HasValue) {
			measurementType = MLUtil.MeasurementType.Meter;
		}

		GeoPointDouble3D[] linePoints;
		if (firstLastOnly && indexes.Length >= 2) {
			linePoints = new GeoPointDouble3D[2];
			linePoints[0] = CreateGeoPointDouble3D((GeoPointDouble)data[indexes[0]], Convert.ToDouble(altitudeData[indexes[0]]));
			linePoints[1] = CreateGeoPointDouble3D((GeoPointDouble)data[indexes[indexes.Length - 1]], Convert.ToDouble(altitudeData[indexes[indexes.Length - 1]]));
		} else {
			linePoints = new GeoPointDouble3D[data.Count];
			for (var i = 0; i < data.Count; i++) {
				linePoints[i] = CreateGeoPointDouble3D((GeoPointDouble)data[indexes[i]], Convert.ToDouble(altitudeData[indexes[i]]));
			}
		}

		var lineset = new LineSetDouble3D(new LineDouble3D[] { new LineDouble3D(linePoints) });
		//lineset.SplitLinesAtDateline();

		return lineset.GetWKT();

		GeoPointDouble3D CreateGeoPointDouble3D(GeoPointDouble xy, double alt) {
			alt = MLUtil.Measurement.Convert(alt, measurementType.Value, MLUtil.MeasurementType.Meter);
			return new GeoPointDouble3D(xy.X, xy.Y, alt);
		}
	}

	[MethodDesc("Geo", "Given a gradient, a scale, and one or more values, calculate the gradient values.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string CalculateGradient(string gradient, double minValue, double maxValue, double v1, double v2) {
		return CalculateGradient(gradient, minValue, maxValue, new[] { v1, v2 });
	}

	[MethodDesc("Geo", "Given a gradient, a scale, and one or more values, calculate the gradient values.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string CalculateGradient(string gradient, double minValue, double maxValue, double[] values) {
		if (maxValue <= minValue)
			throw new Exception("Max must be greater than min.");
		if (values == null || values.Length < 1)
			throw new Exception("values is required!");
		if (string.IsNullOrWhiteSpace(gradient))
			throw new Exception("Gradient is required!");

		if (!QueryColor.TryParseGradient(gradient, out var colors)) {
			throw new Exception($"Failed to parse gradient {gradient}!");
		}
		if (!colors.IsMultiColor) {
			throw new Exception($"Failed to parse at least two colors from gradient {gradient}!");
		}
		double range = maxValue - minValue;
		double colorRangeIncrement = 1.0d / (colors.Count - 1);
		var sb = new StringBuilder();
		for (int i = 0; i < values.Length; i++) {
			double curVal = values[i];
			if (curVal < minValue) curVal = minValue;
			if (curVal > maxValue) curVal = maxValue;

			double scaledV = (curVal - minValue) / range;

			int startIdx = (int)(scaledV / colorRangeIncrement);
			if (startIdx >= colors.Count - 1) startIdx = colors.Count - 2; // happens if we hit the max "right on"
			double startVal = startIdx * colorRangeIncrement;
			double endVal = startVal + colorRangeIncrement;
			Color start = colors[startIdx];
			Color end = colors[startIdx + 1];

			int r = (int)RasterRenderer.Interpolate(start.R, end.R, startVal, endVal, scaledV);
			int g = (int)RasterRenderer.Interpolate(start.G, end.G, startVal, endVal, scaledV);
			int b = (int)RasterRenderer.Interpolate(start.B, end.B, startVal, endVal, scaledV);

			var c = Color.FromArgb(RasterRenderer.Clamp(r), RasterRenderer.Clamp(g), RasterRenderer.Clamp(b));

			if (i > 0)
				sb.Append(",");
			sb.Append(c.ToString());
		}
		return sb.ToString();
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<GeoPointDouble> a, [ParamDesc("B")] Nullable<GeoPointDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = Measure.HaversineDistance(a.Value.Y, a.Value.X, b.Value.Y, b.Value.X);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<GeoPointDouble> a, [ParamDesc("B")] Nullable<LineSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = b.Value.DistanceToLineSet(a.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<GeoPointDouble> a, [ParamDesc("B")] Nullable<ShapeSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = b.Value.DistanceToShapeSet(a.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<LineSetDouble> a, [ParamDesc("B")] Nullable<GeoPointDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = a.Value.DistanceToLineSet(b.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<LineSetDouble> a, [ParamDesc("B")] Nullable<LineSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = a.Value.DistanceToLineSet(b.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<LineSetDouble> a, [ParamDesc("B")] Nullable<ShapeSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = b.Value.DistanceToShapeSet(a.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<ShapeSetDouble> a, [ParamDesc("B")] Nullable<GeoPointDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = a.Value.DistanceToShapeSet(b.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<ShapeSetDouble> a, [ParamDesc("B")] Nullable<LineSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = a.Value.DistanceToShapeSet(b.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<ShapeSetDouble> a, [ParamDesc("B")] Nullable<ShapeSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !b.Assigned) return default;
		var dist = a.Value.DistanceToShapeSet(b.Value, 0);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<GeoPointDouble> a, [ParamDesc("B")] Nullable<string> wkt, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !wkt.Assigned) return default;
		var b = GeoFromWKT(wkt.Value, true);
		var dist = b.Distance(a.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<LineSetDouble> a, [ParamDesc("B")] Nullable<string> wkt, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !wkt.Assigned) return default;
		var b = GeoFromWKT(wkt.Value, true);
		var dist = b.Distance(a.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<ShapeSetDouble> a, [ParamDesc("B")] Nullable<string> wkt, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!a.Assigned || !wkt.Assigned) return default;
		var b = GeoFromWKT(wkt.Value, true);
		var dist = b.Distance(a.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}


	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<string> wkt, [ParamDesc("B")] Nullable<GeoPointDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!b.Assigned || !wkt.Assigned) return default;
		var w = GeoFromWKT(wkt.Value, true);
		var dist = w.Distance(b.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<string> wkt, [ParamDesc("B")] Nullable<LineSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!b.Assigned || !wkt.Assigned) return default;
		var w = GeoFromWKT(wkt.Value, true);
		var dist = w.Distance(b.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<string> wkt, [ParamDesc("B")] Nullable<ShapeSetDouble> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!b.Assigned || !wkt.Assigned) return default;
		var w = GeoFromWKT(wkt.Value, true);
		var dist = w.Distance(b.Value);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}


	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	[MethodDesc("Geo", "Calculates the distance between two shapes. By default the result is in meters.")]
	public Nullable<double> GeoDistance([ParamDesc("A")] Nullable<string> a, [ParamDesc("B")] Nullable<string> b, [ParamDesc("Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles)")] string unit = "m") {
		if (!b.Assigned || !a.Assigned) return default;
		var aw = GeoFromWKT(a.Value, true);
		var bw = GeoFromWKT(b.Value, true);
		var dist = aw.Distance(bw);
		return new Nullable<double>(ConvertUnits(dist, unit));
	}

	private readonly long _wktCacheHash = MurmurHash3.ComputeHash("WKTCache").GetLongHashCode();
	[MethodDesc("Geo", "Creates a shape from a valid WKT string.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	// ReSharper disable once InconsistentNaming
	public Nullable<ShapeSetDouble> ShapeFromWKT(
		[ExpectedReturnType(true)] Nullable<ShapeSetDouble> _,
		Nullable<string> wkt, bool splitDateLine = true) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<ShapeSetDouble>(wktCache.GetShapeSetDoubleFromWKT(wkt.Value, splitDateLine));
	}
		
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSet> ShapeFromWKT(
		[ExpectedReturnType] Nullable<ShapeSet> _, 
		Nullable<string> wkt, bool splitDateLine = true) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<ShapeSet>(wktCache.GetShapeSetFromWKT(wkt.Value, splitDateLine));
	}

	[MethodDesc("Geo", "Creates a line from a valid WKT string.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSetDouble> LineFromWKT(
		[ExpectedReturnType(true)] Nullable<LineSetDouble> _,
		Nullable<string> wkt, bool splitDateLine = true) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<LineSetDouble>(wktCache.GetLineSetDoubleFromWKT(wkt.Value, splitDateLine));
	}
	
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<LineSet> LineFromWKT(
		[ExpectedReturnType] Nullable<LineSet> _,
		Nullable<string> wkt, bool splitDateLine = true) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<LineSet>(wktCache.GetLineSetFromWKT(wkt.Value, splitDateLine));
	}

	private IGeoTypeDouble GeoFromWKT(string wkt, bool splitDateLine = true) {
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());
		return wktCache.GetIGeoTypeFromWKT(wkt, splitDateLine);
	}

	[MethodDesc("Geo", "Returns a line segment between two shapes, attached at the midpoint of the nearest segments of each shape.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public LineSetDouble SegmentBetween(Nullable<ShapeSetDouble> shapeObjA, Nullable<ShapeSetDouble> shapeObjB, int shortSideIntersectionPointsMax = 1, int longSideIntersectionPointsMax = 1) {
		shortSideIntersectionPointsMax = Math.Min(shortSideIntersectionPointsMax, 100);
		longSideIntersectionPointsMax = Math.Min(longSideIntersectionPointsMax, 100);

		LineSetDouble shortestLine = null;
		double shortestDistance = double.MaxValue;

		Dictionary<ShapeDouble, List<ValueTuple<GeoPointDouble,GeoPointDouble>>> linesByShapeA = new Dictionary<ShapeDouble, List<ValueTuple<GeoPointDouble, GeoPointDouble>>>();
		Dictionary<ShapeDouble, List<ValueTuple<GeoPointDouble, GeoPointDouble>>> linesByShapeB = new Dictionary<ShapeDouble, List<ValueTuple<GeoPointDouble, GeoPointDouble>>>();

		shapeObjA.Value.shapes.ToList().ForEach(shape => {
			linesByShapeA.Add(shape, new List<ValueTuple<GeoPointDouble, GeoPointDouble>>());
			for (int i = 0; i < shape.Points.Length-1; ++i)
			{
				linesByShapeA[shape].Add((shape.Points[i], shape.Points[i + 1]));
			}
		});

		shapeObjB.Value.shapes.ToList().ForEach(shape => {
			linesByShapeB.Add(shape, new List<ValueTuple<GeoPointDouble, GeoPointDouble>>());
			for (int i = 0; i < shape.Points.Length-1; ++i)
			{
				linesByShapeB[shape].Add((shape.Points[i], shape.Points[i + 1]));
			}
		});

		// find all distances between LineSets
		foreach (ShapeDouble shapeA in linesByShapeA.Keys) {
			foreach (ShapeDouble shapeB in linesByShapeB.Keys) {
				foreach (ValueTuple<GeoPointDouble, GeoPointDouble> segmentA in linesByShapeA[shapeA]) {
					foreach (ValueTuple<GeoPointDouble, GeoPointDouble> segmentB in linesByShapeB[shapeB]) {
						List<double> shortSideFractions = new List<double>();
						List<double> longSideFractions = new List<double>();
						// intersectionPointsMaximum == 1 => [ 0.5 ]
						// intersectionPointsMaximum == 2 => [ 0.33, 0.67 ]
						// intersectionPointsMaximum == 3 => [ 0.25, 0.50, 0.75 ]
						for (int f = 1; f <= shortSideIntersectionPointsMax; ++f)
							shortSideFractions.Add((1.0 / (shortSideIntersectionPointsMax + 1.0)) * f);

						for (int f = 1; f <= longSideIntersectionPointsMax; ++f)
							longSideFractions.Add((1.0 / (longSideIntersectionPointsMax + 1.0)) * f);

						double segmentA_Length = Math.Pow(segmentA.Item2.X - segmentA.Item1.Y, 2) + Math.Pow(segmentA.Item2.X - segmentA.Item1.X, 2);
						double segmentB_Length = Math.Pow(segmentB.Item2.X - segmentB.Item1.Y, 2) + Math.Pow(segmentB.Item2.X - segmentB.Item1.X, 2);

						List<double> fractionsA = segmentA_Length > segmentB_Length ? longSideFractions : shortSideFractions;
						List<double> fractionsB = segmentB_Length > segmentA_Length ? longSideFractions : shortSideFractions;

						for (int i = 0; i < fractionsA.Count; ++i) {
							double fractionOfSegment_A_first = fractionsA[i];
							double fractionOfSegment_A_second = 1.0 - fractionOfSegment_A_first;

							double pointX_A = (segmentA.Item1.X * fractionOfSegment_A_first) + (segmentA.Item2.X * fractionOfSegment_A_second);
							double pointY_A = (segmentA.Item1.Y * fractionOfSegment_A_first) + (segmentA.Item2.Y * fractionOfSegment_A_second);

							for (int j = 0; j < fractionsB.Count; ++j) {
								double fractionOfSegment_B_first = fractionsB[j];						// e.g. 0.75
								double fractionOfSegment_B_second = 1.0 - fractionOfSegment_B_first;	// e.g. 0.25

								double pointX_B = (segmentB.Item1.X * fractionOfSegment_B_first) + (segmentB.Item2.X * fractionOfSegment_B_second);
								double pointY_B = (segmentB.Item1.Y * fractionOfSegment_B_first) + (segmentB.Item2.Y * fractionOfSegment_B_second);

								double distanceSquared = Math.Pow(pointX_B - pointX_A, 2) + Math.Pow(pointY_B - pointY_A, 2);

								if (distanceSquared < shortestDistance) {
									// disqualify line if it runs through either shape
									if (shapeA.FindAllIntersectionsWithMercatorProjection(pointX_A, pointY_A, pointX_B, pointY_B).Count() > 1) {
										continue;
									}

									if (shapeB.FindAllIntersectionsWithMercatorProjection(pointX_A, pointY_A, pointX_B, pointY_B).Count() > 1) {
										continue;
									}

									shortestDistance = distanceSquared;
									shortestLine = new LineSetDouble(new[] { new LineDouble(new[] { new GeoPointDouble((int)pointX_A, (int)pointY_A), new GeoPointDouble((int)pointX_B, (int)pointY_B) }) });
								}
							}
						}
					}
				}
			}
		}

		return shortestLine;
	}

	[MethodDesc("Geo", "Returns a new shape of the overlap between two shapes.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble Intersect(ShapeSetDouble a, ShapeSetDouble b) {
		var clipper = new MartinezClipper();

		return clipper.Compute(a, b, ClipOperation.Intersection);
	}

	[MethodDesc("Geo", "Returns a new line containing only the overlap of the line and the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public LineSetDouble Intersect(LineSetDouble a, ShapeSetDouble b) {
		return IntersectUtil(a, b);
	}

	[MethodDesc("Geo", "Returns a new line containing only the overlap of the line and the shape.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public static LineSetDouble IntersectUtil(LineSetDouble a, ShapeSetDouble b) {
		List<LineDouble> result = new List<LineDouble>();

		foreach (var l in a.Lines) {
			if (l.Points.Length == 0)
				continue;
			List<GeoPointDouble> acceptedPoints = new List<GeoPointDouble>(l.Points.Length);
			var lastP = l.Points[0];
			bool lastInP = b.InPoly(lastP.X, lastP.Y);
			if (lastInP)
				LineSetDouble.AddIfDistinct(acceptedPoints, lastP);

			for (int i = 1; i < l.Points.Length; i++) {
				var pt = l.Points[i];
				bool inP = b.InPoly(pt.X, pt.Y);
				// find intersection(s) of line with shape
				var intersections = b.shapes.SelectMany(s => s.FindAllIntersectionsWithMercatorProjection(lastP.X, lastP.Y, pt.X, pt.Y));

				if (lastInP && inP && !intersections.Any()) {
					LineSetDouble.AddIfDistinct(acceptedPoints, pt);
				} else {
					// order intersections by progress along line
					var orderedIntersections = intersections.OrderBy(p => a.ProgressAlongLine(p.X, p.Y).percentage).ToList();

					// if the start pt in shape, insert it
					if (lastInP)
						orderedIntersections.Insert(0, lastP);

					// if the end pt in the shape, insert it
					if (inP)
						orderedIntersections.Add(pt);

					// figure out if the first segment is in or out
					bool currentSegmentIn;
					double midx, midy;
					for (int j = 0; j < orderedIntersections.Count - 1; j++) {
						midx = (orderedIntersections[j].X + orderedIntersections[j + 1].X) / 2;
						midy = (orderedIntersections[j].Y + orderedIntersections[j + 1].Y) / 2;
						currentSegmentIn = b.InPoly(midx, midy);

						if (currentSegmentIn) {
							LineSetDouble.AddIfDistinct(acceptedPoints, orderedIntersections[j]);
							LineSetDouble.AddIfDistinct(acceptedPoints, orderedIntersections[j + 1]);
						} else {
							// our segment is not inside; close out the last line and start the next
							result.Add(new LineDouble(acceptedPoints.ToArray()));
							acceptedPoints = new List<GeoPointDouble>(l.Points.Length);
						}
					}

					if (!inP && acceptedPoints.Count > 0) {
						// close out the last line from intersections
						// our segment is not inside; close out the last line and start the next
						result.Add(new LineDouble(acceptedPoints.ToArray()));
						acceptedPoints = new List<GeoPointDouble>(l.Points.Length);
					}
				}

				lastP = pt;
				lastInP = inP;
			}

			if (acceptedPoints.Count > 0)
				result.Add(new LineDouble(acceptedPoints.ToArray()));
		}
		return new LineSetDouble(result.ToArray());
	}

	[MethodDesc("Geo", "Creates a point from a valid WKT string.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> PointFromWKT(
		[ExpectedReturnType(true)] Nullable<GeoPointDouble> _,
		Nullable<string> wkt) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<GeoPointDouble>(wktCache.GetPointDoubleFromWKT(wkt.Value));
	}
		
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPoint> PointFromWKT(
		[ExpectedReturnType] Nullable<GeoPoint> _,
		Nullable<string> wkt) {
		if (!wkt.Assigned) return default;
		var wktCache = (WKTCache)Cache.Hash.GetOrAdd(_wktCacheHash, _ => new WKTCache());

		return new Nullable<GeoPoint>(wktCache.GetPointFromWKT(wkt.Value));
	}


	[MethodDesc("Geo", "Creates a point by attempting to parse common lat/long formats, such as decimal degrees or degrees, minutes, and seconds.")]
	public Nullable<GeoPointDouble> PointFromCommonLatLongFormat(Nullable<string> input) {
		if (!input.Assigned) return default;

		if (LatLongFormatsParser.TryParseAllFormats(input.Value, out var result))
			return new Nullable<GeoPointDouble>(result);
		else
			return default;
	}

	[MethodDesc("Geo", "Creates a multipoint from a valid WKT string.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	// ReSharper disable once InconsistentNaming
	public MultipointDouble MultipointFromWKT(string wkt) {
		return new MultipointDouble(wkt);
	}

	private static double ConvertUnits(double value, string currentUnits, string newUnits) => MLUtil.Measurement.Convert(value, currentUnits, newUnits);
	private static double ConvertUnits(double value, string unit) {
		//Return as meters.
		return unit switch {
			"m" => value,
			"km" => value * 0.001,
			"mi" => value * 0.000621371,
			"ft" => value * 3.28084,
			"nmi" => value * 0.000539957,
			"nm" => value * 0.000539957,
			_ => throw new Exception($"Unknown unit: {unit}")
		};
	}

	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public double CalculateHeading(GeoPoint? firstPoint, GeoPoint? secondPoint) {
		if (firstPoint == null || secondPoint == null)
			return 0;
		return (Math.Atan2(firstPoint.Value.y - secondPoint.Value.y, firstPoint.Value.x - secondPoint.Value.x) * 180 / Math.PI + 180) % 360;
	}
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public GeoPointDouble GetXYFromWkt(string definition, long hash) {
		return (GeoPointDouble)Cache.Hash.GetOrAdd(hash, _ => new GeoPointDouble(definition));

	}
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public LineSetDouble GetLineSetFromWkt(string definition, long hash) {
		return (LineSetDouble)Cache.Hash.GetOrAdd(hash, _ => new LineSetDouble(definition, SplitAtDateLineMethod.None));

	}
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble GetShapeSetFromWkt(string definition, long hash) {
		return (ShapeSetDouble)Cache.Hash.GetOrAdd(hash, _ => new ShapeSetDouble(definition));

	}
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble GetCircleFromWkt(string definition, long hash) {
		return (ShapeSetDouble)Cache.Hash.GetOrAdd(hash, _ => new ShapeSetDouble(definition));
	}
	[MethodDesc("Geo","Returns true if the point is within the provided distance of the line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsWithin(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> lineSet, double distance) {
		if (!lineSet.Assigned || !point.Assigned)
			return default;
		return new Nullable<bool>(lineSet.Value.DistanceToLineSetWithin(point.Value, distance));
	}
	[MethodDesc("Geo","Returns true if the point is not within the provided distance of the line.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public Nullable<bool> IsNotWithin(Nullable<GeoPointDouble> point, Nullable<LineSetDouble> lineSet, double distance) {
		if (!lineSet.Assigned || !point.Assigned)
			return default;
		return new Nullable<bool>(!lineSet.Value.DistanceToLineSetWithin(point.Value, distance));
	}

	[MethodDesc("Geo", "Creates a bounding box.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public BoundingBoxDouble BBox([CacheIndex] int cacheIndex, double minX, double minY, double maxX, double maxY) {
		if (Cache.TryGetValue<BoundingBoxDouble>(cacheIndex, out var cacheValue))
			return (BoundingBoxDouble)cacheValue;
		var bbox = new BoundingBoxDouble(minX, minY, maxX, maxY, ProjectionMode.EPSG_4326);
		Cache[cacheIndex] = bbox;
		return bbox;
	}

	[MethodDesc("Geo", "Project the given point to the provided projection and return it as a WKT string.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ProjectedWKT(GeoPointDouble point,
		[ParamDesc("Projection definition.")] string projection,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null, double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {
		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, projection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY));
		var tp = geo.Project(point);
		return tp.GetWKT();
	}

	[MethodDesc("Geo", "Project the given shape to the provided projection and return it as a WKT string. Optionally clip the geometry to a region.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ProjectedWKT(ShapeSetDouble shapeSet,
		[ParamDesc("Projection definition.")] string projection,
		[ParamDesc("Clip to the defined area of use of the projection. Typically requires the projection definition to be an EPSG code.")] bool clipToAreaOfUse = false,
		[ParamDesc("If false, the clipping bounds are defined in the projection's units, not latitude and longitude.")] bool clipDimInLatLng = true,
		double? clipMinX = null, double? clipMinY = null, double? clipMaxX = null, double? clipMaxY = null,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null, double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {
		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, projection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY), clipToAreaOfUse, clipDimInLatLng, UnifiedGeometryReprojector.CreateRectangleParameter(clipMinX, clipMinY, clipMaxX, clipMaxY));
		var ts = geo.Project(shapeSet, false, false);
		return ts.shapes.Length > 0 ? ts.GetWKT() : null;
	}

	[MethodDesc("Geo", "Project the given line to the provided projection and return it as a WKT string. Optionally clip the geometry to a region.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public string ProjectedWKT(LineSetDouble lineSet,
		[ParamDesc("Projection definition.")] string projection,
		[ParamDesc("Clip to the defined area of use of the projection. Typically requires the projection definition to be an EPSG code.")] bool clipToAreaOfUse = false,
		[ParamDesc("If false, the clipping bounds are defined in the projection's units, not latitude and longitude.")] bool clipDimInLatLng = true,
		double? clipMinX = null, double? clipMinY = null, double? clipMaxX = null, double? clipMaxY = null,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null, double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {
		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, projection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY), clipToAreaOfUse, clipDimInLatLng, UnifiedGeometryReprojector.CreateRectangleParameter(clipMinX, clipMinY, clipMaxX, clipMaxY));
		var tl = geo.Project(lineSet);
		return tl.lines.Length > 0 ? tl.GetWKT() : null;
	}

	[MethodDesc("Geo", "Project the given point to the provided projection and return it.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public GeoPointDouble Project(GeoPointDouble point,
		[ParamDesc("Source projection.")] string sourceProjection,
		[ParamDesc("Target projection.")] string targetProjection,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null, double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {

		sourceProjection ??= ImportProjection.EPSG4326;

		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, sourceProjection, targetProjection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY));
		var tp = geo.Project(point);
		ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Projection, AttributeType.System)] = targetProjection;
		if (operation != null) ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Operation, AttributeType.System)] = operation;
		return tp;
	}
	[MethodDesc("Geo", "Returns the total number of points in shape.")]
	public Nullable<int> CountPoints(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return new Nullable<int>();
		return new Nullable<int>(shape.Value.shapes.Sum(p => p.Points.Length));
	}
	[MethodDesc("Geo", "Returns the total number of points in line.")]
	public Nullable<int> CountPoints(Nullable<LineSetDouble> line) {
		if (!line.Assigned) return new Nullable<int>();
		return new Nullable<int>(line.Value.lines.Sum(p => p.Points.Length));
	}
	[MethodDesc("Geo","Returns the number of points in a multipoint object.")]
	public Nullable<int> CountPoints([ParamDesc("The geometry data")] Nullable<MultipointDouble> multipoint) {
		if (!multipoint.Assigned) return default;
		return new(multipoint.Value.Points.Length);
	}
	[MethodDesc("Geo", "Project the given raster to the provided projection and return it.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public RasterBinaryWrapper Project(RasterBinaryWrapper rbw,
		[ParamDesc("Source projection.")] string sourceProjection,
		[ParamDesc("Target projection.")] string targetProjection,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null, double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {

		sourceProjection ??= ImportProjection.EPSG4326;

		var originalHeader = rbw.InRowPayload;
		var dat = rbw.DeferredLoadAsync(_core).ConfigureAwait(false).GetAwaiter().GetResult();

		var unifiedProjectionDef = UnifiedProjectionDefinitionFactory.Create(_core.ProjFacade, StrictModeUsageType.Import, ProjectionMode.REPROJECT, sourceProjection, targetProjection);
		var opEntry = unifiedProjectionDef.FindOperationEntry(_core.ProjFacade, StrictModeUsageType.Import);
		var converter = GdalCoordinateConverter.CreateForGdal(_core.ProjFacade.ProjPaths, opEntry);

		XYZToGridFloat ztgf = new XYZToGridFloat(true, false);
		var imptProj = new ImportProjection(_core?.ProjFacade?.ProjPaths, originalHeader.proj);
		var targetProj = new ImportProjection(_core?.ProjFacade?.ProjPaths, targetProjection);

		RasterResult buildResult;
		//var converter = XYZToGridFloat.RasterGridParameters.CreateConverterFromProjection(this._core.ProjFacade.ProjPaths, imptProj, targetProj, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY), out var transformParameters);
		RasterGridImportReprojection.GetIsWgs84(_core.ProjFacade?.ProjPaths, unifiedProjectionDef, out bool isWgs84Only, out bool isSourceWgs84, out _);
		var rasterTransformParameters = new RasterTransformParameters(isSourceWgs84, null, null);
		try {
			var parameters = new XYZToGridFloat.RasterGridParameters(dat.data, originalHeader) {
				converter = converter,
				transformParameters = rasterTransformParameters,
				invert = true
			};
			buildResult = ztgf.BuildGrid(parameters);
		} finally {
			converter?.Dispose();
		}

		var newHeader = new GridFloatHeader {
			byteorder = GridFloatHeader.LSB_FIRST,
			cellwidth = buildResult.cellWidth,
			cellheight = buildResult.cellHeight,
			ncols = buildResult.cols,
			nrows = buildResult.rows,
			nodata = buildResult.missing,
			gridfile = originalHeader.gridfile,
			proj = targetProjection,
			xllcorner = buildResult.minX,
			yllcorner = buildResult.minY
		};
		var newData = buildResult.data;

		var ret = new RasterBinaryWrapper(newHeader, new GridFloatData(newData));

		ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Projection, AttributeType.System)] = targetProjection;
		if (operation != null) ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Operation, AttributeType.System)] = operation;

		return ret;
	}

	[MethodDesc("Geo", "Project the given shape to the provided projection and return it. Optionally clip the geometry to a region.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble Project(ShapeSetDouble shapeSet,
		[ParamDesc("Source projection.")] string sourceProjection,
		[ParamDesc("Target projection.")] string targetProjection,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null,
		[ParamDesc("Clip to the defined area of use of the projection. Typically requires the projection definition to be an EPSG code.")] bool clipToAreaOfUse = false,
		[ParamDesc("If false, the clipping bounds are defined in the projection's units, not latitude and longitude.")] bool clipDimInLatLng = true,
		double? clipMinX = null, double? clipMinY = null, double? clipMaxX = null, double? clipMaxY = null,
		double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {

		sourceProjection ??= ImportProjection.EPSG4326;

		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, sourceProjection, targetProjection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY), clipToAreaOfUse, clipDimInLatLng, UnifiedGeometryReprojector.CreateRectangleParameter(clipMinX, clipMinY, clipMaxX, clipMaxY));
		var ts = geo.Project(shapeSet, true, false);
		ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Projection, AttributeType.System)] = targetProjection;
		if (operation != null) ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Operation, AttributeType.System)] = operation;
		return ts;
	}

	[MethodDesc("Geo", "Project the given line to the provided projection and return it. Optionally clip the geometry to a region.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public LineSetDouble Project(LineSetDouble lineSet,
		[ParamDesc("Source projection.")] string sourceProjection,
		[ParamDesc("Target projection.")] string targetProjection,
		[ParamDesc("Optional projection transformation operation definition.")] string operation = null,
		[ParamDesc("Clip to the defined area of use of the projection. Typically requires the projection definition to be an EPSG code.")] bool clipToAreaOfUse = false,
		[ParamDesc("If false, the clipping bounds are defined in the projection's units, not latitude and longitude.")] bool clipDimInLatLng = true,
		double? clipMinX = null, double? clipMinY = null, double? clipMaxX = null, double? clipMaxY = null,
		double? operationMinX = null, double? operationMinY = null, double? operationMaxX = null, double? operationMaxY = null) {

		sourceProjection ??= ImportProjection.EPSG4326;

		using var geo = new UnifiedGeometryReprojector(this._core?.ProjFacade, sourceProjection, targetProjection, operation, UnifiedGeometryReprojector.CreateRectangleParameter(operationMinX, operationMinY, operationMaxX, operationMaxY), clipToAreaOfUse, clipDimInLatLng, UnifiedGeometryReprojector.CreateRectangleParameter(clipMinX, clipMinY, clipMaxX, clipMaxY));
		var tl = geo.Project(lineSet);
		ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Projection, AttributeType.System)] = targetProjection;
		if (operation != null) ColumnAttributesToSet[new AttributeKey(AttributeConstants.ColumnAttribute_Operation, AttributeType.System)] = operation;

		return tl;
	}

	public enum BoundingBoxPart {
		MinX,
		MinLng,
		MaxX,
		MaxLng,
		MinY,
		MinLat,
		MaxY,
		MaxLat
	}
	[AggregateFunction]
	[MethodDesc("Geo", "Returns a specified part (MinX, MaxX, MinY, MaxY) of the bounding box for these points.")]
	public Nullable<double> BoundingBoxColumn(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func, BoundingBoxPart part) {
		double result = default;
		bool assigned = false;
		Nullable<GeoPointDouble> currPoint;
		foreach(var row in multiRow) {
			currPoint = func(this, AggregatesQueryableIndex.GetRowStart(row));
			if (!currPoint.Assigned) continue;

			switch (part) {
				case BoundingBoxPart.MinX:
				case BoundingBoxPart.MinLng:
					result = !assigned ? currPoint.Value.X : Math.Min(result, currPoint.Value.X);
					assigned = true;
					break;
				case BoundingBoxPart.MaxX:
				case BoundingBoxPart.MaxLng:
					result = !assigned ? currPoint.Value.X : Math.Max(result, currPoint.Value.X);
					assigned = true;
					break;
				case BoundingBoxPart.MinY:
				case BoundingBoxPart.MinLat:
					result = !assigned ? currPoint.Value.Y : Math.Min(result, currPoint.Value.Y);
					assigned = true;
					break;
				case BoundingBoxPart.MaxY:
				case BoundingBoxPart.MaxLat:
					result = !assigned ? currPoint.Value.Y : Math.Max(result, currPoint.Value.Y);
					assigned = true;
					break;
			}
		}

		return assigned ? new Nullable<double>(result) : Nullable<double>.CreateNull();
	}
		
	[MethodDesc("Geo", "Returns a specified part (MinX, MaxX, MinY, MaxY) of the bounding box.")]
	public Nullable<double> BoundingBoxColumn(Nullable<ShapeSetDouble> shape, BoundingBoxPart part) {
		if (!shape.Assigned) return default;
		return new(BoundingBoxColumnImpl(shape.Value, part));
	}
	[MethodDesc("Geo", "Returns a specified part (MinX, MaxX, MinY, MaxY) of the bounding box.")]
	public Nullable<double> BoundingBoxColumn(Nullable<LineSetDouble> line, BoundingBoxPart part) {
		if (!line.Assigned) return default;
		return new(BoundingBoxColumnImpl(line.Value, part));
	}
	[MethodDesc("Geo", "Returns a specified part (MinX, MaxX, MinY, MaxY) of the bounding box.")]
	public Nullable<double> BoundingBoxColumn(Nullable<MultipointDouble> multipoint, BoundingBoxPart part) {
		if (!multipoint.Assigned) return default;
		return new(BoundingBoxColumnImpl(multipoint.Value, part));
	}
	private double BoundingBoxColumnImpl(IGeoBounded geo, BoundingBoxPart part) {
		switch (part) {
			case BoundingBoxPart.MinX:
			case BoundingBoxPart.MinLng:
				return geo.MinX;
			case BoundingBoxPart.MaxX:
			case BoundingBoxPart.MaxLng:
				return geo.MaxX;
			case BoundingBoxPart.MinY:
			case BoundingBoxPart.MinLat:
				return geo.MinY;
			case BoundingBoxPart.MaxY:
			case BoundingBoxPart.MaxLat:
				return geo.MaxY;
			default:
				throw new ArgumentException($"Invalid bounding box column {part}", nameof(part));
		}
	}

	[MethodDesc("Geo", "Get the shape at the specified index.")]
	[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble ExtractShapeAtIdx(ShapeSetDouble shapeSet, int idx) {
		var shape = shapeSet.shapes[idx];
		var ssd = new ShapeSetDouble(new ShapeDouble[] { shape });
		return ssd;
	}


	[MethodDesc("Geo", "Returns true if the dwell times overlap.")]
	[MethodOptimization(typeof(DwellOverlapJoinOptimizer))]
	public bool DwellOverlap(Nullable<ShapeSetDouble> shapeA, Nullable<DateTimeOffset> startA, Nullable<DateTimeOffset> endA,
		Nullable<ShapeSetDouble> shapeB, Nullable<DateTimeOffset> startB, Nullable<DateTimeOffset> endB) {
		throw new NotImplementedException("Can only be used in optimized form");
	}

	[MethodDesc("Geo", "Converts the distance unit.")]
	public double ConvertDistance(double value, string from, string to) {
		var current = MLUtil.Measurement.GetMeasurementType(from);
		if (current == null)
			throw new Exception("Unable to determine type of measurment unit " + from);
		var destination = MLUtil.Measurement.GetMeasurementType(to);
		if (destination == null)
			throw new Exception("Unable to determine type of measurment unit " + to);
		return MLUtil.Measurement.Convert(value, current.Value, destination.Value);
	}

	[MethodDesc("Geo", "Converts the time unit.")]
	public double ConvertTime(double value, string from, string to) {
		var current = MLUtil.TimeMeasurement.GetMeasurementType(from);
		if (current == null)
			throw new Exception("Unable to determine type of measurement unit " + from);
		var destination = MLUtil.TimeMeasurement.GetMeasurementType(to);
		if (destination == null)
			throw new Exception("Unable to determine type of measurement unit " + to);
		return MLUtil.TimeMeasurement.Convert(value, current.Value, destination.Value);
	}

	[MethodDesc("Geo", "Creates a point from a pair of DMS coordinates.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public GeoPointDouble PointFromDMS(string lat, string lng) {
		double x = DMSCoordinate.ToDD(lng);
		double y = DMSCoordinate.ToDD(lat);

		return new GeoPointDouble(x, y);
	}

	[MethodDesc("Geo", "Calculates the bearing angle between two points in degrees.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public double BearingBetween([ParamDesc("First point")] GeoPointDouble point1, [ParamDesc("Second point")] GeoPointDouble point2) {
		Vincenty.CalculateGeodeticCurveSphere(point1.Y, point1.X, point2.Y, point2.X, out _, out var angle);
		return angle.Degrees;
	}

	[MethodDesc("Geo", "Returns a shape of <i>a</i> where the overlap with shape <i>b</i> has been removed.")]
	public ShapeSetDouble Difference(ShapeSetDouble a, ShapeSetDouble b) {
		var clipper = new MartinezClipper();

		return clipper.Compute(a, b, ClipOperation.Difference);
	}

	[MethodDesc("Geo", "Creates a point from a latitude and longitude (which are assumed to be EPSG:4326/'WGS84').")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public GeoPointDouble Point(double lat, double lng) {
		return Point(lat, lng, true);
	}

	[MethodDesc("Geo", "Creates a point from a latitude and longitude.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public GeoPointDouble Point(
		double lat, double lng,
		[ParamDesc("If true, this will apply operations like normalization")] bool isWgs84) {
		if (isWgs84) {
			if (Util.IsNormalizedToMercator(lat, lng, out double oLat, out double oLng)) {
				lat = oLat;
				lng = oLng;
			}
		}

		return new GeoPointDouble(lng, lat);
	}

	[MethodDesc("Geo", "Creates a projected point from an x and y coordinate.")]
	[ExecutionOptions(ParallelExecution.Yes)]
	public GeoPointDouble ProjectedPoint(double x, double y) {
		return new GeoPointDouble(x, y);
	}

		
	public ShapeSetDouble ShapeFromMorton(long xyMorton, int shift) {
		Morton.Corners((ulong)xyMorton, shift, out var x, out var y);
		var maxPx = GoogleGeo.geo.MAX_PIXELS_AT_ZOOM[Core.BaseZoomSetting];
		var corners = new GeoPointDouble[5];
		for(int i = 0; i < corners.Length; i++) {
			GoogleGeo.geo.getLatLngFromPixel(Common.Utils.NumberUtils.Clamp(x[i % x.Length], 0, maxPx), Common.Utils.NumberUtils.Clamp(y[i % y.Length], 0, maxPx)
				, Core.BaseZoomSetting, out var xDbl, out var yDbl);
			corners[i] = new GeoPointDouble(xDbl, yDbl);
		}

		var shape = ShapeDouble.PopulateShape(corners, false);
		return new ShapeSetDouble(new[] { shape }, holeMode: HoleMode.None, initPointInPolyOptimization: false);
	}

	[MethodDesc("Geo", "Converts UTM grid zone to geographic bounds.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public ShapeSetDouble GetUTMZoneBounds([ParamDesc("Full UTM grid zone with numeric zone and single character latitude band")] string utm) {
		var bbox = MGRSCoordinate.UtmGridZoneToWgsBounds(utm);

		return new ShapeSetDouble(new ShapeDouble[]
		{
			new ShapeDouble(new PointDouble[]
			{
				bbox.GetCorner(0),
				bbox.GetCorner(1),
				bbox.GetCorner(2),
				bbox.GetCorner(3),
				bbox.GetCorner(0),
			})
		});
	}

	[MethodDesc("Geo", "Returns the min lat for this shape.")]
	public Nullable<double> MinLat(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		return new Nullable<double>((new BoundingBoxDouble(shape.Value)).minY);
	}
	[MethodDesc("Geo", "Returns the max lat for this shape.")]
	public Nullable<double> MaxLat(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		return new Nullable<double>((new BoundingBoxDouble(shape.Value)).maxY);
	}
	[MethodDesc("Geo", "Returns the min lng for this shape.")]
	public Nullable<double> MinLng(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		return new Nullable<double>((new BoundingBoxDouble(shape.Value)).minX);
	}
	[MethodDesc("Geo", "Returns the max lng for this shape.")]
	public Nullable<double> MaxLng(Nullable<ShapeSetDouble> shape) {
		if (!shape.Assigned) return default;
		return new Nullable<double>((new BoundingBoxDouble(shape.Value)).maxX);
	}

	[MethodDesc("Geo", "Creates a single shape from the provided shapes.")]
	[AggregateFunction]
	public Nullable<ShapeSetDouble> Union(IList<int> multiRow, ExpressionScopeDelegate<ShapeSetDouble> shapeFunc) {
		if (multiRow.Count == 0) return new Nullable<ShapeSetDouble>(new ShapeSetDouble(Array.Empty<ShapeDouble>()));
		if (multiRow.Count == 1) return shapeFunc(this, AggregatesQueryableIndex.GetRowStart(multiRow[0]));

		var values = new ShapeSetDouble[multiRow.Count];
		Nullable<ShapeSetDouble> tmp;
		for(int i = 0; i < multiRow.Count; i++) {
			tmp = shapeFunc(this, AggregatesQueryableIndex.GetRowStart(multiRow[i]));
			if (!tmp.Assigned)
				return Nullable<ShapeSetDouble>.CreateNull();
			else
				values[i] = tmp.Value;
		}

		var clipper = new MartinezClipper();

		//we're going to combine all the shapes except the first into a single shapeset, so we can make one call to the clipper
		var combinedShapes = values.Skip(1).SelectMany(s => s.shapes).ToArray();
		var cipSS = new ShapeSetDouble(combinedShapes, HoleMode.None, false);

		return new Nullable<ShapeSetDouble>(clipper.Compute(values[0], cipSS, ClipOperation.Union));
	}

	[MethodDesc("Geo", "Removes any holes from the given shape.")]
	public Nullable<ShapeSetDouble> RemoveHoles(Nullable<ShapeSetDouble> shape) {
		return !shape.Assigned ? shape : new Nullable<ShapeSetDouble>(shape.Value.WithInnerRingsRemoved());
	}

	#region Clipper expressions

	private List<List<ClipLib.GeoPoint>> GetOrBuildClipperShapes(IList<int> multiRow, [CacheIndex] int cacheIndex, ExpressionScopeDelegate<ShapeSetDouble> getShapeSet) {
		// convert double lat/lon coordinates from ShapeSetDouble to integer ClipLib.GeoPoint via ClipperFixedPoint; build new or retrieve existing from cache
		List<List<ClipLib.GeoPoint>> clipShapes; // analogous to ShapeSetDouble.shapes
		if (!PartitionCache.TryGetValue(cacheIndex, out clipShapes)) {
			clipShapes = new List<List<ClipLib.GeoPoint>>();
			foreach (var shapeSet in multiRow.Select(row => getShapeSet(this, AggregatesQueryableIndex.GetRowStart(row))).Where(result => result.Assigned)) {
				foreach (var shape in shapeSet.Value.shapes) {
					clipShapes.Add(shape.Points.Select(geoPointDouble => new ClipperFixedPoint.Point(geoPointDouble.X, geoPointDouble.Y).IntPoint).ToList());
				}
			}
			PartitionCache[cacheIndex] = clipShapes;
		}

		return clipShapes;
	}

	private ShapeSetDouble GetShapeSetDoubleFromClipperShapes(List<List<ClipLib.GeoPoint>> clipShapes) {
		var shapeArray = new ShapeDouble[clipShapes.Count];
		for (int i = 0; i < clipShapes.Count; i++) {
			// convert integer ClipLib.GeoPoint to ShapeSetDouble
			shapeArray[i] = ShapeDouble.PopulateShape(clipShapes[i].Select(ipt => new ClipperFixedPoint.Point(ipt).PointD).ToArray());
		}
		return new ShapeSetDouble(shapeArray);
	}

	private Nullable<ShapeSetDouble> RunClipperLibOnSingleShapes(Nullable<ShapeSetDouble> subject, Nullable<ShapeSetDouble> clip, ClipLib.ClipType clipType) {
		if (!subject.Assigned)
			return default;
		if (!clip.Assigned)
			return subject;

		ClipLib.Clipper clipper = new();

		foreach (var subjectShape in subject.Value.Shapes) {
			clipper.AddPath(subjectShape.Points.Select(geoPointDouble => new ClipperFixedPoint.Point(geoPointDouble.X, geoPointDouble.Y).IntPoint).ToList(),
				ClipLib.PolyType.ptSubject, true);
		}

		foreach (var clipShape in clip.Value.Shapes) {
			clipper.AddPath(clipShape.Points.Select(geoPointDouble => new ClipperFixedPoint.Point(geoPointDouble.X, geoPointDouble.Y).IntPoint).ToList(),
				ClipLib.PolyType.ptClip, true);
		}

		List<List<ClipLib.GeoPoint>> solution = new();
		clipper.Execute(clipType, solution, ClipLib.PolyFillType.pftNonZero, ClipLib.PolyFillType.pftNonZero);
		return new Nullable<ShapeSetDouble>(this.GetShapeSetDoubleFromClipperShapes(solution));
	}

	[MethodDesc("Geo", "Returns a shape set intersecting the shapes of <i>subject</i> and <i>clip</i>.")]
	public Nullable<ShapeSetDouble> IntersectClipper(Nullable<ShapeSetDouble> subject, Nullable<ShapeSetDouble> clip) {
		return this.RunClipperLibOnSingleShapes(subject, clip, ClipLib.ClipType.ctIntersection);
	}

	[MethodDesc("Geo", "Returns a line set intersecting the lines of <i>subject</i> and <i>clip</i> using <i>epsilon</i> in meters.")]
	public Nullable<LineSetDouble> IntersectClipper(Nullable<LineSetDouble> subject, Nullable<LineSetDouble> clip, Nullable<double> epsilonMeters) {
		if (!subject.Assigned || !clip.Assigned)
			return default;

		var clipShape = Buffer(clip, epsilonMeters, false);
		if (!clipShape.Assigned)
			return default;

		ClipLib.Clipper clipper = new();
		foreach (var s in subject.Value.Lines) {
			clipper.AddPath(s.Points.Select(p => new ClipperFixedPoint.Point(p.X, p.Y).IntPoint).ToList(), ClipLib.PolyType.ptSubject, false);
		}
		foreach (var s in clipShape.Value.shapes) {
			clipper.AddPath(s.Points.Select(p => new ClipperFixedPoint.Point(p.X, p.Y).IntPoint).ToList(), ClipLib.PolyType.ptClip, true);
		}
		ClipLib.PolyTree pTree = new();
		clipper.Execute(ClipLib.ClipType.ctIntersection, pTree, ClipLib.PolyFillType.pftNonZero, ClipLib.PolyFillType.pftNonZero);
		var solution = ClipLib.Clipper.OpenPathsFromPolyTree(pTree);
		var lineArray = new LineDouble[solution.Count];
		for (int i = 0; i < solution.Count; i++) {
			lineArray[i] = new LineDouble(solution[i].Select(ipt => new ClipperFixedPoint.Point(ipt).PointD).ToArray());
		}
		return new Nullable<LineSetDouble>(new LineSetDouble(lineArray));
	}
		
	[MethodDesc("Geo", "Removes points to simplify the shape.")]
	public ShapeSetDouble Simplify(
		ShapeSetDouble shape, 
		[ParamDesc(_toleranceDescription)] double tolerance = 1,
		[ParamDesc("If true, it will decrease the threshold for small shapes to avoid oversimplifying")] bool doNotOverSimplify = true) {
			
		if (tolerance is < 0 or > 500)
			throw new ArgumentException("Threshold must be between 0.0 and 500.0");

		var output = shape.Simplify(tolerance * 1000);
		if (doNotOverSimplify) {
			var inputPointCount = shape.Shapes.Max(s => s.Points?.Length);
			while (inputPointCount > 6 && output.Shapes.Max(s => s.Points?.Length) <= 6 && tolerance > 1) {
				tolerance /= 2.0;
				output = shape.Simplify(tolerance * 1000);
			}
		}
		return output;
	}

	[MethodDesc("Geo", "Removes points to simplify the line (using Douglas-Peucker algorithm).")]
	public LineSetDouble Simplify(
		LineSetDouble lineSet,
		[ParamDesc(_toleranceDescription)] double tolerance = 1) {
			
		if (tolerance < 0 || tolerance > 500)
			throw new ArgumentException("Threshold must be between 0.0 and 500.0");

		return SimplifyDouglasPeucker(lineSet,tolerance * 1000);
	}

	private LineSetDouble SimplifyDouglasPeucker(LineSetDouble lineSet, double epsilon)
	{
		var lines = lineSet.Lines;
		var newLines = new LineDouble[lines.Length];
		for (var i = 0; i < lines.Length; i++) {
			var input = new List<PointDouble>(lines[i].Points.Length);
				
			for (int k = 0; k < lines[i].Points.Length; k++) {
				var p = lines[i].Points[k];
				input.Add(new PointDouble(p.X, p.Y));
			}
				
			var output = ShapeReduce.Reduce(input, epsilon, false)
				.Select(p => new GeoPointDouble(p.X, p.Y))
				.ToArray();
				
			newLines[i] = new LineDouble(output);
		}
		return new LineSetDouble(newLines);
	}

	[MethodDesc("Geo", "Returns a shape set <i>subject</i> minus shape set <i>clip</i>.")]
	public Nullable<ShapeSetDouble> DifferenceClipper(Nullable<ShapeSetDouble> subject, Nullable<ShapeSetDouble> clip) {
		return this.RunClipperLibOnSingleShapes(subject, clip, ClipLib.ClipType.ctDifference);
	}

	[AggregateFunction]
	[MethodDesc("Geo", "Returns a shape set <i>subject</i> minus shape sets <i>clip</i>.")]
	public Nullable<ShapeSetDouble> DifferenceClipper(IList<int> multiRow, [CacheIndex] int cacheIndex, Nullable<ShapeSetDouble> subject, ExpressionScopeDelegate<ShapeSetDouble> clip) {
		if (!subject.Assigned)
			return default;
			
		List<List<ClipLib.GeoPoint>> clipShapes = this.GetOrBuildClipperShapes(multiRow, cacheIndex, clip);

		ClipLib.Clipper clipper = new();
		foreach (var subjectShape in subject.Value.shapes) {
			clipper.AddPath(subjectShape.Points.Select(geoPointDouble => new ClipperFixedPoint.Point(geoPointDouble.X, geoPointDouble.Y).IntPoint).ToList(),
				ClipLib.PolyType.ptSubject, true);
		}

		clipper.AddPaths(clipShapes, ClipLib.PolyType.ptClip, true);

		List<List<ClipLib.GeoPoint>> solution = new();
		clipper.Execute(ClipLib.ClipType.ctDifference, solution, ClipLib.PolyFillType.pftNonZero, ClipLib.PolyFillType.pftNonZero);
		return new Nullable<ShapeSetDouble>(this.GetShapeSetDoubleFromClipperShapes(solution));
	}

	[AggregateFunction]
	[MethodDesc("Geo", "Returns a shape of <i>subject</i> where the overlap with shapes <i>clip</i> has been removed.")]
	public Nullable<ShapeSetDouble> RemoveUnionClipper(IList<int> multiRow, [CacheIndex] int cacheIndex, Nullable<ShapeSetDouble> subject, ExpressionScopeDelegate<ShapeSetDouble> clip) {
		// there does not appear to be any difference in the implementation of DifferenceClipper() and RemoveUnionClipper() in qv1
		return this.DifferenceClipper(multiRow, cacheIndex, subject, clip);
	}

	[MethodDesc("Geo", "Returns a shape set combining the shapes of <i>subject</i> and <i>clip</i>.")]
	public Nullable<ShapeSetDouble> UnionClipper(Nullable<ShapeSetDouble> subject, Nullable<ShapeSetDouble> clip) {
		return this.RunClipperLibOnSingleShapes(subject, clip, ClipLib.ClipType.ctUnion);
	}

	[AggregateFunction]
	[MethodDesc("Geo", "Returns a shape set combining the shapes of <i>data</i>.")]
	public Nullable<ShapeSetDouble> UnionClipper(IList<int> multiRow, [CacheIndex] int cacheIndex, ExpressionScopeDelegate<ShapeSetDouble> clip) {
		List<List<ClipLib.GeoPoint>> clipShapes = this.GetOrBuildClipperShapes(multiRow, cacheIndex, clip);
		List<List<ClipLib.GeoPoint>> unionShapes = clipShapes.Count > 4096
			? this.UnionClipper_Parallel(clipShapes)
			: this.UnionClipper_Execute(clipShapes);
		return new Nullable<ShapeSetDouble>(this.GetShapeSetDoubleFromClipperShapes(unionShapes));
	}

	private List<List<ClipLib.GeoPoint>> UnionClipper_Execute(List<List<ClipLib.GeoPoint>> clipShapes) {
		// add clipper shapes to the path; the first one is the subject
		ClipLib.Clipper clipper = new();
		bool first = true;
		foreach (var clipShape in clipShapes) {
			clipper.AddPath(clipShape, first ? ClipLib.PolyType.ptSubject : ClipLib.PolyType.ptClip, true);
			first = false;
		}

		List<List<ClipLib.GeoPoint>> solution = new();
		clipper.Execute(ClipLib.ClipType.ctUnion, solution, ClipLib.PolyFillType.pftNonZero, ClipLib.PolyFillType.pftNonZero);
		return solution;
	}

	private List<List<ClipLib.GeoPoint>> UnionClipper_Parallel(List<List<ClipLib.GeoPoint>> clipShapes) {
		// separate clipper shapes into task groups
		var maxParallelTasks = Util.ParallelOpt().MaxDegreeOfParallelism;
		var shapesPerGroup = clipShapes.Count / maxParallelTasks;
		var clipGroups = Enumerable.Range(0, maxParallelTasks).Select(_ => new List<List<ClipLib.GeoPoint>>()).ToArray();
		for (int i = 0; i < clipShapes.Count; i++) {
			clipGroups[Common.Utils.NumberUtils.Clamp(i / shapesPerGroup, 0, maxParallelTasks - 1)].Add(clipShapes[i]);
		}

		// union each group to itself
		ConcurrentBag<List<List<ClipLib.GeoPoint>>> clipGroupUnions = new();
		System.Threading.Tasks.Parallel.ForEach(clipGroups, (clipGroupShapes) => clipGroupUnions.Add(this.UnionClipper_Execute(clipGroupShapes)));

		// flatten the union groups into a single shape and take its union
		return this.UnionClipper_Execute(clipGroupUnions.SelectMany(grp => grp).ToList());
	}

	[MethodDesc("Geo", "Returns a cleaned up version of shape set <i>subject</i> using distance param <i>dist</i>.")]
	public Nullable<ShapeSetDouble> CleanupClipper(Nullable<ShapeSetDouble> subject, Nullable<double> dist) {
		if (!subject.Assigned)
			return default;
		if (!dist.Assigned)
			dist = new Nullable<double>(1.415d);

		List<List<ClipLib.GeoPoint>> inputGeom = new();
		foreach (var subjectShape in subject.Value.shapes) {
			inputGeom.Add(subjectShape.Points.Select(geoPointDouble => new ClipperFixedPoint.Point(geoPointDouble.X, geoPointDouble.Y).IntPoint).ToList());
		}

		var solution = ClipLib.Clipper.CleanPolygons(inputGeom, dist.Value);
		return new Nullable<ShapeSetDouble>(this.GetShapeSetDoubleFromClipperShapes(solution));
	}

	#endregion

	private bool GetNormallyDistributedEllipseMeta(IList<int> multiRow, ExpressionScopeDelegate<GeoPointDouble> func,
		int nstd, int minimumPoints, out GeoPointDouble meanPoint, out double elWidth, out double elHeight, out double theta) {
			
		meanPoint = new GeoPointDouble();
		elWidth = 0;
		elHeight = 0;
		theta = 0;

		int n = multiRow.Count;
		if (n < minimumPoints) return false;

		var points = new List<GeoPointDouble>(multiRow.Count);
		Nullable<GeoPointDouble> tmpPt;
		foreach (var rowNbr in multiRow) {
			tmpPt = func(this, AggregatesQueryableIndex.GetRowStart(rowNbr));
			if (tmpPt.Assigned)
				points.Add(tmpPt.Value);
		}


		//find covariance matrix
		double xSum = 0;
		double ySum = 0;
		GeoPoint tmpPt2;
		foreach (var pt in points) {
			tmpPt2 = pt.ToGeoPoint();
			xSum += tmpPt2.X;
			ySum += tmpPt2.Y;
		}
		double xAvg = xSum / n;
		double yAvg = ySum / n;
		GoogleGeo.geo.getLatLngFromPixel(xAvg, yAvg, Core.BaseZoomSetting, out var lng, out var lat);
		meanPoint = new(lng, lat);

		double xxSum = 0;
		double yySum = 0;
		double xySum = 0;
		double xDiff, yDiff;
		foreach (var pt in points) {
			tmpPt2 = pt.ToGeoPoint();
			xDiff = tmpPt2.X - xAvg;
			yDiff = tmpPt2.Y - yAvg;

			xxSum += xDiff * xDiff;
			yySum += yDiff * yDiff;
			xySum += xDiff * yDiff;
		}
			
		double xxAvg = xxSum / n;
		double yyAvg = yySum / n;
		double xyAvg = xySum / n;

		var cov2 = Matrix<double>.Build.Dense(2, 2);
		cov2[0, 0] = xxAvg;
		cov2[0, 1] = xyAvg;
		cov2[1, 0] = xyAvg;
		cov2[1, 1] = yyAvg;

		//find the eigenvalue decomposition
		var evd = cov2.Evd();

		if (evd.EigenValues[0].Real <= 0 || evd.EigenValues[1].Real <= 0) return false;

		theta = Math.Atan2(evd.EigenVectors[1, 0], evd.EigenVectors[0, 0]);
		elWidth = nstd * Math.Sqrt(evd.EigenValues[0].Real);
		elHeight = nstd * Math.Sqrt(evd.EigenValues[1].Real);

		if (elWidth < 0 || elHeight < 0) return false;

		return true;
	}

	[AggregateFunction]
	[MethodDesc("Geo", "Gets the first point of intersection of the edge of the shape set with the line sets in <i>lines</li>, in the order they are encountered traversing ls1.")]
	public Nullable<GeoPointDouble> GetFirstIntersectionPoint(IList<int> multiRow, Nullable<ShapeSetDouble> shape, ExpressionScopeDelegate<LineSetDouble> lines, int metersTolerance = 0) {
		if (!shape.Assigned) return default;
		LineSetDouble ls1 = new(shape.Value.shapes.Select(t => new LineDouble(t.Points.Clone().ToArray())).ToArray());

		var lsGroup = new List<LineSetDouble>();
		foreach(var ls in multiRow.Select(row => lines(this, AggregatesQueryableIndex.GetRowStart(row)))) {
			if (ls.Assigned)
				lsGroup.Add(ls.Value);
		}

		var intersections = CalculateOrderedIntersections(ls1, lsGroup, metersTolerance, true);
		if (intersections.Count == 0) return default;
		return new(intersections[0].IntersectPoint);
	}

	[AggregateFunction]
	[MethodDesc("Geo", "Gets the first point of intersection of ls1 with the line sets in <i>lines</i>, in the order they are encountered traversing ls1.")]
	public Nullable<GeoPointDouble> GetFirstIntersectionPoint(IList<int> multiRow, Nullable<LineSetDouble> ls1, ExpressionScopeDelegate<LineSetDouble> lines, int metersTolerance = 0) {
		if (!ls1.Assigned) return default;

		var lsGroup = new List<LineSetDouble>();
		foreach (var ls in multiRow.Select(row => lines(this, AggregatesQueryableIndex.GetRowStart(row)))) {
			if (ls.Assigned)
				lsGroup.Add(ls.Value);
		}

		var intersections = CalculateOrderedIntersections(ls1.Value, lsGroup, metersTolerance, true);
		if (intersections.Count == 0) return default;
		return new(intersections[0].IntersectPoint);
	}

	[MethodDesc("Geo", "Gets the first point of intersection of ls1 with the line set ls2, in the order they are encountered traversing ls1.")]
	public Nullable<GeoPointDouble> GetFirstIntersectionPoint(Nullable<LineSetDouble> ls1, Nullable<LineSetDouble> ls2, int metersTolerance = 0) {
		if (!ls1.Assigned || !ls2.Assigned) return default;
		var intersection = CalculateFirstIntersection(ls1.Value, ls2.Value);
		return intersection.HasValue ? new(intersection.Value.IntersectPoint) : default;
	}

	private IList<LineIntersectResult> CalculateOrderedIntersections(LineSetDouble ls1, IEnumerable<LineSetDouble> targets, int metersTolerance = 0, bool firstOnly = false) {
		var intersections = new List<LineIntersectResult>();
		double curDistanceTraveled = 0;
		GeoPointDouble prevPoint, prevTargetPoint;
		double dx, dy, curLen, intersectX, intersectY
			, sourceMinX, sourceMinY, sourceMaxX, sourceMaxY
			, targetMinX, targetMinY, targetMaxX, targetMaxY
			, partialDist;
		foreach(var curLine in ls1.lines) {
			prevPoint = curLine.Points[0];
			foreach(var curPoint in curLine.Points.Skip(1)) {
				dx = curPoint.X - prevPoint.X;
				dy = curPoint.Y - prevPoint.Y;
				curLen = Math.Sqrt(dx * dx + dy * dy);
				int idx = 0;
				foreach(var curTarget in targets) {
					foreach(var curTargetLine in curTarget.lines) {
						prevTargetPoint = curTargetLine.Points[0];
						foreach(var curTargetPoint in curTargetLine.Points.Skip(1)) {
							if(GeometryUtil.LineLineIntersection(prevPoint.X, prevPoint.Y, curPoint.X, curPoint.Y, prevTargetPoint.X, prevTargetPoint.Y
								   , curTargetPoint.X, curTargetPoint.Y, out intersectX, out intersectY)) {
								GetBoundsFromPoints(prevPoint, curPoint, out sourceMinX, out sourceMinY, out sourceMaxX, out sourceMaxY, metersTolerance);
								GetBoundsFromPoints(prevTargetPoint, curTargetPoint, out targetMinX, out targetMinY, out targetMaxX, out targetMaxY, metersTolerance);
								if (intersectX >= sourceMinX && intersectX <= sourceMaxX
															 && intersectY >= sourceMinY && intersectY <= sourceMaxY
															 && intersectX >= targetMinX && intersectX <= targetMaxX
															 && intersectY >= targetMinY && intersectY <= targetMaxY) {

									if (dx > dy)
										partialDist = ((intersectX - prevPoint.X) / dx) * curLen;
									else
										partialDist = ((intersectY - prevPoint.Y) / dy) * curLen;
									intersections.Add(new(curDistanceTraveled + partialDist, idx, new GeoPointDouble(intersectX, intersectY)));
									if (firstOnly)
										return intersections;
								}
							}
						}
					}
					idx++;
				}
				curDistanceTraveled += curLen;
				prevPoint = curPoint;
			}
		}

		intersections.Sort();
		return intersections;
	}
	private LineIntersectResult? CalculateFirstIntersection(LineSetDouble ls1, LineSetDouble ls2) {
		if (!GeometryUtil.BoxOverlap(ls1.MinX, ls1.MinY, ls1.MaxX, ls1.MaxY, ls2.MinX, ls2.MinY, ls2.MaxX, ls2.MaxY)) return null;

		GeoPointDouble prevPoint, prevTargetPoint;
		double intersectX, intersectY;
		foreach(var curLine in ls1.lines) {
			if (!curLine.OverlapQuick(ls2.MinX, ls2.MinY, ls2.MaxX, ls2.MaxY)) continue;
			prevPoint = curLine.Points[0];
			foreach(var curPoint in curLine.Points.Skip(1)) {
				foreach(var curTargetLine in ls2.lines) {
					prevTargetPoint = curTargetLine.Points[0];
					foreach(var curTargetPoint in curTargetLine.Points) {
						if (GeometryUtil.LineSegmentIntersection(prevPoint.X, prevPoint.Y, curPoint.X, curPoint.Y
								, prevTargetPoint.X, prevTargetPoint.Y, curTargetPoint.X, curTargetPoint.Y, out intersectX, out intersectY))
							return new(0, 0, new GeoPointDouble(intersectX, intersectY));

						prevTargetPoint = curTargetPoint;
					}
				}
			}
		}

		return null;
	}

	public readonly struct LineIntersectResult: IComparable<LineIntersectResult> {
		public readonly double DistanceTraveled;
		public readonly int Index;
		public readonly GeoPointDouble IntersectPoint;

		public LineIntersectResult(double distanceTraveled, int index, GeoPointDouble intersectPoint) {
			DistanceTraveled = distanceTraveled;
			Index = index;
			IntersectPoint = intersectPoint;
		}

		public int CompareTo(LineIntersectResult other) => DistanceTraveled.CompareTo(other.DistanceTraveled);
	}

	[MethodDesc("Geo", "Calculates the forward azimuth angle between two points in degrees on the WGS84 ellipsoid.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public Nullable<double> ForwardAzimuthOnWGS84Ellipsoid([ParamDesc("First point")] Nullable<GeoPointDouble> point1, [ParamDesc("Second point")] Nullable<GeoPointDouble> point2, [ParamDesc("Tolerance of convergence in meters (default = 1 cm)")] double tolerance = 0.01) {
		if (!point1.Assigned || !point2.Assigned) return default;
		Vincenty.CalculateGeodeticCurveEllipsoid(new PointDouble(point1.Value.X, point1.Value.Y), new PointDouble(point2.Value.X, point2.Value.Y), tolerance, out double distance, out var startAzimuth, out var endAzimuth);
		return new(startAzimuth.Degrees);
	}

	public Nullable<bool> InProjectedBoundsApprox(
		[CacheIndex] int cacheIndex,
		Nullable<GeoPointDouble> pt,
		double bbMinX,
		double bbMinY,
		double bbMaxX,
		double bbMaxY,
		string projection,
		double bufferMeters = 0.0
	) {
		if (!pt.Assigned)
			return Nullable<bool>.CreateNull();

		GeoBounds[] bounds;
		if (Cache.Initialized[cacheIndex])
			bounds = (GeoBounds[])Cache[cacheIndex];
		else {
			bounds = CreateWgs84DataBounds(bbMinX, bbMinY, bbMaxX, bbMaxY, projection, bufferMeters);
			Cache[cacheIndex] = bounds;
		}

		var inBounds = BoundsContainsWgs84Pt(bounds, pt.Value);
		return new Nullable<bool>(inBounds);
	}

	[MethodDesc("Geo", "Encode the geohash of a point.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public Nullable<string> GeohashEncode([ParamDesc("Point")] Nullable<GeoPointDouble> point, [ParamDesc("Precision")] Nullable<int> precision) {
		if (!point.Assigned) return default;

		var p = point.Value;
		int? prec = precision.Assigned ? precision.Value : null;
		return new(MapLarge.Engine.Geo.Geohash.Encode(p.Y, p.X, prec));
	}
	[MethodDesc("Geo", "Decode the geohash of a point.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public Nullable<GeoPointDouble> GeohashDecode([ParamDesc("Geohash")] Nullable<string> geohash) {
		if (!geohash.Assigned) return default;

		var g = geohash.Value;
		return new(MapLarge.Engine.Geo.Geohash.Decode(g));
	}
	[MethodDesc("Geo", "Get the bounds of a geohash.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public Nullable<ShapeSetDouble> GeohashBounds([ParamDesc("Geohash")] Nullable<string> geohash) {
		if (!geohash.Assigned) return default;

		var g = geohash.Value;
		var d = MapLarge.Engine.Geo.Geohash.Bounds(g);
		var points = d.GetCornerPoints(false).Select(o => new GeoPointDouble(o.X, o.Y)).ToArray();
		var shape = ShapeDouble.PopulateShape(points, calculateCentroid: true);
		var shapeSet = ShapeSetDouble.PopulateShapeSet(new[] { shape });
		return new(shapeSet);
	}
	[MethodDesc("Geo", "Get the adjacent geohash in a specified direction.")]
	[ExecutionOptions(ParallelExecution.Yes, canBeCalculatedDuringCompileTime: true)]
	public Nullable<string> GeohashAdjacent([ParamDesc("Geohash")] Nullable<string> geohash, [ParamDesc("Direction (N, E, S, W)")] Nullable<string> direction) {
		if (!geohash.Assigned || !direction.Assigned) return default;

		var g = geohash.Value;
		if (!Enum.TryParse<Geohash.Direction>(direction.Value, true, out var dir)) {
			return default;
		}

		return new(MapLarge.Engine.Geo.Geohash.Adjacent(g, dir));
	}

	/// <summary>
	/// Returns WGS84 bounds that completely contains the data that should be traversed to
	/// rasterize the tracks. These bounds contain the projected bbox for the raster result,
	/// plus a "maxPointDistKm" buffer around it. If the resulting WGS84 bounds spans the
	/// date-line, then it will be split and returned as an array of two bounding boxes.
	/// </summary>
	private GeoBounds[] CreateWgs84DataBounds(double bbMinX, double bbMinY, double bbMaxX, double bbMaxY, string projection, double bufferMeters = 0.0) {
		var isWGS84 = ImportProjection.IsProjectionEPSG4326(_core.ProjFacade.ProjPaths, projection);
		if (isWGS84) {
			// add padding for maxPointDistKm to expand Y so that the track
			// gets drawn all the way to the edge of the raster.
			// then clamp Y to the poles
			var latDeg = Measure.ConvertMetersToDegreesMax(bufferMeters, 0);
			var minY = Math.Max(-90, bbMinY - latDeg);
			var maxY = Math.Min(90, bbMaxY + latDeg);

			// if Y is at the poles, then use the full world for X bounds
			// because the poles are points
			if (minY == -90 || maxY == 90) {
				return new[] { new GeoBounds(-180, minY, 180, maxY) };
			}

			// add padding for maxPointDistKm to expand X so that the track
			// gets drawn all the way to the edge of the raster
			var farthestY = Math.Max(Math.Abs(minY), Math.Abs(maxY));
			var lngDeg = Measure.ConvertMetersToDegreesMax(bufferMeters, farthestY);
			var minX = bbMinX - lngDeg;
			var maxX = bbMaxX + lngDeg;

			// the bounding box is greater than the entire world, so clamp to -180/180
			if (maxX - minX >= 360) {
				return new[] { new GeoBounds(-180, minY, 180, maxY) };
			}

			// the bounding box's minX crosses the dateline, so split into two
			// bounding boxes
			if (minX < -180) {
				return new[] {
					new GeoBounds(minX + 360, minY, 180, maxY),
					new GeoBounds(-180, minY, maxX, maxY)
				};
			}

			// the bounding box's maxX crosses the dateline, so split into two
			// bounding boxes
			if (maxX > 180) {
				return new[] {
					new GeoBounds(minX, minY, 180, maxY),
					new GeoBounds(-180, minY, maxX - 360, maxY)
				};
			}

			// no clamping required, use the bounds plus the padding
			return new[] { new GeoBounds(minX, minY, maxX, maxY) };
		}
		else {
			using var inverseConverter = GdalCoordinateConverter.CreateForGdal(_core.ProjFacade.ProjPaths, projection, ImportProjection.WGS84);
			const int steps = 4;
			var stepsPlusOne = steps + 1;
			var convertX = new double[stepsPlusOne * stepsPlusOne];
			var convertY = new double[convertX.Length];
			var convertZ = new double[convertX.Length];

			var xStep = (bbMaxX - bbMinX) / steps;
			var yStep = (bbMaxY - bbMinY) / steps;
			for (var (j, y, k) = (0, bbMinY, 0); j <= steps; j++, y += yStep) {
				for (var (i, x) = (0, bbMinX); i <= steps; i++, x += xStep, k++) {
					convertX[k] = x;
					convertY[k] = y;
				}
			}

			inverseConverter.Transform(convertX.Length, convertX, convertY, convertZ, false, true);

			var builder = new BoundingBoxDoubleBuilder(ProjectionMode.REPROJECT);
			var fudgeDist = bufferMeters * Math.Sqrt(2);
			var latDeg = Measure.ConvertMetersToDegreesMax(fudgeDist, 0);
			for (var i = 0; i < convertX.Length; i++) {
				var lng = convertX[i];
				var lat = convertY[i];
				builder.AddPoint(lng, lat);

				// Ensure that the WGS84 data bounding box is large enough to include the
				// prev/next track points just outside our projected bounding box so that
				// we can properly draw our lines.
				builder.AddPoint(lng, lat + latDeg);
				builder.AddPoint(lng, lat - latDeg);
				var lngDeg = Math.Min(180, Measure.ConvertMetersToDegreesMax(fudgeDist, lat));
				builder.AddPoint(lng + lngDeg, lat);
				builder.AddPoint(lng - lngDeg, lat);
			}
			var bounds = builder.ToBoundingBoxDouble();

			if (bounds.MinY < -90) {
				return new[] { new GeoBounds(-180, -90, 180, Math.Min(90, bounds.MaxY)) };
			}
			if (bounds.MaxY > 90) {
				return new[] { new GeoBounds(-180, Math.Max(-90, bounds.MinY), 180, 90) };
			}
			if (bounds.MaxX - bounds.MinX >= 360) {
				return new[] { new GeoBounds(-180, bounds.MinY, 180, bounds.MaxY) };
			}
			if (bounds.MinX < -180) {
				return new[] {
					new GeoBounds(bounds.MinX + 360, bounds.MinY, 180, bounds.MaxY),
					new GeoBounds(-180, bounds.MinY, bounds.MaxX, bounds.MaxY)
				};
			}
			if (bounds.MaxX > 180) {
				return new[] {
					new GeoBounds(bounds.MinX, bounds.MinY, 180, bounds.MaxY),
					new GeoBounds(-180, bounds.MinY, bounds.MaxX - 360, bounds.MaxY)
				};
			}
			return new[] { new GeoBounds(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY) };
		}
	}

	private bool BoundsContainsWgs84Pt(GeoBounds[] bounds, GeoPointDouble wgs84Pt) {
		if (bounds == null || bounds.Length <= 0)
			return true;

		for (var i = 0; i < bounds.Length; i++) {
			var bbox = bounds[i];
			if (bbox.MinX <= wgs84Pt.X && wgs84Pt.X <= bbox.MaxX
									   && bbox.MinY <= wgs84Pt.Y && wgs84Pt.Y <= bbox.MaxY)
				return true;
		}
		return false;
	}

	[MethodDesc("Geo", "Converts a point to a gridding system, optionally specifying a precision.")]
	public Nullable<string> ToGridSystem(Nullable<GeoPointDouble> point, string gridSystem, int precision = -1) {
		if (!point.Assigned)
			return Nullable<string>.CreateNull();
		var grid = GridSystemFactory.GetGridSystem(gridSystem);
		if (precision == -1)
			precision = grid.DefaultPrecision;
		return new Nullable<string>(grid.ConvertToGrid(point.Value.X, point.Value.Y, precision));
	}

	[MethodDesc("Geo", "Converts a gridding system coordinate to a point.")]
	public Nullable<GeoPointDouble> FromGridSystem(Nullable<string> coordinate, string gridSystem, Nullable<bool> center) {
		if (!coordinate.Assigned)
			return Nullable<GeoPointDouble>.CreateNull();
		var grid = GridSystemFactory.GetGridSystem(gridSystem);
		PointD point = grid.ConvertFromGrid(coordinate.Value, center.Assigned ? center.Value : false);
		return new Nullable<GeoPointDouble>(new GeoPointDouble(point.X, point.Y));
	}

	#region Counting functions V2

	/// <summary>
	/// Returns the number of shapes inside the shapeset
	/// </summary>
	/// <param name="geoObj">The geometry data</param>
	/// <param name="includeHoles">Only applicable to polygons. If true, this will count a hole as a shape (default false)</param>
	/// <returns></returns>
	[MethodDesc("Geo", "Returns the number of shapes inside the shapeset (default excludes the holes).")]
	public int NShapes(
		[ParamDesc("The geometry data")] IMLPathGeometry geoObj,
		[ParamDesc("Only applicable to polygons. If true, this will count a hole as a shape (default false)")] bool includeHoles = false) {
		// Limitation: If there is a polygon inside a polygon, this will be counted
		var result = 0;

		if (geoObj != null) result = geoObj.Length;

		var shapeSet = geoObj as ShapeSetDouble;
		if (shapeSet != null && !includeHoles) {
			// If the holes are to be excluded (default behavior) we have to count
			var count = 0;

			foreach (var shape in shapeSet.shapes) {
				if (!shape.isHole) count++;
			}

			result = count;
		}

		return result;
	}

	/// <summary>
	/// Returns the number of points
	/// </summary>
	/// <param name="geoObj">The geometry data</param>
	/// <returns></returns>
	[MethodDesc("Geo", "Returns the number of points.")]
	public int NPoints([ParamDesc("The geometry data")] IMLGeoPath geoObj) {
		// Point version
		var result = 0;

		if (geoObj != null) {
			result = geoObj.Length;
		}

		return result;
	}

	/// <summary>
	/// Returns the number of points for a single point..
	/// </summary>
	/// <param name="geoObj">The geometry data</param>
	/// <returns></returns>
	[MethodDesc("Geo", "Returns the number of points.")]
	public int NPoints([ParamDesc("The geometry data")] GeoPointDouble geoPoint) {
		// Point version
		var result = 0;

		if (!geoPoint.Equals(new GeoPointDouble())) result = 1;

		return result;
	}

	/// <summary>
	/// Returns the number of points
	/// </summary>
	/// <param name="geoSet">The geometry data</param>
	/// <returns></returns>
	[MethodDesc("Geo", "Returns the number of points.")]
	public Nullable<int> NPoints([ParamDesc("The geometry data")] Nullable<MultipointDouble> multipoint) {
		if (!multipoint.Assigned) return default;

		var m = multipoint.Value;
		return new(m.Points.Length);
	}

	/// <summary>
	/// Returns the number of points inside the shapeset
	/// </summary>
	/// <param name="geoObj">The geometry data</param>
	/// <returns></returns>
	[MethodDesc("Geo", "Returns the number of points inside the shapeset.")]
	public int NPoints([ParamDesc("The geometry data")] IMLPathGeometry geoObj) {
		var result = 0;

		if (geoObj != null) result = geoObj.Length;

		var shapeSet = geoObj as ShapeSetDouble;
		var lineSet = geoObj as LineSetDouble;
		if (shapeSet != null) {
			var count = 0;

			foreach (var shape in shapeSet.shapes) {
				if (shape.Points != null) count += shape.Points.Length;
			}

			result = count;
		} else if (lineSet != null) {
			var count = 0;

			foreach (var line in lineSet.lines) {
				if (line.Points != null) count += line.Points.Length;
			}

			result = count;
		}

		return result;
	}


	#endregion Counting functions
		
	[MethodDesc("Geo", "Creates the largest inscribed circle for a given shapeset.")]
	public Nullable<ShapeSetDouble> InscribedCircle(
		[ParamDesc("shapeset", "The shapeset to operate on")] Nullable<ShapeSetDouble> shapeset,
		[ParamDesc("precision", "Precision of the grid to use in the creation of the inscribed circle.  Smaller values will be more precisce and increase computation time. Use zero to auto-calculate a best guess on size.  Negative values are not allowed. Default: 0.0001")] double precision = 0.0001) {
		if (!shapeset.Assigned) return Nullable<ShapeSetDouble>.CreateNull();
		if (precision < 0) throw new ArgumentOutOfRangeException(nameof(precision), "Value cannot be negative");
		double p = precision == 0 ? 0.00001 : precision;
		(double centerX, double centerY, double radius) =
			GeoNumerics.Fx.FindLargestInscribedCircle(shapeset.Value.Shapes, p);
		return new Nullable<ShapeSetDouble>(new ShapeSetDouble(new [] {
			ShapeDouble.Circle(centerX, centerY, radius)
		}));
	}
		
	[MethodDesc("Geo", "Calculates the position of the largest inscribed circle for a given shapeset.")]
	public Nullable<(double centerX, double centerY, double radius)> InscribedCircleCoords(
		[ParamDesc("shapeset", "The shapeset to operate on")] Nullable<ShapeSetDouble> shapeset,
		[ParamDesc("precision", "Precision of the grid to use in the creation of the inscribed circle.  Smaller values will be more precisce and increase computation time. Use zero to auto-calculate a best guess on size.  Negative values are not allowed. Default: 0.0001")] double precision = 0.0001) {
		if (!shapeset.Assigned) return Nullable<(double centerX, double centerY, double radius)>.CreateNull();
		if (precision < 0) throw new ArgumentOutOfRangeException(nameof(precision), "Value cannot be negative");
		double p = precision == 0 ? 0.00001 : precision;
		(double centerX, double centerY, double radius) circleData =
			GeoNumerics.Fx.FindLargestInscribedCircle(shapeset.Value.Shapes, p);
		return new Nullable<(double centerX, double centerY, double radius)>(circleData);
	}

	#region Multipoint Stitching and Extraction Methods
	[MethodDesc("Geo", "Returns the last point in a MultiPoint geometry, or null if none")]
	public Nullable<GeoPointDouble> LastPoint(Nullable<MultipointDouble> mpt) {
		if (!mpt.Assigned || mpt.Value.Points.Length == 0)
			return Nullable<GeoPointDouble>.CreateNull();
		return new Nullable<GeoPointDouble>(mpt.Value.Points[mpt.Value.Points.Length - 1]);
	}

	[MethodDesc("Geo", "Returns the first point in a MultiPoint geometry, or null if none")]
	public Nullable<GeoPointDouble> FirstPoint(Nullable<MultipointDouble> mpt) {
		if (!mpt.Assigned || mpt.Value.Points.Length == 0)
			return Nullable<GeoPointDouble>.CreateNull();
		return new Nullable<GeoPointDouble>(mpt.Value.Points[0]);
	}

	[MethodDesc("Geo", "Takes multiple grouped MultiPoint geometries, and stitches them into a single MultiPoint geometry in the order provided by the query.  Returns null if no geometries in the grouping.")]
	[AggregateFunction]
	public Nullable<MultipointDouble> Stitch(IList<int> multiRow, ExpressionScopeDelegate<MultipointDouble> mptFunc) {
		if(multiRow.Count == 0)
			return Nullable<MultipointDouble>.CreateNull();

		var lst = new List<GeoPointDouble>();
		for(int i=0; i< multiRow.Count; i++) {
			var rowStart = AggregatesQueryableIndex.GetRowStart(multiRow[i]);
			var curMpt = mptFunc(this, rowStart);
			if (!curMpt.Assigned || curMpt.Value.Length == 0) continue;
			var mpt = curMpt.Value;
			for (int k = 0; k < mpt.Points.Length; k++)
				lst.Add(mpt.Points[k]);
		}
		return new Nullable<MultipointDouble>(new MultipointDouble(lst.ToArray()));
	}

	[MethodDesc("Geo", "Converts the provided Multipoint geometry into a Line.  Returns null if input geometry is null.")]
	public Nullable<LineSetDouble> CreateLine(Nullable<MultipointDouble> mpt) {
		if (!mpt.Assigned)
			return Nullable<LineSetDouble>.CreateNull();
		var lst = new List<GeoPointDouble>();
		var mptVal = mpt.Value;
		for (int i = 0; i < mptVal.Points.Length; i++)
			lst.Add(mptVal.Points[i]);
		return new Nullable<LineSetDouble>(new LineSetDouble(new[]{new LineDouble(lst.ToArray()) }));
	}
	#endregion
}
