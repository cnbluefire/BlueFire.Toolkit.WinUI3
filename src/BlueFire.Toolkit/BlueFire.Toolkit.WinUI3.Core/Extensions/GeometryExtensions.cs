using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    /// <summary>
    /// <see cref="Microsoft.UI.Xaml.Media.Geometry"/> Extensions.
    /// </summary>
    public static class GeometryExtensions
    {
        /// <summary>
        /// Deep clone a geometry.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static Geometry? CloneGeometry(this Geometry? geometry) => geometry switch
        {
            EllipseGeometry ellipse => new EllipseGeometry()
            {
                Center = ellipse.Center,
                RadiusX = ellipse.RadiusX,
                RadiusY = ellipse.RadiusY,
                Transform = geometry.Transform
            },
            LineGeometry line => new LineGeometry()
            {
                EndPoint = line.EndPoint,
                StartPoint = line.StartPoint,
                Transform = line.Transform
            },
            RectangleGeometry rect => new RectangleGeometry()
            {
                Rect = rect.Rect,
                Transform = rect.Transform
            },
            GeometryGroup group => new GeometryGroup()
            {
                Children = CreateCollection<GeometryCollection, Geometry>(group.Children.Select(CloneGeometry)),
                FillRule = group.FillRule,
                Transform = group.Transform
            },
            PathGeometry path => new PathGeometry()
            {
                Figures = CreateCollection<PathFigureCollection, PathFigure>(path.Figures.Select(ClonePathFigure)),
                FillRule = path.FillRule,
                Transform = path.Transform,
            },
            _ => null
        };

        private static PathFigure? ClonePathFigure(PathFigure? figure) => figure switch
        {
            null => null,
            _ => new PathFigure()
            {
                IsClosed = figure.IsClosed,
                IsFilled = figure.IsFilled,
                Segments = CreateCollection<PathSegmentCollection, PathSegment>(figure.Segments.Select(ClonePathSegment)),
                StartPoint = figure.StartPoint,
            }
        };

        private static PathSegment? ClonePathSegment(PathSegment? segment) => segment switch
        {
            ArcSegment arc => new ArcSegment()
            {
                IsLargeArc = arc.IsLargeArc,
                RotationAngle = arc.RotationAngle,
                Point = arc.Point,
                Size = arc.Size,
                SweepDirection = arc.SweepDirection
            },
            BezierSegment bezier => new BezierSegment()
            {
                Point1 = bezier.Point1,
                Point2 = bezier.Point2,
                Point3 = bezier.Point3,
            },
            LineSegment line => new LineSegment()
            {
                Point = line.Point
            },
            PolyBezierSegment polyBezier => new PolyBezierSegment()
            {
                Points = CreateCollection<PointCollection, Point>(polyBezier.Points),
            },
            PolyLineSegment polyLine => new PolyLineSegment()
            {
                Points = CreateCollection<PointCollection, Point>(polyLine.Points),
            },
            PolyQuadraticBezierSegment polyQuadraticBezier => new PolyQuadraticBezierSegment()
            {
                Points = CreateCollection<PointCollection, Point>(polyQuadraticBezier.Points),
            },
            QuadraticBezierSegment quadraticBezier => new QuadraticBezierSegment()
            {
                Point1 = quadraticBezier.Point1,
                Point2 = quadraticBezier.Point2,
            },
            _ => null
        };

        private static TCollection CreateCollection<TCollection, TItem>(IEnumerable<TItem?> items) where TCollection : IList<TItem>, new()
        {
            var collection = new TCollection();
            foreach (var item in items)
            {
                if (item != null)
                {
                    collection.Add(item);
                }
            }
            return collection;
        }
    }
}