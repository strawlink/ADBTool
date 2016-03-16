using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ADBTool
{
    /// <summary>
    /// Interaction logic for ItemWindow.xaml
    /// </summary>
    public partial class ItemWindow : Window
    {
        public string InputName
        {
            get { return textBoxName.Text; }
        }

        public string InputPath
        {
            get { return textBoxPath.Text; }
        }

        public string InputBundleId
        {
            get { return textBoxBundleId.Text; }
        }

        public ItemWindow(ListObject listObject = null)
        {
            InitializeComponent();

            if(listObject != null)
            {
                SetFromListObject(listObject);
            }
            else
            {
                SetToDefault();
            }
        }

        private ListObject cachedListObject = null;
        
        private void SetFromListObject(ListObject listObject)
        {
            cachedListObject = listObject;
            textBoxName.Text = listObject.Name;
            textBoxPath.Text = listObject.Path;
            textBoxBundleId.Text = listObject.BundleId;
        }

        private void SetToDefault()
        {
            textBoxName.Text = "New Application";
            textBoxPath.Text = "";
            textBoxBundleId.Text = "";
        }

        private bool ValidateInput()
        {
            ListObject foundObject = SettingsManager.Settings.ItemCollection.Find(x => x != cachedListObject && x.Name == InputName);
            if(foundObject != null)
            {
                MessageBox.Show("Name is already in use.");
                return false;
            }

            // We only want to make sure it's an APK, it doesn't matter if the file doesn't exist
            if (!InputPath.EndsWith(".apk", StringComparison.InvariantCultureIgnoreCase))
            {
                MessageBox.Show("Invalid path, it must direct to a valid .apk");
                return false;
            }

            return true;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if(ValidateInput())
            {
                DialogResult = true;
            }
        }
    }
}
