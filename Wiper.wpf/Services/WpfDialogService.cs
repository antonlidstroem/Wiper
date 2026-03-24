using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wiper.Core.Interfaces;

namespace Wiper.wpf.Services
{
    public class WpfDialogService : IDialogService
    {
        public bool Confirm(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
