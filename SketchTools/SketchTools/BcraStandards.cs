using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    /// <summary>
    /// Standard file locations
    /// </summary>
    public static class FileLocations
    {
        public static readonly String addInDirectory = @"C:\2013-BCRA-RVT\Addins\";
        public static readonly String addInResourcesDirectory = @"C:\2013-BCRA-RVT\Addins\Resources\";
        public static readonly String imperialTemplatesDirectory = @"C:\ProgramData\Autodesk\RAC 2013\Family Templates\English_I\";
        public static readonly String bcraTitleBlocksNetworkDirectory = @"X:\CAD\00Revit Library - 2013\_BCRA-Titleblocks\";
        public static readonly String askMarkerFile = "ASK MARKER.rfa";
        public static readonly String blankTitleBlockFile = "BLANK TITLE BLOCK.rfa";
    }

    /// <summary>
    /// Standards for BCRARVT
    /// </summary>
    public static class Standard
    {
        public static readonly Double verticalMargin = 0.5 / 12.0;
        public static readonly Double horizontalMargin = 0.5 / 12.0;
        //This is a larger margin to leave room for the title block information
        public static readonly Double bottomTitleblockMargin = 2.25 / 12.0;
        //This is a larger margin to leave room for the title block information
        public static readonly Double sideTitleBlockMargin = 2.25 / 12.0;

        public static readonly Dictionary<String, SheetConfiguration> blankSheetConfigurations = new Dictionary<string, SheetConfiguration>()
        {
            {"8.5x11 (PORTRAIT)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory+FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "8.5x11 (PORTRAIT)",
                    SheetSize = new SheetDimensions(8.5/12.0, 11.0/12.0)
                }},
            {"8.5x11 (LANDSCAPE)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory+FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "8.5x11 (LANDSCAPE)",
                    SheetSize = new SheetDimensions(11.0/12.0, 8.5/12.0)
                }},
            {"11x17 (LANDSCAPE)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory+FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "11x17 (LANDSCAPE)",
                    SheetSize = new SheetDimensions(17.0/12.0, 11.0/12.0)
                }},
            {"11x17 (PORTRAIT)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "11x17 (PORTRAIT)",
                    SheetSize = new SheetDimensions(11.0/12.0, 17.0/12.0)
                }},
            {"18x24", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "18x24",
                    SheetSize = new SheetDimensions(24.0/12.0, 18.0/12.0)
                }},
            {"24x36", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "24x36",
                    SheetSize = new SheetDimensions(36.0/12.0, 24.0/12.0)
                }},
            {"30x42", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "30x42",
                    SheetSize = new SheetDimensions(42.0/12.0, 30.0/12.0)
                }},
            {"36x48", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + FileLocations.blankTitleBlockFile),
                    FamilySymbolName = "36x48",
                    SheetSize = new SheetDimensions(48.0/12.0, 36.0/12.0)
                }}
        };

        /// <summary>
        /// SheetConfiguration for standard BCRA SK titleblocks
        /// </summary>
        public static readonly Dictionary<String, SheetConfiguration> askSheetConfigurations = new Dictionary<string, SheetConfiguration>()
        {
            {"8.5x11 (Portrait)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + "BCRA_8.5x11 - SK.rfa"),
                    FamilySymbolName = "BCRA_8.5x11 - SK",
                    SheetSize = new SheetDimensions()
                    {
                            Width = 8.5/12.0,
                            Height = 11.0/12.0,
                            TopMargin = Standard.verticalMargin,
                            RightMargin = Standard.horizontalMargin,
                            BottomMargin = Standard.bottomTitleblockMargin,
                            LeftMargin = Standard.horizontalMargin
                    }
                }},
            {"11x17 (Portrait)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + "BCRA_11x17 - V - SK.rfa"),
                    FamilySymbolName = "BCRA_11x17 - V - SK",
                    SheetSize = new SheetDimensions()
                    {
                            Width = 11.0/12.0,
                            Height = 17.0/12.0,
                            TopMargin = Standard.verticalMargin,
                            RightMargin = Standard.horizontalMargin,
                            BottomMargin = Standard.bottomTitleblockMargin,
                            LeftMargin = Standard.horizontalMargin
                    }
                }},
            {"11x17 (Landscape)", new SheetConfiguration()
                {
                    RfaFile = new FileInfo(FileLocations.addInResourcesDirectory + "BCRA_11x17 - H - SK.rfa"),
                    FamilySymbolName = "BCRA_11x17 - H - SK",
                    SheetSize = new SheetDimensions()
                        {
                            Width = 17.0/12.0,
                            Height = 11.0/12.0,
                            TopMargin = Standard.verticalMargin,
                            RightMargin = Standard.sideTitleBlockMargin,
                            BottomMargin = Standard.verticalMargin,
                            LeftMargin = Standard.horizontalMargin
                        }
                }}
        };

        /// <summary>
        /// Extension method on ViewSheet to attempt to retrievew a corresponding key in Standard.AskSheetConfigurations
        /// </summary>
        /// <param name="sheet">The sheet to check against existing SheetConfigurations</param>
        /// <returns>the dictionary key corresonding to the ViewSheet</returns>
        public static String GetSheetConfigurationKey(this ViewSheet sheet)
        {
            FilteredElementCollector c = new FilteredElementCollector(sheet.Document, sheet.Id);
            c.OfCategory(BuiltInCategory.OST_TitleBlocks);
            if (1 == c.Count())
	        {
                String titleBlockName = ((FamilyInstance)c.First()).Symbol.Name;
                var q = from String k
                        in Standard.askSheetConfigurations.Keys
                        where titleBlockName == Standard.askSheetConfigurations[k].FamilySymbolName
                        select k;
                if (q.Count() > 0)
                {
                    return q.First();
                }
	        }

            return null;
        }
    }

    /// <summary>
    /// Represents a TitleBlock asset stored in an .rfa file
    /// </summary>
    public class SheetConfiguration
    {
        public FileInfo RfaFile { get; set; }
        public String FamilySymbolName { get; set; }
        public SheetDimensions SheetSize { get; set; }

        public SheetConfiguration() { }
    }

    /// <summary>
    /// Page dimensions. All values are stored in decimal feet
    /// </summary>
    public class SheetDimensions
    {
        public Double TopMargin { get; set; }
        public Double RightMargin { get; set; }
        public Double BottomMargin { get; set; }
        public Double LeftMargin { get; set; }
        public Double Height { get; set; }
        public Double Width { get; set; }

        public SheetDimensions() { }

        /// <summary>
        /// Constructor of a page with a given height and width
        /// </summary>
        /// <param name="width">The width of the page, in decimal feet</param>
        /// <param name="height">The height of the page, in decimal feet</param>
        public SheetDimensions(Double width, Double height)
            : this()
        {
            Height = height;
            Width = width;
            TopMargin = Standard.verticalMargin;
            RightMargin = Standard.horizontalMargin;
            BottomMargin = Standard.verticalMargin;
            LeftMargin = Standard.horizontalMargin;
        }
    }
}
