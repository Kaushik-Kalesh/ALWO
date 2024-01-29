using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Diagnostics;
using Microsoft.UI.Xaml.Shapes;
using Windows.Media.Protection.PlayReady;
using static System.Net.WebRequestMethods;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ALWO
{
    public class ProcessItem : INotifyPropertyChanged
    {
        private Visibility _deleteButtonVisibility;
        private Thickness _processNameMargin;

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

    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<ProcessItem> ProcessesCollection { get; } = new ObservableCollection<ProcessItem>();
        private Dictionary<string, Dictionary<string, string>> workspaces = new Dictionary<string, Dictionary<string, string>>();
        private string chosenWorkspaceName = "";
        private bool isEditModeActive = false;

        public MainWindow()
        {
            this.InitializeComponent();
            ProcessesListView.ItemsSource = ProcessesCollection;
            FetchWorkspaces();
        }

        // Helper function to display Message Dialogs
        private async void ShowMessageDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Message",
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = MainGrid.XamlRoot
            };

            await dialog.ShowAsync();
        }

        // Fetch workspaces from sqlite db
        private void FetchWorkspaces()
        {
            foreach (var workspaceName in WorkspaceInfoAccess.GetWorkspaceNames())
            {
                workspaces[workspaceName] = new Dictionary<string, string>();
                foreach (var processPath in WorkspaceInfoAccess.GetProcessPaths(workspaceName))
                {
                    var processName = System.IO.Path.GetFileName(processPath);
                    if (processPath == "")
                    {
                        workspaces[workspaceName].TryAdd(processName, processPath);
                    }
                }     
            }
        }
        private void AddProcess(string processName)
        {
            if (processName == "") { return; }
            ProcessesCollection.Add(new ProcessItem
            {
                ProcessName = processName,
                DeleteButtonVisibility = isEditModeActive ? Visibility.Visible : Visibility.Collapsed
            });
        }

        // Clear processes from the UI
        private void ClearProcesses()
        {
            if (!workspaces.TryAdd(chosenWorkspaceName, new Dictionary<string, string>()))
            {
                workspaces[chosenWorkspaceName] = new Dictionary<string, string>();
            }
            ProcessesCollection.Clear();
        }

        private void ToggleProcessItemState()
        {
            foreach (var processItem in ProcessesCollection)
            {
                processItem.DeleteButtonVisibility = isEditModeActive ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        //Event Handlers
        private void WorkspaceNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FetchProcessesButton.Content = workspaces.Keys.Contains(WorkspaceNameTextBox.Text) ? "Fetch" : "Create";
        }

        private void FetchProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            // Ignore fetch requests from the workspace already in focus
            if (WorkspaceNameTextBox.Text == chosenWorkspaceName) { return; }

            ClearProcesses();             

            chosenWorkspaceName = WorkspaceNameTextBox.Text;
            workspaces[chosenWorkspaceName] = new Dictionary<string, string>();
            foreach (var processPath in WorkspaceInfoAccess.GetProcessPaths(chosenWorkspaceName))
            {
                var processName = System.IO.Path.GetFileName(processPath);
                workspaces[chosenWorkspaceName].TryAdd(processName, processPath);
                AddProcess(processName);
            }
            
            RunButton.Visibility = Visibility.Visible;
            EditProcessesButton.Visibility = Visibility.Visible;
            DeleteWorkspaceButton.Visibility = Visibility.Visible;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (workspaces[chosenWorkspaceName].Count == 0)
            {
                ShowMessageDialog("No processes to run");
                return;
            }

            foreach (var processItem in ProcessesCollection)
            {
                try
                {
                    Process.Start(workspaces[chosenWorkspaceName][processItem.ProcessName]);
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    ShowMessageDialog($"Error running {processItem.ProcessName}: {ex.Message}");
                }
            }
        }

        private void DeleteWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);
            ClearProcesses();
            chosenWorkspaceName = "";
        }

        private void EditProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            // Make the editing tools visible
            isEditModeActive = true;
            AddProcessesButton.Visibility = Visibility.Visible;
            EditControlStackPanel.Visibility = Visibility.Visible;
            EditProcessesButton.Visibility = Visibility.Collapsed;
            ToggleProcessItemState();
            ProcessesListView.CanDragItems = true;
            ProcessesListView.CanReorderItems = true;
            ProcessesListView.AllowDrop = true; 

        }

        private async void AddProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            var window = App.MainWindow;

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            openPicker.FileTypeFilter.Add(".exe");

            // Open the picker for the user to pick a file
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (StorageFile file in files)
                {
                    if (workspaces[chosenWorkspaceName].TryAdd(file.Name, file.Path)) {
                        AddProcess(file.Name);
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteButton = (Button)sender;

            // Get the process name from the encoded Name of the button
            var deletedProcessName = ((ProcessItem)deleteButton.DataContext).ProcessName;
            workspaces[chosenWorkspaceName].Remove(deletedProcessName);
            foreach (var processItem in ProcessesCollection.ToArray())
            {
                if (processItem.ProcessName == deletedProcessName)
                {
                    ProcessesCollection.Remove(processItem);
                }
            }
        }

        private void ClearProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearProcesses();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the editing tools
            isEditModeActive = false;
            AddProcessesButton.Visibility = Visibility.Collapsed;
            EditControlStackPanel.Visibility = Visibility.Collapsed;
            EditProcessesButton.Visibility = Visibility.Visible;
            ToggleProcessItemState();
            ProcessesListView.CanDragItems = false;
            ProcessesListView.CanReorderItems = false;
            ProcessesListView.AllowDrop = false;

            var processesPathList = new List<string>();
            foreach(var processItem in ProcessesCollection)
            {
                processesPathList.Add(workspaces[chosenWorkspaceName][processItem.ProcessName]);
            }

            string encodedProcessPaths = string.Join(",", processesPathList);
            List<string> workspaceProcesses = WorkspaceInfoAccess.GetProcessPaths(chosenWorkspaceName);
            if (workspaceProcesses.Count == 0)
            {
                WorkspaceInfoAccess.AddWorkspace(chosenWorkspaceName, encodedProcessPaths);
            }
            else
            {
                WorkspaceInfoAccess.UpdateWorkspace(chosenWorkspaceName, encodedProcessPaths);
            }
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
