using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AnnoReviewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AnnoReviewLogic logic = new AnnoReviewLogic(commandData.Application);

            //this dialog will report its result to the AnnoReviewLogic object
            QueryDialog dialog = new QueryDialog(logic);

            dialog.ShowDialog();

            if (!logic.CommandCancelled)
            {
                if (AnnoReviewTask.Report == logic.Task)
                {
                    AnnoReviewWindow window = new AnnoReviewWindow(logic);

                    window.ShowDialog();
                    return Result.Succeeded;
                }
                else if (AnnoReviewTask.Flag == logic.Task)
                {
                    IEnumerable<ElementId> queryResults = logic.GetIdAnnotationsContainingQuery(logic.SearchString);
                    logic.FlagSelectedTextNotes(queryResults);

                    return Result.Succeeded;
                }
                else
                {
                    return Result.Failed;
                }
            }
            else
            {
                return Result.Cancelled;
            }
        }
    }

    internal class AnnoReviewLogic
    {
        //fields
        private UIApplication uiApp;
        private UIDocument uiDoc;
        private Document dbDoc;

        private static String flagString = "!FLAG!";

        /// <summary>
        /// all text note types that have an associated flagged style
        /// </summary>
        private List<TextNoteType> flaggedTextNoteTypes = new List<TextNoteType>();

        //properties
        internal Boolean CommandCancelled = false;
        internal String SearchString = String.Empty;
        internal AnnoReviewTask Task;

        //constructors
        internal AnnoReviewLogic(UIApplication uiApp)
        {
            this.uiApp = uiApp;
            uiDoc = uiApp.ActiveUIDocument;
            dbDoc = uiDoc.Document;

            //Find all the flag note styles that already exist (to avoid recreating existing flag styles)
            FilteredElementCollector collector = new FilteredElementCollector(dbDoc);
            collector.OfClass(typeof(TextNoteType));
            var query = from TextNoteType noteType in collector
                        where noteType.Name.EndsWith(flagString)
                        select noteType;
            foreach (TextNoteType noteTypeToAdd in query)
            {
                flaggedTextNoteTypes.Add(noteTypeToAdd);
            }
        }

        //methods

        /// <summary>
        /// Event handler for text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void QueryExecuted(object sender, Autodesk.Revit.UI.Events.TextBoxEnterPressedEventArgs args)
        {
            TextBox queryTextBox = sender as TextBox;
            String textBoxString = queryTextBox.Value as String;
            TaskDialog.Show("message", String.Format("You searched for {0}", textBoxString));

            AnnoReviewLogic logic = new AnnoReviewLogic(args.Application);
            logic.SearchString = textBoxString;
            AnnoReviewWindow window = new AnnoReviewWindow(logic);

            window.ShowDialog();
        }

        /// <summary>
        /// Generate a set of descriptions for a set of text notes
        /// </summary>
        /// <param name="textNoteIds"></param>
        /// <returns></returns>
        internal IEnumerable<ResultDescription> GetSelectedTextNotesDescription(IEnumerable<ElementId> textNoteIds)
        {
            List<ResultDescription> queryResultDescription = new List<ResultDescription>();

            foreach (ElementId noteId in textNoteIds)
            {
                TextNote note = dbDoc.GetElement(noteId) as TextNote;
                String viewName = dbDoc.GetElement(note.OwnerViewId).Name;
                ResultDescription result = new ResultDescription()
                {
                    ViewName = viewName,
                    AnnotationText = note.Text,
                    AnnotationElementID = note.Id.ToString()
                };
                queryResultDescription.Add(result);
            }

            return queryResultDescription;
        }

        /// <summary>
        /// change the appearance of a set of selected text notes
        /// </summary>
        /// <param name="textNoteIds"></param>
        /// <returns></returns>
        internal Result FlagSelectedTextNotes(IEnumerable<ElementId> textNoteIds)
        {
            using (Transaction flagTransaction = new Transaction(dbDoc))
            {
                flagTransaction.SetName("Flag annotations containing search string");
                TransactionStatus status = flagTransaction.Start();
                if (TransactionStatus.Started != status)
                {
                    TaskDialog.Show("Error", "Could not modify document");
                    return Result.Failed;
                }

                foreach (ElementId noteId in textNoteIds)
                {
                    TextNote note = dbDoc.GetElement(noteId) as TextNote;
                    TextNoteType noteType = note.TextNoteType;

                    //check to see if note is already flagged, and skip if it is
                    if (noteType.Name.EndsWith(flagString))
                    {
                        continue;
                    }

                    //search for an existing flagged version of the TextNoteType
                    var query = from flaggedNotetype in flaggedTextNoteTypes where flaggedNotetype.Name == noteType.Name + flagString select flaggedNotetype;

                    //if it exists, use it
                    if (query.Count<TextNoteType>() > 0)
                    {
                        note.TextNoteType = query.First();
                    }
                    //if it doesn't, create it with a call to CreateFlagNoteType()
                    else
                    {
                        //only operate on notes that aren't groups
                        if (null == note.Group)
                        {
                            note.TextNoteType = CreateFlagNoteType(note.TextNoteType);
                        }
                        else
                        {
                            //TODO: ungroup, change note type, regroup
                            Autodesk.Revit.DB.Group noteGroup = note.Group;
                        }
                    }
                }

                flagTransaction.Commit();
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// creates a flag version of a TextNoteType
        /// </summary>
        /// <param name="textNoteType"></param>
        /// <returns></returns>
        private TextNoteType CreateFlagNoteType(TextNoteType textNoteType)
        {
            TextNoteType flagNoteType = textNoteType.Duplicate(textNoteType.Name + flagString) as TextNoteType;

            ParameterMap flagNoteTypeParameters = flagNoteType.ParametersMap;
            Parameter colorParameter = flagNoteTypeParameters.get_Item("Color");
            colorParameter.Set(255);
            Parameter borderParameter = flagNoteTypeParameters.get_Item("Show Border");
            borderParameter.Set(1);
            Parameter lineWeightParameter = flagNoteTypeParameters.get_Item("Line Weight");
            lineWeightParameter.Set(8);

            flaggedTextNoteTypes.Add(flagNoteType);

            return flagNoteType;
        }


        /// <summary>
        /// Retrieve the ids of all text notes containing the search string
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns>a collection of element ids generated by the query</returns>
        internal IEnumerable<ElementId> GetIdAnnotationsContainingQuery(String searchString)
        {
            searchString = searchString.ToUpper();
            List<ElementId> queryResults = new List<ElementId>();

            FilteredElementCollector collector = new FilteredElementCollector(dbDoc);
            collector.OfClass(typeof(TextNote));

            foreach (TextNote note in collector)
            {
                if (note.Text.ToUpper().Contains(searchString))
                {
                    queryResults.Add(note.Id);
                }
            }

            return queryResults;
        }
    }

    public class ResultDescription
    {
        public String ViewName { get; set;}
        public String AnnotationText { get; set; }
        public String AnnotationElementID { get; set; }
    }

    internal enum AnnoReviewTask
    {
        Flag,
        Report
    }
}
