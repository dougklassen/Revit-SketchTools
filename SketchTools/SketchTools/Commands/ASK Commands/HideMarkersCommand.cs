using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HideMarkersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;

            FilteredElementCollector c = new FilteredElementCollector(dbDoc);
            c.OfCategory(BuiltInCategory.OST_Views);

            //todo: need to exclude view templates

            using (Transaction t = new Transaction(dbDoc, "Hide ASK markers"))
            {
                t.Start();

                foreach (View v in c)
                {
                    if (!v.IsTemplate)
                    {
                        FilteredElementCollector d = new FilteredElementCollector(dbDoc, v.Id);
                        d.OfCategory(BuiltInCategory.OST_GenericAnnotation);
                        var q = from FamilyInstance f in d where AskMarker.IsAskMarker(f) select f;

                        List<ElementId> markersToHide = new List<ElementId>();
                        foreach (FamilyInstance marker in q)
                        {
                            if (marker.CanBeHidden(v))
                            {
                                markersToHide.Add(marker.Id);
                            }
                        }

                        if (markersToHide.Count() > 0)
                        {
                            v.HideElements(markersToHide);
                        }
                    }
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
