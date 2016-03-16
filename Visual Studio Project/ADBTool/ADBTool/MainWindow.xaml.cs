using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ADBTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ADBConnection _connection = null;

        private const string ACTION_INSTALL     = "install -r \"{0}\""; // path
        private const string ACTION_UNINSTALL   = "uninstall {0}"; // bundleID
        private const string ACTION_CLEAR_DATA  = "shell pm clear {0}"; // bundleID
        private const string ACTION_LAUNCH      = "shell monkey -p {0} -c android.intent.category.LAUNCHER 1"; // bundleID
        private const string ACTION_KILL        = "shell am force-stop {0}"; // bundleID
        private const string ACTION_DEVICES     = "devices -l";

        public MainWindow()
        {
            InitializeComponent();

            SettingsManager.LoadSettings();
            UpdateUIFromSettings(SettingsManager.Settings);

            _connection = new ADBConnection(Output);

            RefreshDeviceList();

            Output("Idle");
        }

        // TODO: Event-based instead?
        private void UpdateUIFromSettings(Settings settings)
        {
            itemComboBox.Items.Clear();

            foreach (ListObject item in settings.ItemCollection)
            {
                itemComboBox.Items.Add(item.Name);
            }

            if (string.IsNullOrEmpty(settings.SelectedListObject))
            {
                if (settings.ItemCollection.Count > 0)
                {
                    settings.SelectedListObject = settings.ItemCollection[0].Name;
                }
            }

            itemComboBox.SelectedItem = settings.SelectedListObject;

            OnItemCollectionSelectionChanged();

            itemComboBox.IsEnabled = itemComboBox.HasItems;
            bool hasSelected = itemComboBox.SelectedItem != null;
            buttonEditItem.IsEnabled = hasSelected;
            buttonRemoveItem.IsEnabled = hasSelected;

            buttonInstall.IsEnabled = hasSelected;
            buttonLaunch.IsEnabled = hasSelected;
            buttonKill.IsEnabled = hasSelected;
            buttonClearData.IsEnabled = hasSelected;
            buttonUninstall.IsEnabled = hasSelected;
        }

        private void RefreshDeviceList()
        {
            // TODO: Get devices from _connection

            deviceComboBox.IsEnabled = deviceComboBox.HasItems;
        }

        private ListObject FindCurrentSelectedItem()
        {
            return SettingsManager.Settings.ItemCollection.Find(x => x.Name == itemComboBox.SelectedItem as string);
        }

        private void OnItemCollectionSelectionChanged()
        {
            ListObject objectInfo = FindCurrentSelectedItem();
            UpdateLabels(objectInfo);

            if (objectInfo != null)
            {
                // Temp (?)
                UpdateSettingsFromUI();
            }
        }

        private void UpdateLabels(ListObject objectInfo)
        {
            string bundleId = objectInfo != null ? objectInfo.BundleId : "";
            string path = objectInfo != null ? objectInfo.Path : "";

            labelBundleId.Content = bundleId;
            labelPath.Content = path;
        }

        private void UpdateSettingsFromUI()
        {
            SettingsManager.Settings.SelectedListObject = itemComboBox.SelectedItem as string;
            SettingsManager.SaveSettings();
        }

        private const string OUTPUT_LOG_FORMAT = "[{0}] - {1}";

        private void Output(string text)
        {
            Action action = () =>
            {
                TimeSpan time = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                int index = listBoxOutputLog.Items.Add(string.Format(OUTPUT_LOG_FORMAT, time.ToString(), text));

                // This is not the best way to scroll, since it doesn't handle duplicates, but eh..
                listBoxOutputLog.ScrollIntoView(listBoxOutputLog.Items[index]);
            };

            Dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }


        private void OpenWindowAddItem()
        {
            ListObject listObject = new ListObject();
            ShowEditWindow(listObject, true);
        }

        private void OpenWindowEditItem()
        {
            ListObject selectedItem = FindCurrentSelectedItem();

            if (selectedItem != null)
            {
                ShowEditWindow(selectedItem, true);
            }
            else
            {
                // This should never happen, the edit button should be disabled in such scenario
                throw new Exception();
            }
        }

        private const string INSTALL    = "Installing \"{0}\""; // Name
        private const string UNINSTALL  = "Uninstalling \"{0}\""; // Name
        private const string LAUNCH     = "Launching \"{0}\""; // Name
        private const string KILL       = "Killing \"{0}\""; // Name
        private const string CLEAR_DATA = "Clearing data for \"{0}\""; // Name
        private const string DEVICES    = "Refreshing device list";

        #region Actions // TODO: Refactor

        private void ActionInstall(ListObject listObject)
        {
            //Output(string.Format(INSTALL, listObject.Name));
            if(!File.Exists(listObject.Path))
            {
                Output(string.Format("Invalid path \"{0}\"", listObject.Path));
                return;
            }

            _connection.StartExecuteCommand(string.Format(ACTION_INSTALL, listObject.Path));
        }

        private void ActionLaunch(ListObject listObject)
        {
            //Output(string.Format(LAUNCH, listObject.Name));

            _connection.StartExecuteCommand(string.Format(ACTION_LAUNCH, listObject.BundleId));
        }
        
        private void ActionUninstall(ListObject listObject)
        {
            //Output(string.Format(UNINSTALL, listObject.Name));

            _connection.StartExecuteCommand(string.Format(ACTION_UNINSTALL, listObject.BundleId));
        }
        
        private void ActionKill(ListObject listObject)
        {
            //Output(string.Format(KILL, listObject.Name));

            _connection.StartExecuteCommand(string.Format(ACTION_KILL, listObject.BundleId));
        }

        private void ActionClear(ListObject listObject)
        {
            //Output(string.Format(CLEAR_DATA, listObject.Name));

            _connection.StartExecuteCommand(string.Format(ACTION_CLEAR_DATA, listObject.BundleId));
        }
        
        private void ActionRefreshDevices()
        {
            //Output(DEVICES);

            _connection.StartExecuteCommand(ACTION_DEVICES);
        }

        #endregion

        private void ShowEditWindow(ListObject listObject, bool addObjectToSettings)
        {
            ItemWindow window = new ItemWindow(listObject);
            window.Owner = App.Current.MainWindow;
            bool? result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // OK pressed
                listObject.Name = window.InputName;
                listObject.Path = window.InputPath;
                listObject.BundleId = window.InputBundleId;

                if (addObjectToSettings)
                {
                    SettingsManager.Settings.ItemCollection.Add(listObject);
                }
                SettingsManager.Settings.SelectedListObject = listObject.Name;
                SettingsManager.SaveSettings();

                UpdateUIFromSettings(SettingsManager.Settings);
            }
            else
            {
                // Cancel pressed, discard window by doing nothing
            }
        }

        private void ShowDeleteItemConfirmation()
        {
            ListObject listObject = FindCurrentSelectedItem();

            if (listObject != null)
            {
                MessageBoxResult result = MessageBox.Show(App.Current.MainWindow, string.Format("Are you sure you want to delete '{0}'? This action can not be reverted.", listObject.Name), "Confirm Deletion", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    SettingsManager.Settings.ItemCollection.Remove(listObject);

                    // TODO: Proper selection of next item
                    //int index = SettingsManager.Settings.ItemCollection.IndexOf(listObject);
                    //int nextSelectedIndex = Math.Min(index, SettingsManager.Settings.ItemCollection.Count - 1);

                    string nextItem = string.Empty;

                    if (SettingsManager.Settings.ItemCollection.Count > 0)
                    {
                        nextItem = SettingsManager.Settings.ItemCollection[0].Name;
                    }

                    SettingsManager.Settings.SelectedListObject = nextItem;

                    SettingsManager.SaveSettings();
                    UpdateUIFromSettings(SettingsManager.Settings);
                }
            }
            else
            {
                // This should never happen, the remove button should be disabled in such scenario
                throw new Exception();
            }
        }

        #region Events

        private void buttonAddItem_Click(object sender, RoutedEventArgs e)
        {
            OpenWindowAddItem();
        }

        private void buttonEditItem_Click(object sender, RoutedEventArgs e)
        {
            OpenWindowEditItem();
        }

        private void buttonRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            ShowDeleteItemConfirmation();
        }

        private void itemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnItemCollectionSelectionChanged();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsManager.SaveSettings();
        }

        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {
            ActionInstall(FindCurrentSelectedItem());
        }
        
        private void buttonLaunch_Click(object sender, RoutedEventArgs e)
        {
            ActionLaunch(FindCurrentSelectedItem());
        }

        private void buttonKill_Click(object sender, RoutedEventArgs e)
        {
            ActionKill(FindCurrentSelectedItem());
        }

        private void buttonClearData_Click(object sender, RoutedEventArgs e)
        {
            ActionClear(FindCurrentSelectedItem());
        }

        private void buttonUninstall_Click(object sender, RoutedEventArgs e)
        {
            ActionUninstall(FindCurrentSelectedItem());
        }

        #endregion

        private void buttonRefreshDevice_Click(object sender, RoutedEventArgs e)
        {
            ActionRefreshDevices();
        }
    }
}