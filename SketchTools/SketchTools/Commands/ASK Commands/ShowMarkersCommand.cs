using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class ShowMarkersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;

            using (Transaction t = new Transaction(dbDoc, "Unhide ASK markers"))
            {
                t.Start();

                FilteredElementCollector d = new FilteredElementCollector(dbDoc);
                d.OfCategory(BuiltInCategory.OST_GenericAnnotation);
                var q = from f in d where AskMarker.IsAskMarker(f) select f;

                //the UnhideElements() method requires an ICollection<ElementId>
                ElementId[] markersToShow = new ElementId[1];
                foreach (var marker in q)
                {
                    View ownerView = (View)dbDoc.GetElement(marker.OwnerViewId);
                    if (marker.IsHidden(ownerView))
                    {
                        markersToShow[0] = marker.Id;
                        ownerView.UnhideElements(markersToShow);
                    }
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
