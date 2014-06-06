using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace BCRA.Revit
{
    /// <summary>
    /// Interaction logic for AnnoReviewWindow.xaml
    /// </summary>
    public partial class AnnoReviewWindow : Window
    {
        //fields
        private AnnoReviewLogic logic;

        //constructors
        internal AnnoReviewWindow(AnnoReviewLogic logic) : this()
        {
            this.logic = logic;
            IEnumerable<ElementId> queryResultIds = logic.GetIdAnnotationsContainingQuery(logic.SearchString);
            IEnumerable<ResultDescription> queryResult = logic.GetSelectedTextNotesDescription(queryResultIds);

            this.Title = "Search results for \"" + logic.SearchString + "\"";
            queryResultTextBlock.Text = queryResult.Count<ResultDescription>() + " instances found";
            queryResultDataGrid.ItemsSource = queryResult;
        }

        private AnnoReviewWindow()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void flagButton_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<ElementId> queryResults = logic.GetIdAnnotationsContainingQuery(logic.SearchString);
            logic.FlagSelectedTextNotes(queryResults);
        }
    }
}
