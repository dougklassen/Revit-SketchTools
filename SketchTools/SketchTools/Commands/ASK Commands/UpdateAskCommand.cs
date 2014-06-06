using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    /// <summary>
    /// Regenerates the ASK view and ASK sheet based on the selected ASK marker. Will only run when an ASK MARKER annotation object is selected,
    /// based on MarkerSelectedCommandAvailability IExternalCommandAvailability class.
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class UpdateASKCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;

            using (Transaction t = new Transaction(dbDoc, "Update ASK"))
            {
                t.Start();
                FamilyInstance markerAnnotation = (FamilyInstance)dbDoc.GetElement(uiDoc.Selection.GetElementIds().First());
                AskMarker askToUpdateMarker = AskMarker.GetAskMarker(markerAnnotation);
                askToUpdateMarker.UpdateDupeView();
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
