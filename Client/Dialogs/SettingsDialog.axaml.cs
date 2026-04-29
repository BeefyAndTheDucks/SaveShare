using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Client.Interfaces;
using Common;
using CommunityToolkit.Mvvm.Input;

namespace Client.Dialogs;

public partial class SettingsDialog : Window
{
    private readonly ISettingsStore _settingsStore;

    public SettingsDialog()
    {
        InitializeComponent();
        DataContext = this;
        
        _settingsStore = null!;
    }
    
    public SettingsDialog(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        
        InitializeComponent();

        DataContext = this;
        
        LoadSettings().Forget();
    }

    private async Task LoadSettings()
    {
        AppSettings? currentSettings = await _settingsStore.LoadAsync();
        
        if (currentSettings is not null)
            ServerUrlTextBox.Text = currentSettings.ServerUri.ToString();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveAndRestartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SaveAndRestartAsync().Forget();
    }

    [RelayCommand]
    private async Task SaveAndRestartAsync()
    {
        AppSettings newSettings = new(new Uri(ServerUrlTextBox.Text ?? ""));
        await _settingsStore.SaveAsync(newSettings);
        App.Restart();
    }
}