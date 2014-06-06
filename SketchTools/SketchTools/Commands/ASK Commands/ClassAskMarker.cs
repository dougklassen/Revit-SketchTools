using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    public class AskMarker
    {
        //the document in which the marker lives
        private Document dbDoc;

        /// <summary>
        /// The FamilyInstance of the ASK MARKER in the document
        /// </summary>
        private FamilyInstance AsFamilyInstance
        {
            get
            {
                return (FamilyInstance)dbDoc.GetElement(this.Id);
            }
        }

        /// <summary>
        /// The View within which the ASK MARKER is placed, and from which the DupeView is derived
        /// </summary>
        private View OriginView
        {
            get
            {
                ElementId oVId = this.AsFamilyInstance.OwnerViewId;
                return (View)dbDoc.GetElement(oVId);
            }
        }

        /// <summary>
        /// The Id of the AskMarker annotation
        /// </summary>
        public ElementId Id { get; set; }

        /// <summary>
        /// The center point of the CoverageArea in model space
        /// </summary>
        public XYZ Origin
        {
            get
            {
                LocationPoint insertionLocation = (LocationPoint)this.AsFamilyInstance.Location;
                return insertionLocation.Point;
            }
        }

        /// <summary>
        /// The width of the CoverageArea in model space in decimal feet
        /// </summary>
        public Double CoverageWidth
        {
            get
            {
                Parameter p = this.AsFamilyInstance.ParametersMap.get_Item("WIDTH");
                return p.AsDouble() * OriginView.Scale;
            }
        }

        /// <summary>
        /// The height of the CoverageArea in model space in decimal feet
        /// </summary>
        public Double CoverageHeight
        {
            get
            {
                Parameter p = this.AsFamilyInstance.ParametersMap.get_Item("HEIGHT");
                return p.AsDouble() * OriginView.Scale;
            }
        }

        /// <summary>
        /// the sketchView marked by this marker
        /// </summary>
        public SketchView DupeView
        {
            get
            {
                FilteredElementCollector c = new FilteredElementCollector(dbDoc);
                c.OfCategory(BuiltInCategory.OST_Views);
                var q = from View v in c where this.Id == SketchView.GetSketchView(v).MarkerId select v;

                if (0 == q.Count())
                {
                    throw new InvalidOperationException("ASK MARKER Id " + this.Id.IntegerValue + " is invalid. No DupeView found");
                }
                return SketchView.GetSketchView(q.First());
            }
        }

        /// <summary>
        /// private constructor, create AskMarker objects using the factory method
        /// </summary>
        private AskMarker() { }

        /// <summary>
        /// Factory method to return an AskMarker from a generic annotation
        /// </summary>
        /// <param name="markerAnnotation">The marker annotation to be represented as an AskMarker</param>
        /// <returns>the AskMarker</returns>
        public static AskMarker GetAskMarker(FamilyInstance markerAnnotation)
        {
            if (!IsAskMarker(markerAnnotation))
            {
                throw new InvalidOperationException("Element " + markerAnnotation.Id.IntegerValue + " is not an ASK MARKER");
            }
            AskMarker newMarker = new AskMarker();
            newMarker.dbDoc = markerAnnotation.Document;
            newMarker.Id = markerAnnotation.Id;
            return newMarker;
        }

        /// <summary>
        /// Updates the SketchView based on the current state of the originView. First, stores the placement of the SketchView on the ASK sheet.
        /// Then deletes the SketchView, generates a new one, and places it back on the sheet.
        /// </summary>
        /// <returns>The DupeView that was created</returns>
        public void UpdateDupeView()
        {
            SketchView viewToUpdate = SketchView.GetSketchView((View)dbDoc.GetElement(DupeView.Id));
            String dupeViewName = viewToUpdate.Name;
            XYZ insertionPoint = viewToUpdate.ViewportInsertionPoint;
            ASK ask = viewToUpdate.Ask;
            dbDoc.Delete(this.DupeView.Id);
            //todo: scale is wrong
            View newView = this.OriginView.DuplicateToSketchView(dupeViewName, this.Origin, this.CoverageWidth, this.CoverageHeight);
            //todo: should this be part of View.DuplicateToSketchView?
            newView.WriteMarkerEData(this.Id);
            //todo: provide for setting location. UV of the centerpoint should suffice
            ask.PlaceSketchView(newView, insertionPoint);
        }

        /// <summary>
        /// Determines if an annotation is a Marker by searching for a view that has that element designated as its marker
        /// </summary>
        /// <param name="genericAnnotation">the FamilyInstance Element to check</param>
        /// <returns>Whether the FamilyInstance is an ASK MARKER</returns>
        public static Boolean IsAskMarker(Element genericAnnotation)
        {
            //only AnnotationSymbol objects qualify
            if (!(genericAnnotation is AnnotationSymbol))
            {
                return false;
            }
            Document dbDoc = genericAnnotation.Document;
            FilteredElementCollector c = new FilteredElementCollector(dbDoc);
            c.OfCategory(BuiltInCategory.OST_Views);
            //todo: exclude ViewSheets to speed up search

            var q = from View v
                    in c
                    where ((null != SketchView.GetSketchView(v).MarkerId) && (genericAnnotation.Id.IntegerValue == SketchView.GetSketchView(v).MarkerId.IntegerValue))
                    select v; //todo: this throws an exception when MarkerId is null for the sketchview

            return (q.Count() > 0);
        }

        /// <summary>
        /// Helper method to retrieve the FamilySymbol for ASK MARKER. Loads if not already found in dbDoc.
        /// Can only be called from within an open transaction
        /// </summary>
        /// <returns>The loaded FamilySymbol for ASK MARKER</returns>
        internal static FamilySymbol GetAskMarkerFamilySymbol(Document dbDoc)
        {
            //look for the family
            Family ASKBlockFamily = null;
            FilteredElementCollector collector = new FilteredElementCollector(dbDoc);
            List<Element> familiesInDocument = (List<Element>)collector.OfClass(typeof(Family)).ToElements();
            foreach (Element e in familiesInDocument)
            {
                if (e.Name == "ASK MARKER") ASKBlockFamily = (Family)e;
            }

            //if not found, load ASK MARKER.rfa
            if (null == ASKBlockFamily)
            {
                Boolean success = dbDoc.LoadFamily(FileLocations.addInResourcesDirectory + FileLocations.askMarkerFile, out ASKBlockFamily);
                if (!success) throw new Exception("Could not find " + FileLocations.addInResourcesDirectory + FileLocations.askMarkerFile);
            }

            FamilySymbol ASKBlockFamilySymbol = null;
            foreach (FamilySymbol s in ASKBlockFamily.Symbols)
            {
                if (s.Name == "ASK MARKER") ASKBlockFamilySymbol = s;
            }
            return ASKBlockFamilySymbol;
        }
    }
}