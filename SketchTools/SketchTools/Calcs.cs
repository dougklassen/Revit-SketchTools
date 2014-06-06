using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    public static class Calcs
    {
        /// <summary>
        /// Get the minimum bounding point of a set of points
        /// </summary>
        /// <param name="points">a set of points</param>
        /// <returns>The minimum bounding point</returns>
        public static XYZ GetMinPoint(params XYZ[] points)
        {
            if (points.Length==0) return null;

            Double minX = Double.MaxValue;
            Double minY = Double.MaxValue;
            Double minZ = Double.MaxValue;

            foreach (var p in points)
	        {
                minX = p.X < minX ? p.X : minX;
                minY = p.Y < minY ? p.Y : minY;
                minZ = p.Z < minZ ? p.Z : minZ;
	        }

            return new XYZ(minX, minY, minZ);
        }

        /// <summary>
        /// Get the maximum bounding point of a set of points
        /// </summary>
        /// <param name="points">a set of points</param>
        /// <returns>The maximum bounding point</returns>
        public static XYZ GetMaxPoint(params XYZ[] points)
        {
            if (points.Length == 0) return null;

            Double maxX = Double.MinValue;
            Double maxY = Double.MinValue;
            Double maxZ = Double.MinValue;

            foreach (var p in points)
            {
                maxX = p.X > maxX ? p.X : maxX;
                maxY = p.Y > maxY ? p.Y : maxY;
                maxZ = p.Z > maxZ ? p.Z : maxZ;
            }

            return new XYZ(maxX, maxY, maxZ);
        }

        /// <summary>
        /// Calculates this centerpoint of an outline
        /// </summary>
        /// <param name="outline"></param>
        /// <returns></returns>
        public static XYZ GetCenterPoint(Outline outline)
        {
            return new XYZ(
                (outline.MaximumPoint.X + outline.MinimumPoint.X) / 2,
                (outline.MaximumPoint.Y + outline.MinimumPoint.Y) / 2,
                (outline.MaximumPoint.Z + outline.MinimumPoint.Z) / 2);
        }

        /// <summary>
        /// Project a point into the coordinate system of a view
        /// </summary>
        /// <param name="point">The point to be projected</param>
        /// <param name="view">The view to project onto</param>
        /// <returns>The point in the coordinate system of the view</returns> 
        public static XYZ ProjectToView(XYZ point, View view)
        {
            BoundingBoxXYZ cropBox = view.CropBox;
            Transform viewTransform = cropBox.Transform.Inverse;

            return viewTransform.OfPoint(point);
        }

        /// <summary>
        /// Project a point from the coordinate system of a view
        /// </summary>
        /// <param name="point">The point to be projected</param>
        /// <param name="view">The view to project from</param>
        /// <returns>The point in the coordinate system of the view</returns>
        public static XYZ ProjectFromView(XYZ point, View view)
        {
            BoundingBoxXYZ cropBox = view.CropBox;
            Transform modelTransform = cropBox.Transform;

            return modelTransform.OfPoint(point);
        }

        /// <summary>
        /// An extension method to give a string description of an XYZ
        /// </summary>
        /// <param name="someXYZ">The XYZ to describe</param>
        /// <returns>A formatted string giving the coordinates of the XYZ</returns>
        public static String Describe(this XYZ someXYZ)
        {
            return String.Format("<{0:n2}, {1:n2}, {2:n2}>", someXYZ.X, someXYZ.Y, someXYZ.Z);
        }
    }
}
