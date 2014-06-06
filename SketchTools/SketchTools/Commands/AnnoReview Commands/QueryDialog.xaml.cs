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

namespace BCRA.Revit
{
    /// <summary>
    /// Interaction logic for QueryDialog.xaml
    /// </summary>
    public partial class QueryDialog : Window
    {
        //fields
        AnnoReviewLogic logic;

        //methods
        internal QueryDialog(AnnoReviewLogic logic)
            : this()
        {
            this.logic = logic;
        }

        private QueryDialog()
        {
            InitializeComponent();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            logic.CommandCancelled = true;
            this.Close();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            logic.SearchString = queryTextBox.Text.ToUpper();
            logic.Task = AnnoReviewTask.Report;
            this.Close();
        }

        private void flagButton_Click(object sender, RoutedEventArgs e)
        {
            logic.SearchString = queryTextBox.Text.ToUpper();
            logic.Task = AnnoReviewTask.Flag;
            this.Close();
        }
    }
}
