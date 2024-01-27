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
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Dictionary<string, StorageFile> processes = new Dictionary<string, StorageFile>();

        public MainWindow()
        {
            this.InitializeComponent();
        }

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

        private void AddProcess(StorageFile file)
        {
            if (processes.TryAdd(file.Name, file))
            {
                // Add process which includes a Name and Delete Button
                ProcessNamesStackPanel.Children.Add(new TextBlock
                {
                    Text = file.Name,
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 15)
                });

                // Assign a unique Name to the button
                ButtonBase deleteButton = new Button
                {
                    Name = $"DeleteButton{file.Name}",
                    Content = "Delete",
                    Margin = new Thickness(0, 0, 0, 10)
                };
                deleteButton.Click += DeleteButton_Click;
                DeleteButtonsStackPanel.Children.Add(deleteButton);
            }
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
                    AddProcess(file);
                }
            }
        }

        private void EditProcessesButton_Click (object sender, RoutedEventArgs e)
        {
            ProcessesStackPanel.Visibility = Visibility.Visible;
            AddProcessesButton.Visibility = Visibility.Visible;
            DeleteButtonsStackPanel.Visibility = Visibility.Visible;
            EditControlStackPanel.Visibility = Visibility.Visible;
            EditProcessesButton.Visibility = Visibility.Collapsed;

            ProcessNamesStackPanel.Margin = new Thickness(0, 0, 100, 0);
            foreach(TextBlock processName in ProcessNamesStackPanel.Children)
            {
                processName.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if(processes.Count == 0)
            {
                ShowMessageDialog("No processes to run");
                return;
            }
            foreach (StorageFile file in processes.Values)
            {
                try
                {
                    Process.Start(file.Path);
                }
                catch (Exception ex)
                {
                    ShowMessageDialog($"Error running {file.Name}: {ex.Message}");
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button deleteButton = sender as Button;

            // Get the process name from the encoded Name of the button
            string processName = deleteButton.Name.Substring("DeleteButton".Length);
            processes.Remove(deleteButton.Name.Substring("DeleteButton".Length));
            foreach(TextBlock processNameTextBlock in ProcessNamesStackPanel.Children)
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
            processes.Clear();
            ProcessNamesStackPanel.Children.Clear();
            DeleteButtonsStackPanel.Children.Clear();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessesStackPanel.Visibility = processes.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            AddProcessesButton.Visibility = Visibility.Collapsed;
            DeleteButtonsStackPanel.Visibility = Visibility.Collapsed;
            EditControlStackPanel.Visibility = Visibility.Collapsed;
            EditProcessesButton.Visibility = Visibility.Visible;

            ProcessNamesStackPanel.Margin = new Thickness(0);
            foreach (TextBlock processName in ProcessNamesStackPanel.Children)
            {
                processName.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }
    }
}
