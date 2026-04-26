using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Client.Interfaces;

namespace Client;

public sealed class MainWindowProvider : IMainWindowProvider
{
    public Window MainWindow
    {
        get
        {
            if (Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is not null)
            {
                return desktop.MainWindow;
            }

            throw new InvalidOperationException("Main window is not available.");
        }
    }
}