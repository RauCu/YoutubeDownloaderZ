using System;
using System.Diagnostics;
using Stylet;

namespace YoutubeDownloader.ViewModels.Framework;

public abstract class DialogScreen<T> : PropertyChangedBase
{
    public T? DialogResult { get; private set; }

    public event EventHandler? Closed;

    public void Close(T? dialogResult = default)
    {
        DialogResult = dialogResult;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Guide()
    {
        string url = "https://www.ganjingworld.com/video/1fmaedt4qtc6q7n9FPgwTaF9z1d01c?playlistID=1ff41rph6likfNRaA6hssga15e0p";
        Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
    }
}

public abstract class DialogScreen : DialogScreen<bool?>
{
}