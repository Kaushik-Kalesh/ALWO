using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ALWO
{
    public class AppItem
    {
        public BitmapImage Icon { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public sealed partial class AppChooserWindow : Window
    {
        public List<AppInfo> SelectedApps { get; set; } = new List<AppInfo>();
        private List<AppInfo> installedApps = new List<AppInfo>();
        public ObservableCollection<AppItem> AppItemCollection { get; set; } = new ObservableCollection<AppItem>();

        public AppChooserWindow()
        {
            this.InitializeComponent();
            
            AppsListView.ItemsSource = AppItemCollection;

            DisplayAllApps();
        }

        private async void DisplayAllApps()
        {
            if (installedApps.Count == 0)
            {
                LoadingStackPanel.Visibility = Visibility.Visible;
                DoneButton.Visibility = Visibility.Collapsed;
                installedApps = await InstalledApps.GetInstalledApps();
                LoadingStackPanel.Visibility = Visibility.Collapsed;
                DoneButton.Visibility = Visibility.Visible;
            }

            installedApps.ForEach(e => AppItemCollection.Add(new AppItem
                                       {
                                           Name = e.Name,
                                           Icon = e.Icon,
                                           Path = e.Path,
                                       }));
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SelectedApps = SelectedApps.DistinctBy(e => e.Name).ToList();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AppNameAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ManualSearchStackPanel.Visibility = Visibility.Collapsed;
            DoneButton.Visibility = Visibility.Visible;

            AppItemCollection.Clear();

            string appName = sender.Text.ToLower();
            if (string.IsNullOrWhiteSpace(appName))
            {
                DisplayAllApps();
                return;
            }

            installedApps.Where(e => e.Name.ToLower().Contains(appName)).ToList().ForEach(e => AppItemCollection.Add(new AppItem
            {
                Icon = e.Icon,
                Name = e.Name,
                Path = e.Path
            })); 

            if (AppItemCollection.Count == 0)
            {
                ManualSearchStackPanel.Visibility = Visibility.Visible;
                DoneButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AppsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.AddedItems.ToList().ForEach(e => SelectedApps.Add(new AppInfo
            {
                Name = (e as AppItem).Name,
                Path = (e as AppItem).Path
            }));
        }

        private async void ManualSearchButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();

            // Retrieve the window handle (HWND) of the current window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add(".exe");

            // Open the picker for the user to pick a file
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (StorageFile file in files)
                {
                    SelectedApps.Add(new AppInfo
                    {
                        Name = file.Name,
                        Path = file.Path
                    });
                }
            }

            DoneButton.Visibility = Visibility.Visible;
        }
    }
}
