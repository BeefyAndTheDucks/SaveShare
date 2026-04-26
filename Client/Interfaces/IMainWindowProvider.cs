using Avalonia.Controls;

namespace Client.Interfaces;

public interface IMainWindowProvider
{
    Window MainWindow { get; }
}