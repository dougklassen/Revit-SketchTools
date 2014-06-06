using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ClearFlaggedCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ClearFlaggedLogic logic = new ClearFlaggedLogic(commandData);

            TaskDialog clearFlaggedDialog = new TaskDialog("Clear All Flagged");
            clearFlaggedDialog.MainInstruction = "Clear all flagged annotations?";
            clearFlaggedDialog.CommonButtons = TaskDialogCommonButtons.No|TaskDialogCommonButtons.Yes;

            TaskDialogResult command = clearFlaggedDialog.Show();
            if (TaskDialogResult.Yes == command)
            {
                Result result = logic.ClearAllFlagged();
                return result;
            }
            else
            {
                return Result.Cancelled;
            }
        }
    }

    /// <summary>
    /// Logic for unflagging either a single text note or all flag text notes
    /// </summary>
    internal class ClearFlaggedLogic
    {
        //fields
        private ExternalCommandData commandData;
        private UIApplication uiApp;
        private UIDocument uiDoc;
        private Document dbDoc;

        private static String flagString = "!FLAG!";

        //constructors
        internal ClearFlaggedLogic(ExternalCommandData commandData)
        {
            this.commandData = commandData;
            uiApp = commandData.Application;
            uiDoc = uiApp.ActiveUIDocument;
            dbDoc = uiDoc.Document;
        }

        //methods
        internal Result ClearAllFlagged()
        {
            FilteredElementCollector collector = new FilteredElementCollector(dbDoc);
            collector.OfCategory(BuiltInCategory.OST_TextNotes);

            var query = from TextNote textNote in collector
                        where textNote.TextNoteType.Name.EndsWith(flagString)
                        select textNote;

            using (Transaction clearAllFlaggedTransaction = new Transaction(dbDoc, "Clear All Flagged Annotations"))
            {
                TransactionStatus status = clearAllFlaggedTransaction.Start();
                if (status != TransactionStatus.Started)
                {
                    return Result.Failed;
                }

                foreach (TextNote textNote in query)
                {
                    try
                    {
                        UnFlagNote(textNote.Id);
                    }
                    catch (Exception e)
                    {
                        TaskDialog.Show("Error", e.Message);
                        return Result.Failed;
                    }
                }

                clearAllFlaggedTransaction.Commit();
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Unflags the selected note if it is flagged. Can only be called when a transaction is open
        /// </summary>
        /// <param name="textNoteId"></param>
        private void UnFlagNote(ElementId textNoteId)
        {
            TextNote FlaggedTextNote = dbDoc.GetElement(textNoteId) as TextNote;
            //check to make sure the element is a TextNote
            if (null == FlaggedTextNote)
            {
                throw new InvalidOperationException("Error processing flagged annotation: element was not a text note");
            }
            //check to make sure the text note is flagged
            if (!FlaggedTextNote.TextNoteType.Name.EndsWith("!FLAG!"))
            {
                TaskDialog.Show("Message", "Annotation is not flagged");
                return;
            }

            FilteredElementCollector collector = new FilteredElementCollector(dbDoc);
            collector.OfClass(typeof(TextNoteType));
            String FlaggedTextNoteTypeName = FlaggedTextNote.TextNoteType.Name;
            //find the corresponding unflagged Text Note Type
            var query = from TextNoteType textNoteType in collector
                        where FlaggedTextNoteTypeName.Remove(FlaggedTextNoteTypeName.Length - flagString.Length) == textNoteType.Name
                        select textNoteType;
            if (query.Count<TextNoteType>() < 1)
            {
                TaskDialog.Show("Message", "No corresponding text note family, text note will remain flagged");
                return;
            }
            else
            {
                FlaggedTextNote.TextNoteType = query.First<TextNoteType>();
                return;
            }
        }
    }
}
