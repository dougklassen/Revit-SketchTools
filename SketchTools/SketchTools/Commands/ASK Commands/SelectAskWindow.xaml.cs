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

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    /// <summary>
    /// Interaction logic for SelectAskWindow.xaml
    /// </summary>
    public partial class SelectAskWindow : Window
    {
        Document dbDoc;

        public String SelectedAskNumber
        {
            get
            {
                String selectedItem = ((ComboBoxItem)(comboBoxSelectAsk.SelectedItem)).Content.ToString();

                //a return of null will indicate new ASK has been selected
                if ("New SK" == selectedItem)
                {
                    return null;
                }
                else
                {
                    return selectedItem;
                }
            }
        }

        private SelectAskWindow()
        {
            InitializeComponent();
        }

        public SelectAskWindow(UIApplication uiApp)
            : this()
        {
            dbDoc = uiApp.ActiveUIDocument.Document;

            ComboBoxItem cbItem = new ComboBoxItem();
            cbItem.Content = "New SK";
            comboBoxSelectAsk.Items.Add(cbItem);

            List<ASK> asks = dbDoc.GetAsks();
            foreach (ASK a in asks)
            {
                cbItem = new ComboBoxItem();
                cbItem.Content = a.Number;
                comboBoxSelectAsk.Items.Add(cbItem);
            }

            Top = (uiApp.DrawingAreaExtents.Bottom + uiApp.DrawingAreaExtents.Top) / 2 - 100;
            Left = (uiApp.DrawingAreaExtents.Left + uiApp.DrawingAreaExtents.Right) / 2 - 175;

            comboBoxSelectAsk.SelectedIndex = 0;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
