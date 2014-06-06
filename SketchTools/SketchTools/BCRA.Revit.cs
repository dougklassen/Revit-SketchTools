using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace BCRA.Revit
{
    /// <summary>
    /// Displays an about dialog for BCRARVT with version info
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AboutCommand : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref String message, ElementSet elements)
        {
            AssemblyName bcraRvtAssemblyName = Assembly.GetExecutingAssembly().GetName();
            String assemblyDescription = "BCRARVT 2013 add-in";
            String assemblyName = bcraRvtAssemblyName.Name;
            String assemblyVersion = bcraRvtAssemblyName.Version.ToString();
            String description = String.Format("{0}\n\nFilename: {1}.dll\nAssembly Version: {2}", assemblyDescription, assemblyName, assemblyVersion);
            TaskDialog.Show(assemblyName, description);

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }

    /// <summary>
    /// Used to make the about dialog always available, even without a Document open
    /// </summary>
    public class AboutCommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }

    //todo: rename to BCRARVTApplication, update manifest
    public class StartUpApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication revit)
        {
            BitmapImage largeIcon = new BitmapImage(new Uri(FileLocations.addInResourcesDirectory + "BCRA_large.jpg"));
            BitmapImage smallIcon = new BitmapImage(new Uri(FileLocations.addInResourcesDirectory + "BCRA_small.jpg"));
            BitmapImage largeQuery = new BitmapImage(new Uri(FileLocations.addInResourcesDirectory + "query_large.jpg"));
            BitmapImage smallQuery = new BitmapImage(new Uri(FileLocations.addInResourcesDirectory + "query_small.jpg"));

            RibbonPanel utilitiesRibbonPanel = revit.CreateRibbonPanel("BCRARVT Utilities");

            TextBoxData searchTextBoxData = new TextBoxData("searchString")
            {
                Image = smallQuery,
                ToolTip = "search for text"
            };
            
            PushButtonData clearFlaggedPushButtonData = new PushButtonData(
                "clearFlaggedButton",
                "Clear All Flagged",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.ClearFlaggedCommand")
                {
                    LargeImage = null,
                    Image = smallIcon,
                    LongDescription = "Unflag all flagged annotation",
                    ToolTip = "Locates all flagged annotation and returns it to its default appearance"
                };

            //need to add the textbox to get a reference before props can be set
            IList<RibbonItem> stackedItems = utilitiesRibbonPanel.AddStackedItems(searchTextBoxData, clearFlaggedPushButtonData);
            TextBox searchTextBox = stackedItems[0] as TextBox;
            searchTextBox.ShowImageAsButton = true;
            searchTextBox.PromptText = "Search annotations for string";
            searchTextBox.EnterPressed += new EventHandler<Autodesk.Revit.UI.Events.TextBoxEnterPressedEventArgs>(AnnoReviewLogic.QueryExecuted);
            utilitiesRibbonPanel.AddSeparator();

            PushButtonData createSketchViewPushButtonData = new PushButtonData(
                "createSketchViewButton",
                "Create Sketch",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.CreateSketchViewCommand")
                {
                    LargeImage = largeIcon,
                    Image = smallIcon,
                    LongDescription = "Create a sketch from an existing view and place on an SK sheet",
                    ToolTip = "Creates a sketch from an existing view",
                    AvailabilityClassName = "BCRA.Revit.CreateSketchViewCommandAvailability"
                };

            PushButtonData createBlankAskPushButtonData = new PushButtonData(
                "createBlankAskButton",
                "Create blank ASK",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.CreateBlankAskCommand")
                {
                    LargeImage = largeIcon,
                    Image = smallIcon,
                    LongDescription = "Create an empty SK sheet with no views placed on it",
                    ToolTip = "Create an empty SK sheet"
                };

            SplitButtonData createAskSplitButtonData = new SplitButtonData("createAskSplitButton", "Create ASK");
            SplitButton createAskSplitButton = (SplitButton)utilitiesRibbonPanel.AddItem(createAskSplitButtonData);
            createAskSplitButton.AddPushButton(createSketchViewPushButtonData);
            createAskSplitButton.AddPushButton(createBlankAskPushButtonData);

            PushButtonData hideMarkersCommandPushButtonData = new PushButtonData(
                "HideMarkersButton",
                "Hide ASK markers",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.HideMarkersCommand")
                {
                    LargeImage = largeIcon,
                    Image = smallIcon,
                    LongDescription = "Hide all ASK markers in the project",
                    ToolTip = "Hide all ASK markers"
                };

            PushButtonData showMarkersCommandPushButtonData = new PushButtonData(
                "ShowMarkersButton",
                "Show ASK markers",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.ShowMarkersCommand")
            {
                LargeImage = largeIcon,
                Image = smallIcon,
                LongDescription = "Show all hidden ASK markers in the project",
                ToolTip = "Show all ASK markers"
            };

            PulldownButtonData managePullDownData = new PulldownButtonData("managePullDown", "Manage ASKs")
            {
                LargeImage = largeIcon,
                Image = smallIcon,
                LongDescription = "Manage ASKs in the project",
                ToolTip = "manage ASKs"
            };
   
            PushButtonData updateASKPushButtonData = new PushButtonData(
                "updateASKButton",
                "Update ASK",
                FileLocations.addInDirectory + "BCRARVT.dll",
                "BCRA.Revit.UpdateASKCommand")
            {
                LargeImage = largeIcon,
                Image = smallIcon,
                LongDescription = "Update ASK",
                ToolTip = "Update an ASK based on changes to the original drawing",
                AvailabilityClassName = "BCRA.Revit.MarkerSelectedCommandAvailability"
            };

            //stackedItems is recycled from above
            stackedItems = utilitiesRibbonPanel.AddStackedItems(managePullDownData, updateASKPushButtonData);
            PulldownButton managePullDown = (PulldownButton)stackedItems.First();
            managePullDown.AddPushButton(hideMarkersCommandPushButtonData);
            managePullDown.AddPushButton(showMarkersCommandPushButtonData);

            PushButtonData aboutPushButtonData = new PushButtonData(
                "AboutButton", //name of the button
                "About", //text on the button
                FileLocations.addInDirectory + "BCRARVT.dll", //assembly location
                "BCRA.Revit.AboutCommand")
                {
                    LargeImage = largeQuery,
                    Image = smallQuery,
                    AvailabilityClassName = "BCRA.Revit.AboutCommandAvailability",
                    LongDescription = "Version info for BCRARVT add-in"
                };

            utilitiesRibbonPanel.AddSlideOut();
            utilitiesRibbonPanel.AddItem(aboutPushButtonData);

            //Updater to purge ASK data and markers from views created by duplication
            ViewAskPurgerUpdater purgerUpdater = new ViewAskPurgerUpdater(revit.ActiveAddInId);
            //Register updater and trigger on creation of new views.
            //Optional so user will not get a nag if they open an updater modified file without the updater installed
            UpdaterRegistry.RegisterUpdater(purgerUpdater, true);
            ElementCategoryFilter viewFilter = new ElementCategoryFilter(BuiltInCategory.OST_Views);
            UpdaterRegistry.AddTrigger(purgerUpdater.GetUpdaterId(), viewFilter, Element.GetChangeTypeElementAddition());

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication revit)
        {
            return Result.Succeeded;
        }
    }
}
