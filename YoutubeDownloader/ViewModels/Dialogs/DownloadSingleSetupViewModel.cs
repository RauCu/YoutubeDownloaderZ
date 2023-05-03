using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Utils;
using YoutubeDownloader.Services;
using YoutubeDownloader.Utils;
using YoutubeDownloader.ViewModels.Components;
using YoutubeDownloader.ViewModels.Framework;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.ViewModels.Dialogs;

public class DownloadSingleSetupViewModel : DialogScreen<DownloadViewModel>
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly SettingsService _settingsService;

    public IVideo? Video { get; set; }

    public IReadOnlyList<VideoDownloadOption>? AvailableDownloadOptions { get; set; }

    public VideoDownloadOption? SelectedDownloadOption { get; set; }

    public DownloadSingleSetupViewModel(
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        SettingsService settingsService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _settingsService = settingsService;
    }

    public void OnViewLoaded()
    {
        SelectedDownloadOption = AvailableDownloadOptions?.FirstOrDefault(o =>
            o.Container == _settingsService.LastContainer
        );
    }

    public void CopyTitle() => Clipboard.SetText(Video!.Title);

    public void ConfirmPath(String dirPath)
    {
        var container  = Container.Mp4;

        Database.Load(dirPath);
        VideoInfo? videoInfo = Database.Find(Http.getVideoID(Video));
        int number;
        if (videoInfo == null)
        {
            Database.InsertOrUpdate(new VideoInfo(0, Video!.Title, Http.getVideoID(Video), "", "", Video!.Url));
            number = Database.Count();
        }
        else
        {
            // already exist, get it
            number = videoInfo.Number;
        }

        var filePath = Path.Combine(
                dirPath,
                FileNameTemplate.Apply(
                    _settingsService.FileNameTemplate,
                    Video!,
                    container,
                    (number).ToString().PadLeft(YoutubeDownloader.Utils.AppConsts.LenNumber, '0')
                ));

        _settingsService.LastContainer = container;

        Close(
            _viewModelFactory.CreateDownloadViewModel(Video!, SelectedDownloadOption!, filePath)
        );
        Database.Save();
    }
    public void Confirm()
    {
        var dirPath = _dialogManager.PromptDirectoryPath();
        if (string.IsNullOrWhiteSpace(dirPath))
            return;
        ConfirmPath(dirPath);
    }
}


public static class DownloadSingleSetupViewModelExtensions
{
    public static DownloadSingleSetupViewModel CreateDownloadSingleSetupViewModel(
        this IViewModelFactory factory,
        IVideo video,
        IReadOnlyList<VideoDownloadOption> availableDownloadOptions)
    {
        var viewModel = factory.CreateDownloadSingleSetupViewModel();

        viewModel.Video = video;
        viewModel.AvailableDownloadOptions = availableDownloadOptions;

        return viewModel;
    }
}