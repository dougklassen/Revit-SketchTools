using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CreateBlankAskCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document dbDoc = uiDoc.Document;

            CreateASKWindow window = new CreateASKWindow(uiApp);
            if (false == window.ShowDialog())
            {
                return Result.Cancelled;
            }

            ASK newAsk;
            using(Transaction t = new Transaction(dbDoc, "Create a blank SK sheet"))
            {
                t.Start();
                newAsk = ASK.CreateAskSheet(dbDoc, window.NewAskNumber, window.NewAskSheetConfigurationKey);
                t.Commit();
            }

            //change the active view to the newly created sheet
            uiDoc.ActiveView = (View)dbDoc.GetElement(newAsk.ViewSheetId);
            return Result.Succeeded;
        }
    }
}
