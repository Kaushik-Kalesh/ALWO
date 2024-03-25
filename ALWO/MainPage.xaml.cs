using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WindowsDesktop;

namespace ALWO
{
    public class ProcessItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate VDTemplate { get; set; }
        public DataTemplate ProcessTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return (item as ProcessItem).IsProcess ? ProcessTemplate : VDTemplate;
        }
    }

    public sealed partial class MainPage : Page
    {
        public static ObservableCollection<ProcessItem> VirtualDesktops { get; set; } = new ObservableCollection<ProcessItem>();
        public static ObservableCollection<ProcessItem> BackupVirtualDesktops { get; set; } = new ObservableCollection<ProcessItem>();

        private static List<AppInfo> installedApps = new List<AppInfo>();
        private string chosenWorkspaceName = string.Empty;
        private bool isEditModeEnabled = false;
        private static string chosenVDName = string.Empty;

        public MainPage()
        {
            this.InitializeComponent();

            VDTreeView.ItemsSource = VirtualDesktops;
        }

        // Helper functions
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

        private async Task SaveAlert()
        {
            if (isEditModeEnabled)
            {
                await ShowConfirmationDialog("Save Changes?", SaveChanges, CancelChanges);
            }
        }

        private void ToggleEditMode(bool isActive)
        {
            isEditModeEnabled = isActive;
            foreach (ProcessItem vd in VirtualDesktops)
            {
                vd.IsVDNameEditable = isActive;
                vd.EditToolsVisibility = isActive ? Visibility.Visible : Visibility.Collapsed;

                vd.Children.ToList().ForEach(e => e.EditToolsVisibility = isActive ? Visibility.Visible : Visibility.Collapsed);
            }
            foreach (ProcessItem vd in BackupVirtualDesktops)
            {
                vd.IsVDNameEditable = isActive;
                vd.EditToolsVisibility = isActive ? Visibility.Visible : Visibility.Collapsed;

                vd.Children.ToList().ForEach(e => e.EditToolsVisibility = isActive ? Visibility.Visible : Visibility.Collapsed);
            }

            EditButton.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
            WorkspaceNameTextBox.IsEnabled = isActive;
            DeleteWorkspaceButton.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            EditToolsStackPanel.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;            

            if (isActive)
            {
                BackupVirtualDesktops.Clear();
                VirtualDesktops.ToList().ForEach(e => BackupVirtualDesktops.Add(e.Clone() as ProcessItem));
            }
            else
            {
                if (!string.IsNullOrEmpty(chosenVDName))
                {
                    MainWindow.appChooserWindow.Close();
                }
            }
        }


        private static void NewVirtualDesktop(string name = "New Desktop")
        {
            VirtualDesktops.Add(new ProcessItem
            {
                VDName = name,
                TimeInterval = 1,
                IsVDNameEditable = true,
                EditToolsVisibility = Visibility.Visible
            });
        }

        private static VirtualDesktop GetVirtualDesktop(string vdName)
        {
            return VirtualDesktop.GetDesktops().FirstOrDefault(e => e.Name == vdName);
        }

        private void ClearVirtualDesktops()
        {
            VirtualDesktops.Clear();
            BackupVirtualDesktops.Clear();
        }

        private async void DeleteWorkspace()
        {
            ToggleEditMode(false);

            WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);
            // Delete the workspace from the NavigationView
            MainWindow.WorkspacesCollection.Remove(MainWindow.WorkspacesCollection.FirstOrDefault(e => e.WorkspaceName == chosenWorkspaceName));
            ClearVirtualDesktops();

            if (MainWindow.WorkspacesCollection.Count > 0)
            {
                await FetchVirtualDesktops(MainWindow.WorkspacesCollection.Last().WorkspaceName);
            }
            else
            {
                // If no workspaces are left, update the UI
                WorkspaceNameTextBox.Text = chosenWorkspaceName = string.Empty;
                WorkspaceNameTextBox.Visibility = Visibility.Collapsed;
                RunButton.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteWorkspaceButton.Visibility = Visibility.Collapsed;
                EditToolsStackPanel.Visibility = Visibility.Collapsed;
                NoWorkspacesTextBlock.Visibility = Visibility.Visible;
                VirtualDesktopsStackPanel.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        public static void DeleteVD(string vdName)
        {
            if (VirtualDesktop.GetDesktops().Count() == 1)
            {
                NewVirtualDesktop();
            }

            VirtualDesktops.Remove(VirtualDesktops.FirstOrDefault(e => e.VDName == vdName));
        }

        public static void DeleteProcess(string VDName, string ProcesName)
        {
            ObservableCollection<ProcessItem> processes = VirtualDesktops.FirstOrDefault(e => e.VDName == VDName).Children;
            processes.Remove(processes.FirstOrDefault(e => e.ProcessName == ProcesName));
        }

        public static void AddProcesses(string vdName)
        {
            if (!string.IsNullOrEmpty(chosenVDName)) { return; }

            MainWindow.appChooserWindow = new AppChooserWindow(installedApps);
            MainWindow.appChooserWindow.Activate();            
            chosenVDName = vdName;
            MainWindow.appChooserWindow.Closed += AppChooserWindow_Closed;
        }

        private static void AppChooserWindow_Closed(object sender, WindowEventArgs args)
        {
            AppChooserWindow acw = sender as AppChooserWindow;
            ProcessItem vd = VirtualDesktops.FirstOrDefault(e => e.VDName == chosenVDName);

            acw.SelectedApps.ForEach(e => vd.Children.Add(new ProcessItem
            {
                VDName = chosenVDName,
                Icon = e.Icon,
                ProcessName = e.Name,
                ProcessPath = e.Path,                
                IsVDNameEditable = true,
                EditToolsVisibility = Visibility.Visible
            }));

            installedApps = acw.InstalledApps;

            chosenVDName = string.Empty;
        }

        private void SaveChanges()
        {
            if (WorkspaceInfoAccess.GetWorkspaceNames().Contains(WorkspaceNameTextBox.Text) && WorkspaceNameTextBox.Text != chosenWorkspaceName)
            {
                ShowMessageDialog("Workspace Name already used");
                return;
            }           

            ToggleEditMode(false);

            foreach (var vd in VirtualDesktops)
            {
                if (!BackupVirtualDesktops.Select(e => e.VDName).ToList().Contains(vd.VDName))
                {
                    VirtualDesktop newVirtualDesktop = VirtualDesktop.Create();
                    newVirtualDesktop.Name = vd.VDName;
                }
            }
            foreach (var vd in BackupVirtualDesktops)
            {
                if (!VirtualDesktops.Select(e => e.VDName).ToList().Contains(vd.VDName))
                {
                    GetVirtualDesktop(vd.VDName).Remove();
                }
            }

            MainWindow.WorkspacesCollection.Remove(MainWindow.WorkspacesCollection.FirstOrDefault(e => e.WorkspaceName == chosenWorkspaceName));
            WorkspaceInfoAccess.DeleteWorkspace(chosenWorkspaceName);

            chosenWorkspaceName = WorkspaceNameTextBox.Text;
            MainWindow.WorkspacesCollection.Add(new WorkspaceNavigationItem { WorkspaceName = chosenWorkspaceName });
            WorkspaceInfoAccess.UpdateWorkspace(chosenWorkspaceName, VirtualDesktops);
        }

        private void CancelChanges()
        {
            ToggleEditMode(false);

            VirtualDesktops.Clear();
            BackupVirtualDesktops.ToList().ForEach(e => VirtualDesktops.Add(e.Clone() as ProcessItem));
        }

        public async Task FetchVirtualDesktops(string workspaceName, bool isNewWorkspace = false)
        {
            await SaveAlert();

            // Ignore fetch requests from the workspace already in focus
            if (workspaceName == chosenWorkspaceName) { return; }
            
            ClearVirtualDesktops();

            WorkspaceNameTextBox.Visibility = Visibility.Visible;
            WorkspaceNameTextBox.Text = chosenWorkspaceName = workspaceName;

            WorkspaceInfoAccess.GetVirtualDesktops(workspaceName).ToList().ForEach(e => VirtualDesktops.Add(e));            

            RunButton.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
            DeleteWorkspaceButton.Visibility = Visibility.Collapsed;
            EditToolsStackPanel.Visibility = Visibility.Collapsed;
            NoWorkspacesTextBlock.Visibility = Visibility.Collapsed;
            VirtualDesktopsStackPanel.VerticalAlignment = VerticalAlignment.Top;

            if (isNewWorkspace)
            {
                ToggleEditMode(true); // Open new workspace in edit mode                
            }
        }

        //Event Handlers
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode(true);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveAlert();

            foreach (var vd in VirtualDesktops)
            {
                if(vd.Children.Count == 0) { continue; }

                GetVirtualDesktop(vd.VDName).Switch();
                foreach (var process in vd.Children)
                {
                    try
                    {
                        Process.Start(process.ProcessPath);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageDialog($"Error running {process.ProcessName}: {ex.Message}");
                    }
                }
                System.Threading.Thread.Sleep(vd.TimeInterval * 1000); // To ensure delay between virtual desktop switches
            }
        }

        private async void DeleteWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowConfirmationDialog("Are you sure?", DeleteWorkspace);
        }

        private void NewDesktopButton_Click(object sender, RoutedEventArgs e)
        {
            NewVirtualDesktop();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelChanges();
        }
    }
}
