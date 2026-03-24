using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Wiper.wpf.ViewModels
{
    public partial class LogViewModel : ObservableObject
    {
        public ObservableCollection<string> Logs { get; } = [];

        public void Log(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
        }

        [RelayCommand]
        public void CopyLogs()
        {
            if (Logs.Count == 0) return;
            var fullLog = string.Join(Environment.NewLine, Logs.Reverse());
            Clipboard.SetText(fullLog);
            Log("System: Loggen har kopierats till urklipp.");
        }

        [RelayCommand]
        public void ClearLogs() => Logs.Clear();
    }
}
