﻿using System.Windows;
using System.Windows.Input;
using YoutubeDownloader.ViewModels.Components;

namespace YoutubeDownloader.Views.Components;

public partial class DashboardView
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void QueryTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Disable new lines when pressing enter without shift
        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
        {
            e.Handled = true;

            // We handle the event here so we have to directly "press" the default button
            AccessKeyManager.ProcessKey(null, "\x000D", false);
        }
    }

    private void QueryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {

    }
    private void SelectUploadVideoChanged(object sender, RoutedEventArgs e)
    {
        ((DashboardViewModel)this.DataContext).UpdateNumberVideoNeedToUpload();
    }
}