using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    /// <summary>
    /// IExternalCommandAvailability for CreateASKCommand. Command available when a viewport is selected on a sheet view
    /// or when the current view is of a type that will allow a subregion to be selected. Disallows the command when the
    /// active view is an AskOwnerView or AskDupeView.
    /// </summary>
    public class CreateSketchViewCommandAvailability : IExternalCommandAvailability
    {
        Boolean IExternalCommandAvailability.IsCommandAvailable(UIApplication uiApp, CategorySet selectedCategories)
        {
            //return false if no document is open--an exception will be thrown otherwise
            if (null == uiApp.ActiveUIDocument)
            {
                return false;
            }
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;
            View activeView = uiDoc.ActiveView;

            //set availability to false if the ActiveView is an ASK SheetView
            if (activeView is ViewSheet)
            {
                if (((ViewSheet)activeView).IsAskSheetView())
                {
                    return false;
                }
            }

            //check to be sure the current view is not an ASK SketchView
            if (activeView.IsPlacedSketchView())
            {
                return true;
            }

            //if the ActiveView is a type that can allow for the selection of a subregion, make the command available
            ViewType activeViewType = uiDoc.ActiveView.ViewType;
            if (
                activeViewType == ViewType.CeilingPlan ||
                activeViewType == ViewType.Detail ||
                activeViewType == ViewType.Elevation ||
                activeViewType == ViewType.FloorPlan ||
                activeViewType == ViewType.Section)
            {
                return true;
            }
            //if the ActiveView is a ViewSheet and a single viewport is selected, make the command available
            else if (activeViewType == ViewType.DrawingSheet)
            {
                ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();
                if (1 == selectedElementIds.Count)
                {
                    Element selectedElement = dbDoc.GetElement(selectedElementIds.First<ElementId>());
                    if ("Viewports" == selectedElement.Category.Name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CreateSketchViewCommand : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;
            var selection = commandData.Application.ActiveUIDocument.Selection.GetElementIds();            
            var askDictionary = dbDoc.GetAsks();

            //todo: combine all of this into one transaction
            //show a dialog to prompt the user whether to place the view on a new or existing ASK
            SelectAskWindow selectWindow = new SelectAskWindow(uiApp);
            if (false == selectWindow.ShowDialog())
            {
                return Result.Cancelled;
            }

            ASK destinationAsk;

            //Based on user selection, specify which sheet to place the SketchView onto
            if (null == selectWindow.SelectedAskNumber) //this property returns null when "New ASK" has been selected
            {
                //show a dialog to create a new ASK Sheet
                CreateASKWindow createWindow = new CreateASKWindow(uiApp);
                if (false == createWindow.ShowDialog())
                {
                    return Result.Cancelled;
                }
                
                using (Transaction t = new Transaction(dbDoc, "Create SK sheet"))
                {
                    t.Start();
                    destinationAsk = ASK.CreateAskSheet(dbDoc, createWindow.NewAskNumber, createWindow.NewAskSheetConfigurationKey);
                    t.Commit();
                }
            }
            else
            {
                using (Transaction t = new Transaction(dbDoc, "Create SK sheet"))
                {
                    t.Start();
                    destinationAsk = ASK.GetAsk(dbDoc, selectWindow.SelectedAskNumber);
                    t.Commit();
                }
            }
            
            //Create the SketchView and place it on a sheet
            //If an appropriate View type is open, create a SketchView by picking a region
            if (
                uiDoc.ActiveView.ViewType == ViewType.FloorPlan ||
                uiDoc.ActiveView.ViewType == ViewType.Section ||
                uiDoc.ActiveView.ViewType == ViewType.Elevation)
            {   
                PickedBox pickedBox = uiDoc.Selection.PickBox(PickBoxStyle.Enclosing, "Pick the region to show");

                using (Transaction t = new Transaction(dbDoc, "Place the sketch on the SK sheet"))
                {
                    t.Start();
                    View dupeView = SketchView.CreateDupeSketchView(
                        uiDoc.ActiveView,
                        destinationAsk.Number,
                        Standard.askSheetConfigurations[destinationAsk.SheetConfigurationKey].SheetSize,
                        pickedBox.Min,
                        pickedBox.Max);
                    destinationAsk.PlaceSketchView(dupeView);
                    t.Commit();
                }

                uiDoc.ActiveView = (View)dbDoc.GetElement(destinationAsk.ViewSheetId);
            }            
            //if only one element is selected, see if it's a ViewPort. If so, duplicate that View onto a SketchView
            else if (selection.Count() == 1)
            {
                //get first (and only) member of selection, and find its category
                Element selElement = dbDoc.GetElement(selection.First());
                if (selElement.Category != null) //some elements do not have a Category, such as the crop box of a view
                {
                    if (selElement.Category.Name == "Viewports")
                    //create a DupeView based on the CropBox of the OwnerView
                    {
                        //If the user has a drafting view selected, create an ASK directly from that view
                        View originView = (View)dbDoc.GetElement(((Viewport)selElement).ViewId);
                        if (
                            ViewType.DraftingView == originView.ViewType ||
                            ViewType.Section == originView.ViewType ||
                            ViewType.Elevation == originView.ViewType ||
                            ViewType.FloorPlan == originView.ViewType)
                        {
                            using (Transaction t = new Transaction(dbDoc, "Create ASK"  + destinationAsk.Number))
                            {
                                t.Start();
                                View dupeView = SketchView.CreateDupeSketchView(
                                    originView,
                                    destinationAsk.Number,
                                    Standard.askSheetConfigurations[destinationAsk.SheetConfigurationKey].SheetSize);
                                destinationAsk.PlaceSketchView(dupeView);
                                t.Commit();
                            }
                            uiDoc.ActiveView = (View)dbDoc.GetElement(destinationAsk.ViewSheetId);
                        }
                    } 
                }
            }
            
            return Result.Succeeded;
        }
    }
}
