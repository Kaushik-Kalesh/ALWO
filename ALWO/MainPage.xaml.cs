using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ALWO
{
    public class ProcessItem : INotifyPropertyChanged
    {
        private Visibility _deleteButtonVisibility;

        public string ProcessName { get; set; }

        public Visibility DeleteButtonVisibility
        {
            get => _deleteButtonVisibility;
            set
            {
                if (_deleteButtonVisibility != value)
                {
                    _deleteButtonVisibility = value;
                    OnPropertyChanged(nameof(DeleteButtonVisibility));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed partial class MainPage : Page
    {
        public ObservableCollection<ProcessItem> ProcessesCollection { get; } = new ObservableCollection<ProcessItem>();
        private ObservableCollection<ProcessItem> TemporaryProcessesCollection = new ObservableCollection<ProcessItem>();
        private Dictionary<string, string> processesNameMap = new Dictionary<string, string>();
        private Dictionary<string, string> temporaryProcessesNameMap = new Dictionary<string, string>();
        private string chosenWorkspaceName = "";
        private bool isEditModeEnabled = false;
        private bool isAppChooserWindowOpen = false;

        public MainPage()
        {
            this.InitializeComponent();

            ProcessesListView.ItemsSource = ProcessesCollection;
        }

        // Helper tools
        public async void ShowMessageDialog(string message)
        {
            try
            {
                await new ContentDialog
                {
                    Title = "Message",
                    Content = message,
                    CloseButtonText = "Ok",
                    XamlRoot = MainGrid.XamlRoot
                }.ShowAsync();
            }
            catch { }
        }

        public delegate void FunctionTemplate(); // Function paramter template for ShowConfirmationDialog

        public async Task<int> ShowConfirmationDialog(string content, FunctionTemplate primary, FunctionTemplate secondary = null)
        {
            try
            {
                ContentDialogResult res = await new ContentDialog
                {
                    Title = "Warning",
                    Content = content,
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    XamlRoot = MainGrid.XamlRoot
                }.ShowAsync();

                if (res == ContentDialogResult.Primary)
                {
                    primary();
                }
                else
                {
                    if (secondary != null)
                    {
                        secondary();
                    }
                }
                return 0;
            }
            catch { }

            return -1;
        }

        // Hide or Show editing tools 
        private void ToggleEditMode(bool _isEditModeEnabled)
        {
            isEditModeEnabled = _isEditModeEnabled;
            WorkspaceNameTextBox.IsEnabled = _isEditModeEnabled;
            ProcessesTextBlock.Visibility = _isEditModeEnabled ? Visibility.Collapsed : Visibility.Visible;
            AddProcessesButton.Visibility = _isEditModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            EditControlStackPanel.Visibility = _isEditModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            EditButton.Visibility = _isEditModeEnabled ? Visibility.Collapsed : Visibility.Visible;
            foreach (var processItem in ProcessesCollection)
            {
                processItem.DeleteButtonVisibility = _isEditModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
            ProcessesListView.CanDragItems = _isEditModeEnabled;
            ProcessesListView.CanReorderItems = _isEditModeEnabled;
            ProcessesListView.AllowDrop = _isEditModeEnabled;
        }

        private void AddProcess(string processName, string processPath)
        {
            if (processName == "") { return; }

            if (processesNameMap.TryAdd(processName, processPath))
            {
                ProcessesCollection.Add(new ProcessItem
                {
                    ProcessName = processName,
                    DeleteButtonVisibility = isEditModeEnabled ? Visibility.Visible : Visibility.Collapsed
                });
            }
        }

        private void ClearProcesses()
        {
            processesNameMap.Clear();
            ProcessesCollection.Clear();
        }

        public async Task FetchProcesses(string workspaceName, bool isNewWorkspace = false)
        {
            if (isEditModeEnabled)
            {
                await ShowConfirmationDialog("Save Changes?", SaveChanges, CancelChanges);
            }

            // Ignore fetch requests from the workspace already in focus
            if (workspaceName == chosenWorkspaceName) { return; }

            ClearProcesses();

            WorkspaceNameTextBox.Visibility = Visibility.Visible;
            WorkspaceNameTextBox.Text = chosenWorkspaceName = workspaceName;
            List<string> processNames = WorkspaceInfoAccess.GetProcessNames(chosenWorkspaceName), processPaths = WorkspaceInfoAccess.GetProcessPaths(chosenWorkspaceName);
            for (int i = 0; i < processNames.Count; i++)
            {
                AddProcess(processNames[i], processPaths[i]);
            }
            ProcessesTextBlock.Visibility = Visibility.Visible;
            ProcessesTextBlock.Text = processesNameMap.Count == 0 ? "No Processes" : "Processes";
            RunButton.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
            DeleteWorkspaceButton.Visibility = Visibility.Visible;

            if (isNewWorkspace)
            {
                ToggleEditMode(true); // Open new workspace in edit mode
            }
        }

        private void SaveChanges()
        {
            if (WorkspaceInfoAccess.GetWorkspaceNames().Contains(WorkspaceNameTextBox.Text) && WorkspaceNameTextBox.Text != chosenWorkspaceName)
            {
                ShowMessageDialog("Workspace Name already used");
                return;
            }

            ToggleEditMode(false);

            // Delete process names based on the updated ProcessesCollection
            foreach (var processName in processesNameMap.Keys)
            {
                if (!ProcessesCollection.Select(e => e.ProcessName).Contains(processName))
                {
                    processesNameMap.Remove(processName);
                }
            }

            ProcessesTextBlock.Text = processesNameMap.Count == 0 ? "No Processes" : "Processes";
            var processesPathList = new List<string>();
            var processesNameList = new List<string>();
            foreach (var processItem in ProcessesCollection)
            {
                processesNameList.Add(processItem.ProcessName);
                processesPathList.Add(processesNameMap[processItem.ProcessName]);
            }

            string encodedProcessPaths = string.Join(",", processesPathList), encodedProcessNames = string.Join(",", processesNameList);
            if (chosenWorkspaceName != WorkspaceNameTextBox.Text)
            {
                WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);
                MainWindow.WorkspacesCollection.Remove(MainWindow.WorkspacesCollection.FirstOrDefault(e => e.WorkspaceName == chosenWorkspaceName));
                chosenWorkspaceName = WorkspaceNameTextBox.Text;
                // Update the workspace name in the NavigationView
                MainWindow.WorkspacesCollection.Add(new WorkspaceNavigationItem { WorkspaceName = chosenWorkspaceName });
                WorkspaceInfoAccess.AddWorkspace(chosenWorkspaceName, encodedProcessNames, encodedProcessPaths);
            }
            else
            {
                WorkspaceInfoAccess.UpdateWorkspace(chosenWorkspaceName, encodedProcessNames, encodedProcessPaths);
            }
        }

        private void CancelChanges()
        {
            ClearProcesses();

            // Discard the changes 
            TemporaryProcessesCollection.ToList().ForEach(e => ProcessesCollection.Add(e));
            temporaryProcessesNameMap.ToList().ForEach(e => processesNameMap.TryAdd(e.Key, e.Value));

            ToggleEditMode(false);
        }

        private async void DeleteWorkspace()
        {
            WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);
            // Delete the workspace from the NavigationView
            MainWindow.WorkspacesCollection.Remove(MainWindow.WorkspacesCollection.FirstOrDefault(e => e.WorkspaceName == chosenWorkspaceName));
            ClearProcesses();

            if (MainWindow.WorkspacesCollection.Count > 0)
            {
                await FetchProcesses(MainWindow.WorkspacesCollection.Last().WorkspaceName);
            }
            else
            {
                // If no workspaces are left, update the UI
                WorkspaceNameTextBox.Visibility = Visibility.Collapsed;
                WorkspaceNameTextBox.Text = chosenWorkspaceName = "";
                ProcessesTextBlock.Visibility = Visibility.Visible;
                ProcessesTextBlock.Text = "No Workspaces";
                RunButton.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteWorkspaceButton.Visibility = Visibility.Collapsed;
            }
        }

        //Event Handlers
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            TemporaryProcessesCollection.Clear();
            ProcessesCollection.ToList().ForEach(e => TemporaryProcessesCollection.Add(e));

            temporaryProcessesNameMap.Clear();
            processesNameMap.ToList().ForEach(e => temporaryProcessesNameMap.TryAdd(e.Key, e.Value)); 

            ToggleEditMode(true);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditModeEnabled)
            {
                await ShowConfirmationDialog("Save Changes?", SaveChanges, CancelChanges);
            }

            if (processesNameMap.Count == 0)
            {
                ShowMessageDialog("No processes to run");
                return;
            }

            foreach (var processItem in ProcessesCollection)
            {
                try
                {
                    Process.Start(processesNameMap[processItem.ProcessName]);
                    // TODO: Try to extend the interval
                    System.Threading.Thread.Sleep(50); // To ensure delay between app launches
                }
                catch (Exception ex)
                {
                    ShowMessageDialog($"Error running {processItem.ProcessName}: {ex.Message}");
                }
            }
        }

        private async void DeleteWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowConfirmationDialog("Are you sure?", DeleteWorkspace);
        }

        private void AddProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            if (isAppChooserWindowOpen) { return; }

            MainWindow.appChooserWindow = new AppChooserWindow();
            MainWindow.appChooserWindow.Activate();
            isAppChooserWindowOpen = true;
            MainWindow.appChooserWindow.Closed += AppChooserWindow_Closed;
        }

        private void AppChooserWindow_Closed(object sender, WindowEventArgs args)
        {
            (sender as AppChooserWindow).SelectedApps.ForEach(e => AddProcess(e.Name, e.Path));
            isAppChooserWindowOpen = false;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteButton = sender as Button;
            var deletedProcessName = (deleteButton.DataContext as ProcessItem).ProcessName;
            ProcessesCollection.Remove(ProcessesCollection.FirstOrDefault(e => e.ProcessName == deletedProcessName));
        }

        private void ClearProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearProcesses();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelChanges();
        }

        private void ProcessesListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            e.Data.Properties.Add("DraggedItems", ProcessesListView.SelectedItems.Cast<ProcessItem>().ToList());
        }

        private void ProcessesListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Properties.TryGetValue("DraggedItems", out object draggedItemsObj) && draggedItemsObj is List<ProcessItem> draggedItems)
            {
                int startingIndex = ProcessesCollection.IndexOf(draggedItems.First());

                // Reorder the items in the ObservableCollection based on the new order
                List<ProcessItem> newOrder = ProcessesListView.Items.Cast<ProcessItem>().ToList();
                for (int i = 0; i < draggedItems.Count; i++)
                {
                    int newIndex = newOrder.IndexOf(draggedItems[i]);
                    ProcessesCollection.Move(startingIndex + i, newIndex);
                }
            }
        }
    }
}
