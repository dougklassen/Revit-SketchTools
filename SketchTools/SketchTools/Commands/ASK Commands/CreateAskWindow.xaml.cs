using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BCRA.Revit
{
    /// <summary>
    /// Interaction logic for CreateASKWindow.xaml
    /// </summary>
    public partial class CreateASKWindow : Window
    {
        //The current document, stored so that the sheet number of the ASK can be validated
        Document dbDoc;
        //keeps nextAvailableNumber as a field, to avoid calling GetNextAvailableASK() repeatedly
        String nextAvailableNumber;
        String createButtonText;

        /// <summary>
        /// Points to a sheet configuration stored in Standard.askSheetConfigurations
        /// </summary>
        public String NewAskSheetConfigurationKey
        {
            get
            {
                return (String)((ComboBoxItem)comboBoxSheetSize.SelectedItem).Content;
            }
        }

        /// <summary>
        /// The Number of the new ASK
        /// </summary>
        public String NewAskNumber
        {
            get
            {
                return txtBoxASKNumber.Text;
            }
        }

        private CreateASKWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the Window with access to the UIApplication object and text for the "Create" button 
        /// </summary>
        /// <param name="uiApp">a reference to the UIApplication so that dbDoc can be set and the window positioned</param>
        /// <param name="buttonText">String to set the text of the create button
        /// Should be either "Create" when an ASK will be created directly, or
        /// "Pick Region" when a region must be specified for the SheetView</param>
        public CreateASKWindow(UIApplication uiApp, String buttonText = "Create")
            : this()
        {
            dbDoc = uiApp.ActiveUIDocument.Document;
            createButtonText = buttonText;
            //position the window near the middle of the Revit window
            Top = (uiApp.DrawingAreaExtents.Bottom + uiApp.DrawingAreaExtents.Top) / 2 - 100;
            Left = (uiApp.DrawingAreaExtents.Left + uiApp.DrawingAreaExtents.Right) / 2 - 175;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            btnCreate.Content = createButtonText;
            nextAvailableNumber = ASK.GetNextAvailableAsk(dbDoc);
            txtBoxASKNumber.Text = nextAvailableNumber;

            foreach (var k in Standard.askSheetConfigurations.Keys)
            {
                ComboBoxItem cbItem = new ComboBoxItem();
                cbItem.Content = k;
                comboBoxSheetSize.Items.Add(cbItem);
            }
            //select the first item, so the selection won't be blank
            comboBoxSheetSize.SelectedItem = comboBoxSheetSize.Items[0];
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            //data validation on txtBoxASKNumber
            String enteredNumber;
            try
            {
                enteredNumber = txtBoxASKNumber.Text;
            }
            catch (Exception)
            {
                messageLabel.Content = "Please enter a valid number";
                txtBoxASKNumber.Text = nextAvailableNumber;
                return;
            }
            if (!ASK.IsAskNumAvailable(enteredNumber, dbDoc))
            {
                messageLabel.Content = enteredNumber + " already exists";
                txtBoxASKNumber.Text = nextAvailableNumber;
                return;
            }

            //this is the return value of ShowDialog(). true indicates the command has not been cancelled
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            //this is the return value of ShowDialog(). false indicates the command has been cancelled
            DialogResult = false;
            Close();
        }

        //disallow input of non-alphanumberic
        private void txtBoxASKNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //anything but alphanumerics or periods are disallowed
            Regex disallowedChars = new Regex("[^0-9^A-Z^a-z.]");
            //if a disallowed character is found, do not continue with the text input
            if (disallowedChars.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }
    }
}
