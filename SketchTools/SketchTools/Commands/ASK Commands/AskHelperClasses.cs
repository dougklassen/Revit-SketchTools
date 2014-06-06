using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    internal static class AskSchemas
    {
        private static Guid AskSheetSchemaGuid = new Guid("5c6018c3-96ab-46c7-8cf9-92b833b0c05f");
        private static Guid AskMarkerSchemaGuid = new Guid("b918c8e1-1ee4-4f43-b655-2e3f4324dc40");

        /// <summary>
        /// a factory method for obtaining the AskSheetSchema, which is attached to a ViewSheet and documents whether the view is an ASK.
        /// </summary>
        /// <returns>A Schema for storing information about whether a ViewSheet is an ASK</returns>
        internal static Schema GetAskSheetSchema()
        {
            //check for schema already in memory
            Schema sheetSchema = Schema.Lookup(AskSheetSchemaGuid);
            if (null == sheetSchema)
	        {
		        SchemaBuilder schemaBuilder = new SchemaBuilder(AskSheetSchemaGuid);
                schemaBuilder.SetSchemaName("AskSheetSchema");
                schemaBuilder.SetVendorId("BCRA");
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                FieldBuilder fieldBuilder;
                fieldBuilder = schemaBuilder.AddSimpleField("IsAsk", typeof(Boolean));
                fieldBuilder.SetDocumentation("Whether the sheet is an ASK");

                sheetSchema = schemaBuilder.Finish();
	        }
            return sheetSchema;
        }

        /// <summary>
        /// A factory method for obtaining the AskMarkerSchema, which is attached to an ASK Sketch View and identifies the marker which locates it
        /// </summary>
        /// <returns></returns>
        internal static Schema GetAskMarkerSchema()
        {
            //check if the schema is already in memory, and create if not
            Schema markerSchema = Schema.Lookup(AskMarkerSchemaGuid);
            if (null == markerSchema)
            {
                SchemaBuilder schemaBuilder = new SchemaBuilder(AskMarkerSchemaGuid);
                schemaBuilder.SetSchemaName("AskMarkerSchema");
                schemaBuilder.SetVendorId("BCRA");
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("MarkerId", typeof(ElementId));
                fieldBuilder.SetDocumentation("The MarkerId for the Marker of the sketch");

                markerSchema = schemaBuilder.Finish();
            }
            return markerSchema;
        }

        /// <summary>
        /// Extension method to purge all ASK related extensible data from a view
        /// </summary>
        /// <param name="view">the view to be purged</param>
        /// <returns>Whether an entity was deleted from the view</returns>
        internal static Boolean CleanAskData(this Element elementToClean)
        {
            //attempt to delete the appropriate schema
            if (elementToClean is ViewSheet)
            {
                Schema sheetSchema = AskSchemas.GetAskSheetSchema();
                return elementToClean.DeleteEntity(sheetSchema);
            }
            else if (elementToClean is FamilyInstance)
            {
                Schema sketchSchema = AskSchemas.GetAskMarkerSchema();
                return elementToClean.DeleteEntity(sketchSchema);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Extension method to Specify the OriginView for a SketchView by setting Marker e-data information. Can only be called within an active transaction.
        /// </summary>
        /// <param name="sketchView">the view with a marker</param>
        /// <param name="marker">the marker that corresponds to the dupeView</param>
        public static void WriteMarkerEData(this View sketchView, ElementId markerId)
        {
            Schema markerSchema = AskSchemas.GetAskMarkerSchema();
            Entity markerDataEntity = new Entity(markerSchema);
            markerDataEntity.Set<ElementId>("MarkerId", markerId);
            sketchView.SetEntity(markerDataEntity);
        }
    }

    /// <summary>
    /// A class for other methods and extension methods
    /// </summary>
    internal static class AskHelperMethods
    {
        /// <summary>
        /// Extension method on Document to retrieve all ASKs in the project as ASK objects
        /// </summary>
        /// <param name="dbDoc">The document</param>
        /// <returns>All ASKs in the doument, indexed by number</returns>
        internal static List<ASK> GetAsks(this Document dbDoc)
        {
            FilteredElementCollector c = new FilteredElementCollector(dbDoc);
            c.OfClass(typeof(ViewSheet));

            var q = from ViewSheet v in c where v.IsAskSheetView() select ASK.GetAsk(dbDoc, v.Id);

            return q.ToList();
        }

        /// <summary>
        /// Extension method to delete all ASK MARKER markers from a view. Can only be called within an open transaction
        /// </summary>
        /// <param name="view">the view to be purged of ASK MARKERs</param>
        internal static void CleanAskMarkers(this View view)
        {
            Document dbDoc = view.Document;
            FilteredElementCollector c = new FilteredElementCollector(dbDoc, view.Id);
            c.OfCategory(BuiltInCategory.OST_GenericAnnotation);
            var askMarkerFamilyInstances = from FamilyInstance f in c where f.Symbol.Name == "ASK MARKER" select f;

            foreach (var m in askMarkerFamilyInstances)
            {
                dbDoc.Delete(m);
            }
        }

        /// <summary>
        /// Extension method to check if a View is used for an ASK.
        /// This is entirely determined by whether it is place on a ViewSheet that is an ASK sheet
        /// </summary>
        /// <param name="view">A view in the model</param>
        /// <returns>Whether the view is used on an ASK</returns>
        internal static Boolean IsPlacedSketchView(this View view)
        {
            Document dbDoc = view.Document;
            List<ASK> asks = dbDoc.GetAsks();
            //check all the views placed on all AskSheetViews to see if the view in question matches one of them
            foreach (ASK a in asks)
            {
                ViewSheet askSheet = (ViewSheet)dbDoc.GetElement(a.ViewSheetId);
                foreach (View v in askSheet.Views)
                {
                    //drill all the way to IntegerValue of ElementId, based on lack of information about how Equals() is implemented
                    if (view.Id.IntegerValue == v.Id.IntegerValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Extension method to check if a ViewSheet is registered as an ASK
        /// </summary>
        /// <param name="viewSheet">A ViewSheet in the model</param>
        /// <returns>Whether the ViewSheet is an ASK</returns>
        internal static Boolean IsAskSheetView(this ViewSheet viewSheet)
        {
            Schema askSheetSchema = AskSchemas.GetAskSheetSchema();

            //retrieves the entity stored on the ViewSheet.
            //If an entity can't be retrieved, return that the ViewSheet is not an ASK
            Entity sheetEntity = viewSheet.GetEntity(askSheetSchema);
            if (!sheetEntity.IsValid())
            {
                return false;
            }

            return sheetEntity.Get<Boolean>("IsAsk");
        }

        /// <summary>
        /// Derives the bounds of the View in terms of orgin (center), coverageWidth, and coverageHeight
        /// </summary>
        /// <param name="originView">The view to be analyzed</param>
        /// <param name="origin">The centerpoint of the View CropBox in world coordinates</param>
        /// <param name="coverageWidth">The width of the area covered by the View, in decimal feet</param>
        /// <param name="coverageHeight">The height of the area covered by the View, in decimal feet</param>
        private static void GetViewBounds(this View originView, out XYZ origin, out Double coverageWidth, out Double coverageHeight)
        {
            if (ViewType.DraftingView == originView.ViewType)
            {
                BoundingBoxUV viewOutline = originView.Outline;
                origin = new XYZ(
                    originView.Scale * (viewOutline.Min.U + viewOutline.Max.U) / 2,
                    originView.Scale * (viewOutline.Min.V + viewOutline.Max.V) / 2,
                    0);
                coverageWidth = (viewOutline.Max.U - viewOutline.Min.U) * originView.Scale;
                coverageHeight = (viewOutline.Max.V - viewOutline.Min.V) * originView.Scale;
            }
            else
            {
                BoundingBoxXYZ originViewCropBox = originView.CropBox;
                origin = new XYZ(
                    (originViewCropBox.Max.X + originViewCropBox.Min.X) / 2.0,
                    (originViewCropBox.Max.Y + originViewCropBox.Min.Y) / 2.0,
                    (originViewCropBox.Max.Z + originViewCropBox.Min.Z) / 2.0);

                XYZ minInView = Calcs.ProjectToView(originViewCropBox.Min, originView);
                XYZ maxInView = Calcs.ProjectToView(originViewCropBox.Max, originView);
                //minInView may not be < maxInView after projection
                coverageWidth = Math.Abs(maxInView.X - minInView.X);
                coverageHeight = Math.Abs(maxInView.Y - minInView.Y);
            }

        }

        /// <summary>
        /// Extension method to create a ASK MARKER generic annotation object in the originView.
        /// If no bounds are given, the marker covers the whole view
        /// </summary>
        /// <param name="originView">The view within which the marker will be placed</param>
        /// <param name="origin">the centerpoint of the area covered by the DupeView</param>
        /// <param name="coverageWidth">the width of the area covered by the DupeView in decimal feet</param>
        /// <param name="coverageHeight">the height of the area covered by the DupeView in decimal feet</param>
        /// <returns>The Id of the marker that was created</returns>
        public static ElementId PlaceMarker(
            this View originView,
            String markerText,
            XYZ origin = null,
            Double coverageWidth = 0,
            Double coverageHeight = 0)
        {
            if (null == origin)
            {
                originView.GetViewBounds(out origin, out coverageWidth, out coverageHeight);
            }

            Document dbDoc = originView.Document;
            FamilySymbol askMarkerFamilySymbol = AskMarker.GetAskMarkerFamilySymbol(dbDoc);
            FamilyInstance askMarkerAnnotation = dbDoc.Create.NewFamilyInstance(origin, askMarkerFamilySymbol, originView);

            //rotate about the Z axis to match view rotation if the ViewType is one which can be rotated
            if (ViewType.Elevation != originView.ViewType &&
                ViewType.Section != originView.ViewType &&
                ViewType.DraftingView != originView.ViewType)
            {
                Autodesk.Revit.ApplicationServices.Application revit = dbDoc.Application;
                Transform viewTransform = originView.CropBox.Transform;
                Line axis = revit.Create.NewLine(origin, viewTransform.BasisZ, false);
                Double angle = Math.Atan(originView.RightDirection.Y / originView.RightDirection.X);
                askMarkerAnnotation.Location.Rotate(axis, angle);
            }

            //Configure parameters. units are in decimal inches
            Parameter p;
            p = askMarkerAnnotation.ParametersMap.get_Item("WIDTH");
            p.Set(coverageWidth / originView.Scale);

            p = askMarkerAnnotation.ParametersMap.get_Item("HEIGHT");
            p.Set(coverageHeight / originView.Scale);

            p = askMarkerAnnotation.ParametersMap.get_Item("ASK NUMBER");
            p.Set(markerText);

            return askMarkerAnnotation.Id;
        }

        /// <summary>
        /// Extension method creates a new SketchView by duplicating the specified region of the OriginView.
        /// Also places a marker and sets MarkerId Schema of SketchView. Can only be called within an open transaction.
        /// </summary>
        /// <param name="originView">The view to be duplicated</param>
        /// <param name="tagString">identifying information for the SketchView, generally the ASK it will be placed on</param>
        /// <param name="origin">The center point of the subregion covered by the view
        /// If this in null, or not set, the originView's cropBox will be used without cropping</param>
        /// <param name="coverageWidth">the width of the subregion covered by the SketchView in decimal feet</param>
        /// <param name="coverageHeight">the height of the subregion covered by the SketchView in decimal feet</param>
        /// <returns>the duplicated view that was created</returns>
        internal static View DuplicateToSketchView(
            this View originView,
            String tagString,
            XYZ origin = null,
            Double coverageWidth = 0,
            Double coverageHeight = 0)
        {
            Document dbDoc = originView.Document;
            ElementId dupeViewId = originView.Duplicate(ViewDuplicateOption.WithDetailing);
            View dupeView = (View)dbDoc.GetElement(dupeViewId);
            //todo: hide callout box in originView for views duplicated from a callout View

            //set the ViewTemplate to null so the view can be configured
            dupeView.ViewTemplateId = new ElementId(-1);

            //todo: delete all revision clouds in DupeView

            //hide the shared base point and project base point. Checks get_AllowsVisibilityControl because Categories such as ProjectBasePoint
            //don't exist in all ViewTypes
            Category category;
            category = dbDoc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectBasePoint);
            if (category.get_AllowsVisibilityControl(dupeView))
            {
                dupeView.setVisibility(category, false);
            }
            category = dbDoc.Settings.Categories.get_Item(BuiltInCategory.OST_SharedBasePoint);
            if (category.get_AllowsVisibilityControl(dupeView))
            {
                dupeView.setVisibility(category, false);
            }
            //Hide section cuts
            category = dbDoc.Settings.Categories.get_Item(BuiltInCategory.OST_Sections);
            if (category.get_AllowsVisibilityControl(dupeView))
            {
                dupeView.setVisibility(category, false);
            }

            //the Name property is the view name that shows up in the view title, unless another is set
            dupeView.Name = dbDoc.GetUniqueViewName(tagString + " - " + originView.Name);

            //If no value for origin (the center of the sub-region) is provided, the view will take its CropBox from the OwnerView
            if (null == origin)
            {
                dupeView.CropBox = originView.CropBox;
                //The cropbox for DraftingViews exists but cannot be manipulated
                if (ViewType.DraftingView != originView.ViewType)
                {
                    dupeView.CropBoxActive = true;
                    dupeView.CropBoxVisible = true;
                    dupeView.ParametersMap.get_Item("Annotation Crop").Set(1);
                }
            }
            //if origin is set, a subregion is specified and the view will be cropped to the subregion
            else
            {
                //crop view. Take Z coords from askOwnerView to handle sections
                //the z coordinate will be reversed because of a bug in assigned cropBoxes
                dupeView.CropBoxActive = false; //todo: necessary?
                //use the boundingBox of the orginView so that the views have the same transform
                BoundingBoxXYZ boundingBox = originView.CropBox;

                XYZ originInView = Calcs.ProjectToView(origin, originView);
                boundingBox.Min = new XYZ(
                    originInView.X - coverageWidth / 2,
                    originInView.Y - coverageHeight / 2,
                    originView.CropBox.Min.Z);
                boundingBox.Max = new XYZ(
                    originInView.X + coverageWidth / 2,
                    originInView.Y + coverageHeight / 2,
                    originView.CropBox.Max.Z);

                //When the CropBox is assigned, the Z coordinates of min and max are reflected along the z axis and sorted by value
                dupeView.CropBox = boundingBox;
                dupeView.CropBoxActive = true;
                dupeView.CropBoxVisible = true;
            }

            if (dupeView.ParametersMap.Contains("Annotation Crop"))
            {
                dupeView.ParametersMap.get_Item("Annotation Crop").Set(1);
                //todo: reduce the size of the annotaion crop
            }

            return dupeView;
        }
    }

    /// <summary>
    /// An IExternalCommandAvailability implementation that will allow a command to be run when an ASKMarker is selected
    /// </summary>
    public class MarkerSelectedCommandAvailability : IExternalCommandAvailability
    {
        public Boolean IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
        {
            //return false if no document is open--an exception will be thrown otherwise
            if (null == uiApp.ActiveUIDocument)
            {
                return false;
            }
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;

            //command is available when one ASK MARKER is selected
            var selectedElementIds = uiDoc.Selection.GetElementIds();
            if (1 == selectedElementIds.Count())
            {
                //todo: replace with call to AskMarker.IsAskMarker(selectedElement)
                FamilyInstance selectedElement = dbDoc.GetElement(selectedElementIds.First()) as FamilyInstance;
                if (null != selectedElement)
                {
                    if ("ASK MARKER" == selectedElement.Symbol.Name)
                    {
                        return true;
                    }
                }
            }

            //default
            return false;
        }
    }

    /// <summary>
    /// This purges all ASK data from added views. ASK data will only exist in new views if the view was created by duplication
    /// </summary>
    public class ViewAskPurgerUpdater : IUpdater
    {
        static AddInId addInId;
        static UpdaterId updaterId;

        public ViewAskPurgerUpdater(AddInId id)
        {
            addInId = id;
            updaterId = new UpdaterId(addInId, new Guid("c5cda5a2-370f-458a-9e92-dd63222cf772"));
        }

        public void Execute(UpdaterData data)
        {
            Document dbDoc = data.GetDocument();
            //select all added elements which are views but not view templates
            var addedViews
                = data.GetAddedElementIds().Select<ElementId, View>(id => dbDoc.GetElement(id) as View).Where(view => !view.IsTemplate);

            foreach (View v in addedViews)
            {
                //SheetSchema entity will only exist on ViewSheets if an ASK sheet is duplicated.
                //In this case the entity will be removed so the duplicate ViewSheet isn't recorded as an ASK
                v.CleanAskData();
                v.CleanAskMarkers();
            }
        }

        public string GetAdditionalInformation()
        {
            return "BCRA";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Views;
        }

        public UpdaterId GetUpdaterId()
        {
            return updaterId;
        }

        public string GetUpdaterName()
        {
            return "ViewAskPurgerUpdater";
        }
    }
}
