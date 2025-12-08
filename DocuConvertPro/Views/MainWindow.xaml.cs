using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DocuConvertProRefactored.Services;

namespace DocuConvertProRefactored.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 订阅日志列表的CollectionChanged事件
            Loaded += MainWindow_Loaded;
            
            // 添加Ctrl+C快捷键处理
            KeyBinding copyKeyBinding = new KeyBinding(
                new RelayCommand(CopySelectedLogs), 
                new KeyGesture(Key.C, ModifierKeys.Control));
            LogListBox.InputBindings.Add(copyKeyBinding);
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                // 订阅日志消息集合变化事件
                viewModel.LogMessages.CollectionChanged += LogMessages_CollectionChanged;
            }
        }
        
        private void LogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 当添加新项时，滚动到最后一项
            if (e.Action == NotifyCollectionChangedAction.Add && LogListBox.Items.Count > 0)
            {
                // 滚动到最后一项
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            }
        }
        
        // 复制选中的日志到剪贴板
        private void CopySelectedLogs()
        {
            try
            {
                if (LogListBox.SelectedItems.Count > 0)
                {
                    // 将选中的日志消息合并为一个字符串
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (var item in LogListBox.SelectedItems)
                    {
                        if (item is LogMessage logMessage)
                        {
                            sb.AppendLine($"[{logMessage.Timestamp:HH:mm:ss}] {logMessage.Level.ToString().ToUpper()}: {logMessage.Message}");
                        }
                    }
                    
                    // 将日志内容复制到剪贴板
                    Clipboard.SetText(sb.ToString());
                }
            }
            catch (Exception)
            {
                // 忽略复制错误
            }
        }
        
        // 处理预览拖过事件
        private void InputFilePathTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        // 处理放置事件
        private void InputFilePathTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && DataContext is ViewModels.MainWindowViewModel viewModel)
                {
                    viewModel.InputFilePath = files[0];
                    // 输出目录和日志显示的逻辑已移到InputFilePath属性的setter中
                }
            }
        }
        
        // 简单的RelayCommand实现
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            
            public RelayCommand(Action execute)
            {
                _execute = execute;
            }
            
            // 将CanExecuteChanged事件与CommandManager.RequerySuggested事件关联，以消除编译警告
            public event EventHandler? CanExecuteChanged
            {
                add { System.Windows.Input.CommandManager.RequerySuggested += value; }
                remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
            }
            
            public bool CanExecute(object? parameter)
            {
                return true;
            }
            
            public void Execute(object? parameter)
            {
                _execute();
            }
        }

    }

    // BoolToTextConverter实现
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // BoolToVisibilityConverter实现
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    // StringToBoolConverter实现
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string param)
            {
                return string.Equals(stringValue, param, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter is string param)
            {
                return param;
            }
            return Binding.DoNothing;
        }
    }
}