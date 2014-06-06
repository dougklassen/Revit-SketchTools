using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    /// <summary>
    /// Encapsulates information about a view placed on an ASK sheet, including its ownerView if it has one.
    /// Information about the location of the view will be stored in the Marker annotation
    /// </summary>
    public class SketchView
    {
        //the document in which the sketchView lives
        private Document dbDoc;

        /// <summary>
        /// The View Element of the SketchView
        /// </summary>
        private View AsView
        {
            get
            {
                return (View)dbDoc.GetElement(this.Id);
            }
        }

        /// <summary>
        /// The Id of the SketchView
        /// </summary>
        public ElementId Id { get; set; }

        /// <summary>
        /// The ViewName of SketchView in the document
        /// </summary>
        public String Name
        {
            get
            {
                return this.AsView.ViewName;
            }
        }

        /// <summary>
        /// Returns the insertion point of the ViewPort if the view is placed on an ASK ViewSheet
        /// </summary>
        public XYZ ViewportInsertionPoint
        {
            get
            {
                if (!this.AsView.IsPlacedSketchView())
                {
                    throw new InvalidOperationException(this.Name + " is not placed on an ASK sheet");
                }

                FilteredElementCollector c = new FilteredElementCollector(dbDoc);
                c.OfCategory(BuiltInCategory.OST_Viewports);
                var q = from Viewport vp in c where this.Id.IntegerValue == vp.ViewId.IntegerValue select vp;
                Outline viewPortOutline = ((Viewport)q.First()).GetBoxOutline();

                return Calcs.GetCenterPoint(viewPortOutline);
            }
        }

        /// <summary>
        /// The reference view from which the SketchView was duplicated. Derived by determining what view
        /// the Marker is placed in. If the Marker is invalid, return null;
        /// </summary>
        public ElementId OriginViewId
        {
            get
            {
                if (null != MarkerId)
                {
                    if (-1 != MarkerId.IntegerValue)
                    {
                        Element marker = dbDoc.GetElement(MarkerId);
                        return marker.OwnerViewId;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The ASK on which the SketchView is placed, or null if the view isn't placed
        /// </summary>
        public ASK Ask
        {
            get
            {
                List<ASK> asks = dbDoc.GetAsks();
                var q = from ASK a in asks where a.SketchViews.Contains(this) select a;
                if (q.Count() > 0)
                {
                    return q.First();
                }
                return null;
            }
        }

        /// <summary>
        /// The ElementId of the ASK MARKER generic annotation marking the area covered by the ASK.
        /// Retrieve the value stored in the eData of the view.
        /// </summary>
        public ElementId MarkerId
        {
            //todo: replace this property with an AskMarker object
            get
            {
                View sketchView = this.AsView;
                Schema sketchSchema = AskSchemas.GetAskMarkerSchema();

                Entity markerDataEntity = sketchView.GetEntity(sketchSchema);
                if (markerDataEntity.IsValid())
                {
                    return markerDataEntity.Get<ElementId>("MarkerId");
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The center point of the area covered by the sketch. Not meaningful if there is no
        /// OwnerView and Marker
        /// </summary>
        public XYZ Origin
        {
            get
            {
                if (null != MarkerId)
                {
                    if (-1 != MarkerId.IntegerValue)
                    {
                        FamilyInstance marker = (FamilyInstance)dbDoc.GetElement(MarkerId);
                        LocationPoint location = (LocationPoint)marker.Location;
                        return location.Point;
                    }
                }

                throw new InvalidOperationException("MarkerId is not defined");
            }
        }

        /// <summary>
        /// The width in feet of the area covered by the ASK in the model space and in decimal feet
        /// </summary>
        public Double CoverageWidth
        {
            get
            {
                if (null != MarkerId)
                {
                    if (-1 != MarkerId.IntegerValue)
                    {
                        FamilyInstance marker = (FamilyInstance)dbDoc.GetElement(MarkerId);
                        Parameter widthParam = marker.ParametersMap.get_Item("WIDTH");

                        return widthParam.AsDouble() * this.AsView.Scale;
                    }
                }

                throw new InvalidOperationException("MarkerId is not defined");
            }
        }

        /// <summary>
        /// The height in feet of the area covered by the ASK
        /// </summary>
        public Double CoverageHeight
        {
            get
            {
                if (null != MarkerId)
                {
                    if (-1 != MarkerId.IntegerValue)
                    {
                        FamilyInstance marker = (FamilyInstance)dbDoc.GetElement(MarkerId);
                        Parameter heightParam = marker.ParametersMap.get_Item("HEIGHT");

                        return heightParam.AsDouble() * this.AsView.Scale;
                    }
                }

                throw new InvalidOperationException("MarkerId is not defined");
            }
        }

        /// <summary>
        /// private constructor, SketchViews can only be accessed by the static factory methods and accessor methods
        /// </summary>
        private SketchView() { }

        /// <summary>
        /// Returns a SketchView object for an existing view
        /// </summary>
        /// <param name="view">the View from which to derive the SketchView</param>
        /// <returns>A SketchView object for the view</returns>
        public static SketchView GetSketchView(View view)
        {
            SketchView retrievedSketchView = new SketchView();
            retrievedSketchView.dbDoc = view.Document;
            retrievedSketchView.Id = view.Id;

            return retrievedSketchView;
        }

        /// <summary>
        /// Creates a DupeView by duplicating an entire View. The coverage and origin of DraftingViews is determined by originView.Outline
        /// All others
        /// Will not check whether view exceeds max width and height
        /// Can only be called within an open transaction.
        /// </summary>
        /// <param name="originView">the originating view</param>
        /// <param name="markerText">The name to be used for the new View and the ASK MARKER</param>
        /// <param name="sheetDimensions">The size of the sheet that the </param>
        /// <returns>A reference to the duplicated View</returns>
        public static View CreateDupeSketchView(View originView, String markerText, SheetDimensions sheetDimensions)
        {
            View dupeView = originView.DuplicateToSketchView(markerText);
            ElementId markerId = originView.PlaceMarker(markerText);
            dupeView.WriteMarkerEData(markerId);
            return dupeView;
        }

        /// <summary>
        /// Duplicates the specified region of the specified view. Can only be called within an open transaction.
        /// </summary>
        /// <param name="originView">The view to be duplicated</param>
        /// <param name="p1">The first point of the region to be duplicated, in world coordinates</param>
        /// <param name="p2">The second point of the region to be duplicated, in world coordinates</param>
        /// <returns>The view that was created</returns>
        public static View CreateDupeSketchView(View originView, String tagString, SheetDimensions sheetDimensions, XYZ p1, XYZ p2)
        {
            //todo: create a DupeView from the region of the specified view
            //set the origin to the center of the picked region
            XYZ origin = new XYZ(
                (p1.X + p2.X) / 2,
                (p1.Y + p2.Y) / 2,
                (p1.Z + p2.Z) / 2);

            //the maximum width and height of the area covered that will fit on a sheet in world space
            Double maxWidth = (sheetDimensions.Width - sheetDimensions.LeftMargin - sheetDimensions.RightMargin) * originView.Scale;
            Double maxHeight = (sheetDimensions.Height - sheetDimensions.TopMargin - sheetDimensions.BottomMargin) * originView.Scale;

            //project the points from model coordinates to get the width and height in the view coordinates
            XYZ p1InView = Calcs.ProjectToView(p1, originView);
            XYZ p2InView = Calcs.ProjectToView(p2, originView);
            //sort the points into maximum and minimum
            XYZ minInView = Calcs.GetMinPoint(p1InView, p2InView);
            XYZ maxInView = Calcs.GetMaxPoint(p1InView, p2InView);
            Double coverageWidth = ((maxInView.X - minInView.X) < maxWidth) ? maxInView.X - minInView.X : maxWidth;
            Double coverageHeight = ((maxInView.Y - minInView.Y) < maxHeight) ? maxInView.Y - minInView.Y : maxHeight;

            View dupeView = originView.DuplicateToSketchView(tagString, origin, coverageWidth, coverageHeight);
            ElementId markerId = originView.PlaceMarker(tagString, origin, coverageWidth, coverageHeight);
            dupeView.WriteMarkerEData(markerId);

            return dupeView;
        }

        /// <summary>
        /// Equality for SketchViews is based on whether they point to the same View in the document
        /// </summary>
        /// <param name="obj">The SketchView</param>
        /// <returns>Whether equal</returns>
        public override Boolean Equals(object obj)
        {
            SketchView viewToTest = obj as SketchView;
            if (null != viewToTest)
            {
                if (this.Id.IntegerValue == viewToTest.Id.IntegerValue)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
