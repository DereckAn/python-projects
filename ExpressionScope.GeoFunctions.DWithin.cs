#nullable enable
using System;
using MapLarge.Common.GeoNumerics.Crs;
using MapLarge.Engine.Database;
using MapLarge.Engine.Query.Expressions;
using MapLarge.GeoNumerics.DataTypes;
using MapLarge.GeoNumerics.Geo;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace MapLarge.Engine.QueryEngine.Expressions {
	public partial class ExpressionScope {

		private const string EPSG_4326 = "EPSG:4326";
		private const string LINESTRING = "LINESTRING";
		private const string POLYGON = "POLYGON";
		private const string MULTIPOLYGON = "MULTIPOLYGON";
		// POINT DWithin METHODS
		public Nullable<bool> DWithin(Nullable<GeoPointDouble> point, Nullable<GeoPointDouble> xy2, double distance) {
			Nullable<double> d = DistanceBetween(point, xy2, "M");
			return !d.Assigned ? default : new Nullable<bool>(d.Value <= distance);
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<GeoPointDouble> point, ShapeSetDouble? shapeSet, double distance) {
			if (!point.Assigned || shapeSet == null) return default;
			return new Nullable<bool>(shapeSet.DistanceToShapeSetWithin(point.Value, distance));
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<GeoPointDouble> point, LineSetDouble? lineSet, double distance) {
			if (!point.Assigned || lineSet == null) return default;
			return new Nullable<bool>(lineSet.DistanceToLineSetWithin(point.Value, distance));
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<GeoPointDouble> point, Nullable<MultipointDouble> mp, double distance) {
			return DWithin(mp, point, distance);
		}
		
		[MethodDesc("Geo", "Determines if a point is within the provided point, line, or polygon and the added distance.")]
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<GeoPointDouble> point, string? xy2, string projection, string originalProjection, string distance, string shapeType, long hash) {
			if (!point.Assigned || xy2 == null) return default;
			ShapeSetDouble withDistance;
			if (Cache.Hash.TryGetValue(Convert.ToInt64(hash), out var cacheValue)) {
				withDistance = (ShapeSetDouble)cacheValue;
			} else {
				withDistance = shapeType.ToUpper() == "CIRCLE" ? 
					AddDistanceToCircle(xy2, projection, originalProjection, Convert.ToInt32(distance)) : 
					AddDistanceToPolygon(xy2, projection, originalProjection, Convert.ToDouble(distance), shapeType);
				Cache.Hash[Convert.ToInt64(hash)] = withDistance;
			}
			return new Nullable<bool>(InPoly(point, withDistance, "M"));
		}
		
		// SHAPE DWithin METHODS
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ShapeSetDouble> shapeToCheck, ShapeSetDouble? shapeWithin, double distance) {
			if (!shapeToCheck.Assigned || shapeWithin == null) return default;
			return new Nullable<bool>(shapeToCheck.Value.OverlapPoly(shapeWithin) || shapeToCheck.Value.DistanceToShapeSetWithin(shapeWithin, distance));
		}
		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ShapeSetDouble> shapeToCheck, LineSetDouble? lineWithin, double distance) {
			if (!shapeToCheck.Assigned || lineWithin == null) return default;
			
			return new Nullable<bool>(shapeToCheck.Value.OverlapLine(lineWithin) || shapeToCheck.Value.DistanceToShapeSetWithin(lineWithin, distance));
		}
		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ShapeSetDouble> shapeSet, Nullable<GeoPointDouble> point, double distance) {
			if (!shapeSet.Assigned || !point.Assigned)
				return default;
			//DistanceToShapeSetWithin checks InPoly
			return new Nullable<bool>(shapeSet.Value.DistanceToShapeSetWithin(point.Value, distance));
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ShapeSetDouble> shapeSet, Nullable<MultipointDouble> multipoint, double distance) {
			return DWithin(multipoint, shapeSet, distance);
		}
		
		// LINE DWithin METHODS
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<LineSetDouble> lineSet, Nullable<ShapeSetDouble> shapeSet, double distance) {
			if (!lineSet.Assigned || !shapeSet.Assigned)
				return default;
			return DWithin(shapeSet,lineSet.Value, distance);
		}
		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<LineSetDouble> lineSet, Nullable<LineSetDouble> lineSetWithin, double distance) {
			if (!lineSet.Assigned || !lineSetWithin.Assigned)
				return default;
			return new Nullable<bool>(lineSetWithin.Value.DistanceToLineSetWithin(lineSet.Value, distance));
		}
		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<LineSetDouble> lineSet, Nullable<GeoPointDouble> point, double distance) {
			if (!lineSet.Assigned || !point.Assigned)
				return default;
			return new Nullable<bool>(lineSet.Value.DistanceToLineSetWithin(point.Value, distance));
		}
		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<LineSetDouble> lineSet, Nullable<MultipointDouble> multipoint, double distance) {
			return DWithin(multipoint, lineSet, distance);
		}
		
		// MULTIPOINT DWithin METHODS		
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<MultipointDouble> multipoint, Nullable<ShapeSetDouble> shapeSet, double distance) {
			if (!multipoint.Assigned || !shapeSet.Assigned) return default;
			ShapeSetDouble s = shapeSet.Value;
			foreach (GeoPointDouble point in multipoint.Value.Points) {
				if (s.DistanceToShapeSetWithin(point, distance)) return new Nullable<bool>(true);
			}
			return new Nullable<bool>(false);
		}
		
		public Nullable<bool> DWithin(Nullable<MultipointDouble> multipoint, Nullable<LineSetDouble> lineSet, double distance) {
			if (!multipoint.Assigned || !lineSet.Assigned) return default;
			LineSetDouble l = lineSet.Value;
			foreach (GeoPointDouble point in multipoint.Value.Points) {
				if (l.DistanceToLineSetWithin(point, distance)) return new Nullable<bool>(true);
			}
			return new Nullable<bool>(false);
		}
		
		public Nullable<bool> DWithin(Nullable<MultipointDouble> mp, Nullable<GeoPointDouble> p, double distance) {
			if (!mp.Assigned || !p.Assigned) return default;
			GeoPointDouble p2 = p.Value;
			foreach (GeoPointDouble p1 in mp.Value.Points) {
				double distanceBetween = Measure.Distance(p1.Y, p1.X, p2.Y, p2.X, 'M');
				if (distanceBetween <= distance) {
					return new Nullable<bool>(true);
				}
			}
			return new Nullable<bool>(false);
		}
		
		public Nullable<bool> DWithin(Nullable<MultipointDouble> mp1, Nullable<MultipointDouble> mp2, double distance) {
			if (!mp1.Assigned || !mp2.Assigned) return default;
			foreach (GeoPointDouble p1 in mp1.Value.Points) {
				foreach (GeoPointDouble p2 in mp2.Value.Points) {
					double distanceBetween = Measure.Distance(p1.Y, p1.X, p2.Y, p2.X, 'M');
					if (distanceBetween <= distance) {
						return new Nullable<bool>(true);
					}
				}
			}
			return new Nullable<bool>(false);
		}

		//These raster/image expressions are invoked when the raster/image column has a projection column attribute.
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<RasterBinaryWrapper> rbw, string? xy2, string projection, string originalProjection, string distance, string shapeType, long hash) {
			if (!rbw.Assigned || xy2 == null) {
				return default;
			}
			var rasterBox = rbw.Value.GetBoundingBox();

			return RasterDWithin(rasterBox, xy2, projection, originalProjection, distance, shapeType);
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ImageBinaryWrapper> ibw, string? xy2, string projection, string originalProjection, string distance, string shapeType, long hash) {
			if (!ibw.Assigned || xy2 == null) {
				return default;
			}
			var rasterBox = ibw.Value.GetBoundingBox();
			return RasterDWithin(rasterBox, xy2, projection, originalProjection, distance, shapeType);
		}


		//Helper function for DWithin when there is a projection column attribute
		private Nullable<bool> RasterDWithin(ShapeSetDouble rasterBbox, string shape, string projection, string originalProjection, string distance, string shapeType) {
			var st = shapeType.ToUpper();

			if (st == "POINT") {
				var centerPoint = new Nullable<GeoPointDouble>(new GeoPointDouble(shape));

				var areaToCheck = CreateCircle(centerPoint, Convert.ToDouble(distance));
				var overlaps = rasterBbox.OverlapPoly(areaToCheck.Value);
				return new Nullable<bool>(overlaps);
			}


			var withDistance = st == "CIRCLE" ?
					AddDistanceToCircle(shape, projection, originalProjection, Convert.ToInt32(distance)) :
					AddDistanceToPolygon(shape, projection, originalProjection, Convert.ToDouble(distance), shapeType);

			return new Nullable<bool>(rasterBbox.OverlapPoly(withDistance));
		}


		//These raster expressions are invoked when there is no projection column attribute for backwards compatibility.
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<RasterBinaryWrapper> rbw, Nullable<GeoPointDouble> point, double distance) {
			if (!rbw.Assigned || !point.Assigned) {
				return default;
			}
			var rasterBox = rbw.Value.GetBoundingBox();

			return new Nullable<bool>(rasterBox.DistanceToShapeSetWithin(point.Value, distance));
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<RasterBinaryWrapper> rbw, Nullable<LineSetDouble> lineWithin, double distance) {
			if (!rbw.Assigned || !lineWithin.Assigned) {
				return default;
			}

			var rasterBox = rbw.Value.GetBoundingBox();
			var shape = GetCoords(lineWithin.Value.GetWKT());
			return RasterDWithin(rasterBox, shape, ProjectionMode.REPROJECT.ToString(), EPSG_4326, distance.ToString(), LINESTRING);
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<RasterBinaryWrapper> rbw, Nullable<ShapeSetDouble> shapeWithin, double distance) {
			if (!rbw.Assigned || !shapeWithin.Assigned) {
				return default;
			}

			var rasterBox = rbw.Value.GetBoundingBox();
			var shape = GetCoords(shapeWithin.Value.GetWKT());
			return RasterDWithin(rasterBox, shape, ProjectionMode.REPROJECT.ToString(), EPSG_4326, distance.ToString(), POLYGON);
		}

		//These image expressions are invoked when there is no projection column attribute for backward compatability.
		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ImageBinaryWrapper> ibw, Nullable<GeoPointDouble> point, double distance) {
			if (!ibw.Assigned || !point.Assigned) {
				return default;
			}
			var rasterBox = ibw.Value.GetBoundingBox();

			return new Nullable<bool>(rasterBox.DistanceToShapeSetWithin(point.Value, distance));
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ImageBinaryWrapper> ibw, Nullable<LineSetDouble> lineWithin, double distance) {
			if (!ibw.Assigned || !lineWithin.Assigned) {
				return default;
			}

			var rasterBox = ibw.Value.GetBoundingBox();
			var shape = GetCoords(lineWithin.Value.GetWKT());
			return RasterDWithin(rasterBox, shape, ProjectionMode.REPROJECT.ToString(), EPSG_4326, distance.ToString(), LINESTRING);
		}

		[ExecutionOptions(canBeCalculatedDuringCompileTime: true)]
		public Nullable<bool> DWithin(Nullable<ImageBinaryWrapper> ibw, Nullable<ShapeSetDouble> shapeWithin, double distance) {
			if (!ibw.Assigned || !shapeWithin.Assigned) {
				return default;
			}

			var rasterBox = ibw.Value.GetBoundingBox();
			var shape = GetCoords(shapeWithin.Value.GetWKT());
			return RasterDWithin(rasterBox, shape, ProjectionMode.REPROJECT.ToString(), EPSG_4326, distance.ToString(), POLYGON);
		}

		private string GetCoords(string wkt) {
			return wkt.Replace(LINESTRING, "", StringComparison.InvariantCultureIgnoreCase)
				.Replace(MULTIPOLYGON, "", StringComparison.InvariantCultureIgnoreCase)
				.Replace(POLYGON, "", StringComparison.InvariantCultureIgnoreCase).Trim().TrimStart('(').TrimEnd(')');
		}
	}
}