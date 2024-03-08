using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
        public List<AppInfo> SelectedApps { get; set; }
        private List<AppInfo> installedApps = new List<AppInfo>();
        public ObservableCollection<AppItem> AppItemCollection { get; set; } = new ObservableCollection<AppItem>();

        public AppChooserWindow()
        {
            this.InitializeComponent();

            AppsListView.ItemsSource = AppItemCollection;

            DisplayApps();
        }

        private async void DisplayApps()
        {
            installedApps = await InstalledApps.GetInstalledApps();

            installedApps.ForEach(e => AppItemCollection.Add(new AppItem
            {
                Name = e.Name,
                Icon = e.Icon,
                Path = e.Path
            }));
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SelectedApps = AppsListView.SelectedItems
                                       .Cast<AppItem>()
                                       .Select(appItem => new AppInfo { 
                                           Name = appItem.Name, 
                                           Path = appItem.Path })
                                       .ToList();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
