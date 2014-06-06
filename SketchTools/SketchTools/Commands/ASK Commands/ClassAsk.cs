using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    /// <summary>
    /// Encapsulates information about an ASK and manipulates its representation in the project
    /// </summary>
    public class ASK
    {
        //The document in which the ASK lives
        private readonly Document dbDoc;

        /// <summary>
        /// The Id of the ViewSheet for the ASK.
        /// </summary>
        public ElementId ViewSheetId
        {
            get;
            private set;
        }

        /// <summary>
        /// The ViewSheet element for the ASK
        /// </summary>
        private ViewSheet AsViewSheet
        {
            get
            {
                return (ViewSheet)dbDoc.GetElement(ViewSheetId);
            }
        }

        /// <summary>
        /// The number of the sheet for the ASK in the project.
        /// Changed by setting the Parameter of the ViewSheet in the database, not by accessing this property
        /// </summary>
        public String Number
        {
            get
            {
                //An ASK should never be created without a set ViewSheetId
                if (null == ViewSheetId)
                {
                    throw new InvalidOperationException("Cannot retrieve Sheet number. The ViewSheet of this ASK is undefined");
                }
                ViewSheet sheet = (ViewSheet)dbDoc.GetElement(ViewSheetId);
                Parameter p = sheet.ParametersMap.get_Item("Sheet Number");
                return p.AsString();
            }
        }

        /// <summary>
        /// The key for the SheetConfiguration object stored in Standard.askSheetConfigurations
        /// this property is mutable to facilitate changing the sheet size
        /// </summary>
        public String SheetConfigurationKey
        {
            get
            {
                return this.AsViewSheet.GetSheetConfigurationKey();
            }
        }

        /// <summary>
        /// All SketchViews placed on the ASK Sheet. Retrieved when the property is accessed
        /// </summary>
        public List<SketchView> SketchViews
        {
            get
            {
                if (null == ViewSheetId)
                {
                    throw new InvalidOperationException("ASK.SketchViews.get(): SheetViewId must be set to retrieve SketchViews for ASK");
                }

                List<SketchView> views = new List<SketchView>();
                FilteredElementCollector c = new FilteredElementCollector(dbDoc, ViewSheetId);
                c.OfClass(typeof(Viewport));
                //todo: make sure legends, etc are excluded
                foreach (Viewport vp in c)
                {
                    View v = (View)dbDoc.GetElement(vp.ViewId);
                    views.Add(SketchView.GetSketchView(v));
                }
                return views;
            }
        }

        /// <summary>
        /// The number of the sheet on which the origin view is placed
        /// </summary>
        public String ReferenceSheetNumber
        {
            get
            {
                Parameter p = AsViewSheet.ParametersMap.get_Item("REFERENCE SHEET NUMBER");
                return p.AsString();
            }
        }

        /// <summary>
        /// The name of the sheet on which the origin view is placed
        /// </summary>
        public String ReferenceSheetName
        {
            get
            {
                Parameter p = AsViewSheet.ParametersMap.get_Item("REFERENCE SHEET NAME");
                return p.AsString();
            }
        }

        /// <summary>
        /// ASKs should be created with the GetAsk() factory method
        /// </summary>
        /// <param name="askDbDoc">The document that contains the ASK</param>
        private ASK(Document askDbDoc)
        {
            dbDoc = askDbDoc;
        }

        /// <summary>
        /// Retrieves an existing ASK from the document using its sheet number
        /// </summary>
        /// <param name="number">The sheet number of the ASK</param>
        /// <returns>The ASK, or null if not found</returns>
        public static ASK GetAsk(Document askDbDoc, String number)
        {
            FilteredElementCollector c = new FilteredElementCollector(askDbDoc);
            c.OfClass(typeof(ViewSheet));
            var s = from ViewSheet v in c where (number == v.SheetNumber) select v;

            if (0 == s.Count())
	        {
		        return null;
	        }
            else
	        {
                ViewSheet askSheetView = s.First();
                ASK retrievedAsk = new ASK(askDbDoc);
                retrievedAsk.ViewSheetId = askSheetView.Id;
                return retrievedAsk;
	        }
        }

        /// <summary>
        /// Retrieves an existing ASK from the document using its ViewSheet
        /// </summary>
        /// <param name="v">The ViewSheet of the ASK</param>
        /// <returns>The ASK, or null if not found</returns>
        public static ASK GetAsk(Document askDbDoc, ElementId viewSheetId)
        {
            ASK retrievedAsk = new ASK(askDbDoc);
            retrievedAsk.ViewSheetId = viewSheetId;
            return retrievedAsk;
        }

        /// <summary>
        /// Create an ASK sheet. This will add a new ViewSheet to the document
        /// <param name="number">The sheet number of the new ASK</param>
        /// <param name="sheetConfigurationKey">the SheetConfiguration of the new sheet, as stored in Standard.askSheetConfigurations</param>
        /// <param name="askDbDoc">The document within which to create the ASK</param>
        /// <returns>The ASK object created</returns>
        /// </summary>
        public static ASK CreateAskSheet(Document askDbDoc, String number, String sheetConfigurationKey)
        {
            ASK newAsk = new ASK(askDbDoc);

            if (!ASK.IsAskNumAvailable(number, askDbDoc))
            {
                throw new InvalidOperationException("Cannot create new sheet \"" + number + "\"");
            }

            //create sheet
            FamilySymbol askTitleBlockSymbol =
                GetAskTitleBlockFamilySymbol(Standard.askSheetConfigurations[sheetConfigurationKey], askDbDoc);
            ViewSheet newSheet = askDbDoc.Create.NewViewSheet(askTitleBlockSymbol);
            //set the public AskSheetId property
            newAsk.ViewSheetId = newSheet.Id;

            Parameter p;
            //set the sheet number of the new sheet, using the argument that was passed in
            p = newSheet.ParametersMap.get_Item("Sheet Number");
            p.Set(number);

            //set the name of the new sheet, using the argument that was passed in
            p = newSheet.ParametersMap.get_Item("Sheet Name");
            p.Set("Sketch " + number.ToString());

            //set "Sheet Discipline" parameter if it exists
            if (newSheet.ParametersMap.Contains("Sheet Discipline"))
            {
                p = newSheet.ParametersMap.get_Item("Sheet Discipline");
                p.Set("09 - Construction Admin");
            }

            //set e-data to flag this ViewSheet as an ASK
            Schema askSheetSchema = AskSchemas.GetAskSheetSchema();
            Entity isAskEntity = new Entity(askSheetSchema);
            isAskEntity.Set<Boolean>("IsAsk", true);
            newSheet.SetEntity(isAskEntity);

            return newAsk;
        }

        /// <summary>
        /// Places a view on the ASK sheet and positions it. Once the view is place, it becomes a SketchView by definition. Can only be called within an open
        /// transcation. The view must already be configured as a SketchView with e-data written to it before this command is called.
        /// </summary>
        /// <param name="sv">The view to be place</param>
        /// <returns>A SketchView reference to the View that was placed</returns>
        public SketchView PlaceSketchView(View sv, XYZ titleCenterPoint = null)
        {
            //find the titleblock used for this ViewSheet
            FilteredElementCollector c = new FilteredElementCollector(dbDoc, ViewSheetId);
            Element ASKTitleBlock = c.OfCategory(BuiltInCategory.OST_TitleBlocks).First<Element>();
            if (null == titleCenterPoint)
            {
                BoundingBoxXYZ titleBounds = ASKTitleBlock.get_BoundingBox(this.AsViewSheet);
                titleCenterPoint = new XYZ(
                    (titleBounds.Min.X + titleBounds.Max.X) / 2,
                    (titleBounds.Min.Y + titleBounds.Max.Y) / 2,
                    0);
            }

            Viewport sketchViewViewport = Viewport.Create(dbDoc, this.ViewSheetId, sv.Id, titleCenterPoint);
            //The view cannot be accessed as a SketchView until it's placed on an AskSheet
            SketchView sketchView = SketchView.GetSketchView(sv);
            //center the view port. Viewport comes in with its origin lined up to the insertion point
            Outline sketchViewViewportOutline = sketchViewViewport.GetBoxOutline();
            XYZ viewportCenterPoint = Calcs.GetCenterPoint(sketchViewViewportOutline);
            XYZ viewportTranslate = new XYZ(
                titleCenterPoint.X - viewportCenterPoint.X,
                titleCenterPoint.Y - viewportCenterPoint.Y,
                0);
            sketchViewViewport.Location.Move(viewportTranslate);

            //todo: adjust the viewport title

            //Set the REFERENCE SHEET NAME and REFERENCE SHEET NUMBER parameters on the AskSheet
            //find what ViewSheet the originView is placed on, if there is one
            //todo: leave unchanged if already set
            c = new FilteredElementCollector(dbDoc);
            c.OfCategory(BuiltInCategory.OST_Sheets);
            var q = from ViewSheet e in c where e.Views.Contains((View)dbDoc.GetElement(sketchView.OriginViewId)) select e;
            if (q.Count() > 0)
            {
                ViewSheet ownerSheet = q.First();

                Parameter p;
                //set REFERENCE SHEET NAME if it exists
                if (this.AsViewSheet.ParametersMap.Contains("REFERENCE SHEET NAME"))
                {
                    p = this.AsViewSheet.ParametersMap.get_Item("REFERENCE SHEET NAME");
                    p.Set(ownerSheet.Name);
                }

                //set REFERENCE SHEET NUMBER if it exists
                if (this.AsViewSheet.ParametersMap.Contains("REFERENCE SHEET NUMBER"))
                {
                    p = this.AsViewSheet.ParametersMap.get_Item("REFERENCE SHEET NUMBER");
                    p.Set(ownerSheet.SheetNumber);
                }
            }

            return sketchView;
        }

        /// <summary>
        /// Determine the next available unused ASK number
        /// </summary>
        /// <returns>The next available number</returns>
        public static String GetNextAvailableAsk(Document dbDoc)
        {
            List<ASK> asks = dbDoc.GetAsks();
            //The number of an ASK will be determined by any digits appended to it
            //any preceding characters will be ignored
            Regex sheetNumRegEx = new Regex("[0-9]+$");
            //the highest ASK number found
            Int32 maxNum = 0, currNum = 0;

            foreach (ASK a in asks)
            {
                if (sheetNumRegEx.IsMatch(a.Number))
                {
                    currNum = Int32.Parse(sheetNumRegEx.Match(a.Number).ToString());
                    if (currNum > maxNum)
                    {
                        maxNum = currNum;
                    }
                }
            }

            return ASK.GetCanonicalSheetNumber(maxNum + 1);
        }

        /// <summary>
        /// Checks whether the ASK number is in use
        /// </summary>
        /// <param name="num">The number to check</param>
        /// <param name="dbDoc">The document to check</param>
        /// <returns>Whether the number available</returns>
        public static Boolean IsAskNumAvailable(String num, Document dbDoc)
        {
            Boolean isAvailable = true;

            //check to see if the Sheet Number is in use
            FilteredElementCollector c = new FilteredElementCollector(dbDoc);
            c.OfCategory(BuiltInCategory.OST_Sheets);
            var q = from ViewSheet v in c where v.SheetNumber == num select v;
            if (q.Count() > 0) isAvailable = false;
            
            return isAvailable;
        }

        /// <summary>
        /// Return a canoncial sheet number based on the given ASK number
        /// </summary>
        /// <param name="num">The number of the ASK</param>
        /// <returns>The canonical number of the ASK sheet</returns>
        public static String GetCanonicalSheetNumber(Int32 num)
        {
            return String.Format("SK{0}", num);
        }

        /// <summary>
        /// Helper method to retrieve the FamilySymbol for ASK TITLEBLOCK. Loads if not already found in dbDoc.
        /// Can only be called from within an open transaction.
        /// </summary>
        /// <param name="sheetToLoad">A SheetConfiguration object representing the sheet to be loaded</param>
        /// <returns>The loaded FamilySymbol for the ASK TITLEBLOCK</returns>
        private static FamilySymbol GetAskTitleBlockFamilySymbol(SheetConfiguration sheetToLoad, Document dbDoc)
        {
            //determine if ASK TITLE BLOCK is loaded
            foreach (FamilySymbol s in dbDoc.TitleBlocks)
            {
                if (s.Family.Name == sheetToLoad.FamilySymbolName)
                    return s;
            }

            //load if not loaded
            Boolean success = dbDoc.LoadFamily(sheetToLoad.RfaFile.FullName);
            if (!success) throw new Exception("Could not find " + sheetToLoad.RfaFile.FullName);

            //now that the titleblock family has been loaded, retrieve the FamilySymbol
            foreach (FamilySymbol s in dbDoc.TitleBlocks)
            {
                if (s.Name == sheetToLoad.FamilySymbolName) return s;
            }

            //default
            return null;
        }
    }
}
