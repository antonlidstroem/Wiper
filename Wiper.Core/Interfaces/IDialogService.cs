using System.Windows;

namespace Wiper.Core.Interfaces;

public interface IDialogService
{
    bool Confirm(string message, string title);
}

