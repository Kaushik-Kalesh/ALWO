using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ALWO
{
    public class WorkspaceNavigationItem : INotifyPropertyChanged
    {
        private string _workspaceName;

        public string WorkspaceName
        {
            get => _workspaceName;
            set
            {
                if (_workspaceName != value)
                {
                    _workspaceName = value;
                    OnPropertyChanged(nameof(WorkspaceName));
                }
            }
        }

        public override string ToString()
        {
            return WorkspaceName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed partial class MainWindow : Window
    {
        public static ObservableCollection<WorkspaceNavigationItem> WorkspacesCollection { get; } = new ObservableCollection<WorkspaceNavigationItem>();
        public static AppChooserWindow appChooserWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            WorkspacesNavigationView.MenuItemsSource = WorkspacesCollection;

            MainFrame.Navigate(typeof(MainPage));
            InitializeWorkspacesCollection();            
        }

        private async void InitializeWorkspacesCollection()
        {
            // Populate the NavigationView with the already exisitng workspaces
            foreach (string workspaceName in WorkspaceInfoAccess.GetWorkspaceNames())
            {
                WorkspacesCollection.Add(new WorkspaceNavigationItem { WorkspaceName = workspaceName });
            }
            if (MainFrame != null && MainFrame.Content is MainPage mainPage && WorkspacesCollection.Count > 0)
            {
                await mainPage.FetchVirtualDesktops(WorkspacesCollection.First().WorkspaceName);
            }
        }

        private async void WorkspacesNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as WorkspaceNavigationItem;
            if (MainFrame != null && MainFrame.Content is MainPage mainPage && WorkspacesCollection.Contains(selectedItem))
            {
                await mainPage.FetchVirtualDesktops(selectedItem.WorkspaceName);
            }
        }

        private async void NewWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            string newWorkspaceName = "New Workspace";
            if (MainFrame != null && MainFrame.Content is MainPage mainPage)
            {
                WorkspacesCollection.Add(new WorkspaceNavigationItem { WorkspaceName = newWorkspaceName });
                WorkspacesNavigationView.SelectedItem = WorkspacesNavigationView.MenuItems.Last(); // Change selection to the new workspace
                await mainPage.FetchVirtualDesktops(newWorkspaceName, true);
            }
            }

        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            if (MainFrame != null && MainFrame.Content is MainPage mainPage && WorkspacesCollection.Count > 0)
            {
                args.Handled = true;
                await mainPage.FetchVirtualDesktops(WorkspacesCollection.First().WorkspaceName);
                args.Handled = false;
            }

            appChooserWindow?.Close();            
            this.Close();
        }
    }
}
