using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ALWO
{
    public class ProcessItem : INotifyPropertyChanged, ICloneable
    {
        private string _vdName;
        public string VDName
        {
            get { return _vdName; }
            set
            {
                if (_vdName != value)
                {
                    _vdName = value;
                    NotifyPropertyChanged(nameof(VDName));
                }
            }
        }
        public BitmapImage Icon { get; set; }
        public string ProcessName { get; set; }
        public string ProcessPath { get; set; }
        private int _timeInterval;
        public int TimeInterval
        {
            get { return _timeInterval; }
            set
            {
                if (_timeInterval != value)
                {
                    _timeInterval = value;
                    NotifyPropertyChanged(nameof(TimeInterval));
                }
            }
        }
        private ObservableCollection<ProcessItem> _children;
        public ObservableCollection<ProcessItem> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new ObservableCollection<ProcessItem>();
                }
                return _children;
            }
            set
            {
                _children = value;
            }
        }
        public bool IsProcess => ProcessName != null;
        private bool _isVDNameEditable;
        public bool IsVDNameEditable
        {
            get { return _isVDNameEditable; }
            set
            {
                if (_isVDNameEditable != value)
                {
                    _isVDNameEditable = value;
                    NotifyPropertyChanged(nameof(IsVDNameEditable));
                }
            }
        }
        private Visibility _editToolsVisibility;
        public Visibility EditToolsVisibility
        {
            get { return _editToolsVisibility; }
            set
            {
                if (_editToolsVisibility != value)
                {
                    _editToolsVisibility = value;
                    NotifyPropertyChanged(nameof(EditToolsVisibility));
                }
            }
        }

        public void VDNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            VDName = (sender as TextBox).Text;
        }
        public void AddButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.AddProcesses(VDName);
        }
        public void DeleteVDButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DeleteVD(VDName);
        }
        public void TimeIntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int timeInterval;
            var text = (sender as TextBox).Text;
            if (!int.TryParse(text, out timeInterval))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    TimeInterval = 1;
                }
                (sender as TextBox).Text = TimeInterval.ToString();
                return;
            }
            TimeInterval = timeInterval;
        }
        public void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DeleteProcess(VDName, ProcessName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {

            var clonedProcessItem = new ProcessItem
            {
                VDName = VDName,
                Icon = Icon,
                ProcessName = ProcessName,
                TimeInterval = TimeInterval,
                IsVDNameEditable = IsVDNameEditable,
                EditToolsVisibility = EditToolsVisibility
            };

            Children.ToList().ForEach(e => clonedProcessItem.Children.Add(e.Clone() as ProcessItem));

            return clonedProcessItem;
        }
    }
}
