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

namespace ALWO
{
    public sealed partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, string>> workspaces = new Dictionary<string, Dictionary<string, string>>();
        private string chosenWorkspaceName = "";

        public MainWindow()
        {
            this.InitializeComponent();
            FetchWorkspaces();
        }

        // Helper function to display Message Dialogs
        private async void ShowMessageDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Message",
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = MainStackPanel.XamlRoot
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
            // Add process which includes a Name and Delete Button
            ProcessNamesStackPanel.Children.Add(new TextBlock
            {
                Text = processName,
                FontSize = 20,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Assign a unique Name to the button
            ButtonBase deleteButton = new Button
            {
                Name = $"DeleteButton{processName}",
                Content = "Delete",
                Margin = new Thickness(0, 0, 0, 10)
            };
            deleteButton.Click += DeleteButton_Click;
            DeleteButtonsStackPanel.Children.Add(deleteButton);
        }

        // Clear processes from the UI
        private void ClearProcesses()
        {
            workspaces.Remove(chosenWorkspaceName);
            ProcessNamesStackPanel.Children.Clear();
            DeleteButtonsStackPanel.Children.Clear();
        }

        //Event Handlers
        private void WorkspaceNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FetchProcessesButton.Content = workspaces.Keys.Contains(WorkspaceNameTextBox.Text) ? "Fetch" : "Create";
        }

        private void FetchProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            // Ignore fetch requests from the workspace already in focus
            if (WorkspaceNameTextBox.Text == chosenWorkspaceName)
            {
                return;
            }

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
            ProcessNamesStackPanel.Visibility = workspaces[chosenWorkspaceName].Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EditProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            // Make the editing tools visible
            ProcessNamesStackPanel.Visibility = Visibility.Visible;
            AddProcessesButton.Visibility = Visibility.Visible;
            DeleteButtonsStackPanel.Visibility = Visibility.Visible;
            EditControlStackPanel.Visibility = Visibility.Visible;
            EditProcessesButton.Visibility = Visibility.Collapsed;

            // Un-Center the ProcessesNames
            ProcessNamesStackPanel.Margin = new Thickness(0, 0, 100, 0);
            foreach (TextBlock processName in ProcessNamesStackPanel.Children)
            {
                processName.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private void DeleteWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);
            ClearProcesses();
            chosenWorkspaceName = "";
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

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (workspaces[chosenWorkspaceName].Count == 0)
            {
                ShowMessageDialog("No processes to run");
                return;
            }

            foreach (var file in workspaces[chosenWorkspaceName])
            {
                try
                {
                    Process.Start(file.Value);
                }
                catch (Exception ex)
                {
                    ShowMessageDialog($"Error running {file.Key}: {ex.Message}");
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button deleteButton = sender as Button;

            // Get the process name from the encoded Name of the button
            string processName = deleteButton.Name.Substring("DeleteButton".Length);
            workspaces[chosenWorkspaceName].Remove(deleteButton.Name.Substring("DeleteButton".Length));
            foreach (TextBlock processNameTextBlock in ProcessNamesStackPanel.Children)
            {
                if(processNameTextBlock.Text == processName)
                {
                    ProcessNamesStackPanel.Children.Remove(processNameTextBlock);
                }
            }
            (deleteButton.Parent as StackPanel).Children.Remove(deleteButton);
        }

        private void ClearProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearProcesses();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the editing tools
            ProcessNamesStackPanel.Visibility = workspaces[chosenWorkspaceName].Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            AddProcessesButton.Visibility = Visibility.Collapsed;
            DeleteButtonsStackPanel.Visibility = Visibility.Collapsed;
            EditControlStackPanel.Visibility = Visibility.Collapsed;
            EditProcessesButton.Visibility = Visibility.Visible;

            // Center the ProcessesNames
            ProcessNamesStackPanel.Margin = new Thickness(0);
            foreach (TextBlock processName in ProcessNamesStackPanel.Children)
            {
                processName.HorizontalAlignment = HorizontalAlignment.Center;
            }

            string encodedProcessPaths = string.Join(",", workspaces[chosenWorkspaceName].Values);
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

        private void WorkspaceNameTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {

        }
    }
}
