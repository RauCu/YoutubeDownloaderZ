using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using Gress.Completable;
using OpenQA.Selenium;
using Stylet;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Resolving;
using YoutubeDownloader.Core.Tagging;
using YoutubeDownloader.Services;
using YoutubeDownloader.Utils;
using YoutubeDownloader.Core.Utils;
using YoutubeDownloader.ViewModels.Dialogs;
using YoutubeDownloader.ViewModels.Framework;
using YoutubeExplode.Exceptions;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Windows;

namespace YoutubeDownloader.ViewModels.Components;

public class DashboardViewModel : PropertyChangedBase, IDisposable
{
    private bool allItemsAreChecked;
    public event PropertyChangedEventHandler PropertyChanged;

    public bool AllItemsAreChecked
    {
        get
        {
            return this.allItemsAreChecked;
        }
        set
        {
            this.allItemsAreChecked = value;
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs("AllItemsAreChecked"));
            }

            int count = 0;
            foreach (var download in Downloads.ToArray())
            {
                if (download.CanShowFile)
                {
                    download.SelectedToUpload = value;
                    if (value)
                    {
                        count++;
                    }
                }
            }
            this.NumberVideoNeedToUpload = count;
        }
    }
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;
    private readonly SettingsService _settingsService;

    private readonly AutoResetProgressMuxer _progressMuxer;
    private readonly ResizableSemaphore _downloadSemaphore = new();

    private readonly QueryResolver _queryResolver = new();
    private readonly VideoDownloader _videoDownloader = new();
    private readonly MediaTagInjector _mediaTagInjector = new();

    private static Mutex mut = new Mutex();
    public bool IsBusy { get; private set; }
    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => IsBusy && Progress.Current.Fraction is <= 0 or >= 1;

    public string? Query { get; set; }

    public BindableCollection<DownloadViewModel> Downloads { get; } = new();

    private int p_NumberVideoNeedToUpload;

    public int NumberVideoNeedToUpload
    {
        get { return p_NumberVideoNeedToUpload; }

        set
        {
            p_NumberVideoNeedToUpload = value;
            base.NotifyOfPropertyChange("NumberVideoNeedToUpload");
        }
    }

    void OnDownloadListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateNumberVideoNeedToUpload();
    }


    public int UpdateNumberVideoNeedToUpload()
    {
        int count = 0;
        foreach (var download in Downloads.ToArray())
        {
            if (download.CanShowFile)
            {
                if (download.SelectedToUpload)
                {
                    count++;
                }
            }
        }
        this.NumberVideoNeedToUpload = count;
        return count;
    }

    public bool IsDownloadsAvailable => Downloads.Any();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public DashboardViewModel(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        IViewModelFactory viewModelFactory,
        DialogManager dialogManager,
        SettingsService settingsService)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;
        _settingsService = settingsService;

        _progressMuxer = Progress.CreateMuxer().WithAutoReset();

        _settingsService.BindAndInvoke(o => o.ParallelLimit, (_, e) => _downloadSemaphore.MaxCount = e.NewValue);
        Progress.Bind(o => o.Current, (_, _) => NotifyOfPropertyChange(() => IsProgressIndeterminate));
        Downloads.Bind(o => o.Count, (_, _) => NotifyOfPropertyChange(() => IsDownloadsAvailable));

        // Subscribe to CollectionChanged event
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        Downloads.CollectionChanged += OnDownloadListChanged;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
    }

    public bool CanShowSettings => !IsBusy;
    public async void ShowSettings() => await _dialogManager.ShowDialogAsync(
        _viewModelFactory.CreateSettingsViewModel()
    );
    public void Help()
    {
        string url = "https://www.ganjing.com/channel/1fckkvjkqh72FQ5r2Fvprfj6q1lg0c/playlist/1ff41rph6likfNRaA6hssga15e0p";
        // System.Diagnostics.Process.Start(url);
        Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
        // IWebDriver driver = Http.GetDriver();
        // driver.Navigate().GoToUrl(url);
    }
    public void UploadVideo()
    {
        string url = "https://studio.ganjing.com/";
        IWebDriver driver = Http.GetDriver();
        driver.Navigate().GoToUrl(url);
    }

    public async void UploadMultipleVideo()
    {
        IWebDriver? driver = null;
        // reset previous upload status
        foreach (var download in Downloads.ToArray())
        {
            download.UploadDone = false;
            download.UploadError = false;
        }
        //
        bool allSuccess = true;
        bool login_success = false;
        foreach (var download in Downloads.ToArray())
        {
            if (download.CanShowFile && download.SelectedToUpload)
            {
                if (driver == null)
                {
                    driver = download.SignInGJW(out login_success);
                }

                if (login_success)
                {
                    bool errorOccur = false;
                    try
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        download.UploadOnly(driver);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    catch (Exception ex)
                    {
                        errorOccur = true;
                        // await _dialogManager.ShowDialogAsync(
                        //     _viewModelFactory.CreateMessageBoxViewModel("Lỗi", "Đăng video lỗi: " + download.FileNameShort + "\n\n\n" + ex.Message)
                        // );

                        System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                        if (driver != null)
                        {
                            currentSize = driver.Manage().Window.Size;
                            driver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                        }

                        string msg = "Đăng video lỗi, đang lỗi ở video: " + download.FileNameShort + "\n\n\nBạn có thể đăng video bị lỗi này thủ công," +
                        "\nbằng cách dùng các nút 3-4-5 để sao chép các thông tin như tiêu đề, đường dẫn video, hình, rồi dán vào trình duyệt!\n\n\n" + ex.Message;

                        MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                            "Lỗi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Question);

                        if (confirm == MessageBoxResult.OK)
                        {
                            if (driver != null)
                            {
                                driver.Manage().Window.Size = currentSize;
                            }
                        }

                        // stop upload
                        allSuccess = false;
                        break;
                    }
                    finally
                    {
                        if (errorOccur == false)
                        {
                            download.UploadDone = true;
                            download.SelectedToUpload = false;
                        }
                        else
                        {
                            download.UploadError = true;
                        }
                    }
                }
                else
                {
                    // await _dialogManager.ShowDialogAsync(
                    //     _viewModelFactory.CreateMessageBoxViewModel("Đăng nhập tự động thất bại.", "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!")
                    // );
                    System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                    if (driver != null)
                    {
                        currentSize = driver.Manage().Window.Size;
                        driver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                    }

                    string msg = "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!";

                    MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                        "Đăng nhập tự động thất bại.",
                        MessageBoxButton.OK,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.OK)
                    {
                        if (driver != null)
                        {
                            driver.Manage().Window.Size = currentSize;
                        }
                    }
                    allSuccess = false;
                    // stop upload
                    break;
                }
            }
        }
        if (driver != null && allSuccess)
        {
            Thread.Sleep(5000);
            while (true)
            {
                string pageSrc = driver.PageSource;
                if (!isUploading(pageSrc))
                {
                    System.Drawing.Size currentSize = driver.Manage().Window.Size;
                    driver.Manage().Window.Size = new System.Drawing.Size(480, 320);

                    string msg = "";
                    if (isTranscoding(pageSrc))
                    {
                        msg = "Đang Transcoding ... ";
                    }
                    msg += "Có thể tắt trình duyệt được rồi! Đồng ý tắt?";
                    MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                        "Đã Upload xong video!",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        driver.Close();
                        driver = null;
                    }
                    else
                    {
                        driver.Manage().Window.Size = currentSize;
                    }
                    break;
                }
                Thread.Sleep(3000);
            }
        }
    }

    private bool isUploading(string text)
    {
        bool result = false;
        for (int i = 0; i <= 100; i++)
        {
            string t = "Uploading " + i + "%";
            if (text.Contains(t))
            {
                result = true;
                break;
            }
        }
        return result;
    }

    private bool isTranscoding(string text)
    {
        bool result = false;
        for (int i = 0; i <= 100; i++)
        {
            string t = "Transcoding " + i + "%";
            if (text.Contains(t))
            {
                result = true;
                break;
            }
        }
        return result;
    }

    private void EnqueueDownload(DownloadViewModel download, int position = 0)
    {
        var progress = _progressMuxer.CreateInput();

        Task.Run(async () =>
        {
            bool shouldDownload = false;
            VideoInfo? videoInfo = null;
            try
            {
                using var access = await _downloadSemaphore.AcquireAsync(download.CancellationToken);
                // get status of video from cache
                videoInfo = Database.Find(download.Video!.Id);
                if (!download.ForceDownload)
                {
                    if (videoInfo != null && videoInfo.DownloadStatus != "")
                    {
                        string status = videoInfo.DownloadStatus;
                        if (status.Equals(DownloadStatus.Completed.ToString()) ||
                            (status.Equals(DownloadStatus.Completed_Already.ToString())))
                        {
                            if (!File.Exists(download.FilePath))
                            {
                                string? path = Path.GetDirectoryName(download.FilePath!);
                                string[] files = System.IO.Directory.GetFiles(path!, "*" + download.Video!.Id + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
                                if (files.Length == 0)
                                {
                                    // video deleted from FileExplorer
                                    download.Status = DownloadStatus.Deleted;
                                }
                                else
                                {  // renamed
                                    download.Status = DownloadStatus.Completed_Already;
                                }
                            }
                            else
                            {
                                download.Status = DownloadStatus.Completed_Already;
                            }
                            //throw new Exception(status);
                        }
                        else if (status.Equals(DownloadStatus.Deleted.ToString()))
                        {
                            //throw new Exception(status);
                            download.Status = DownloadStatus.Deleted;
                        }
                        else if (status.Equals(DownloadStatus.Failed.ToString()))
                        {
                            //throw new Exception(status);
                            shouldDownload = true;
                        }
                        else if (status.Equals(DownloadStatus.Canceled.ToString()))
                        {
                            //throw new Exception(status);
                            download.Status = DownloadStatus.Canceled;
                        }
                        else if (status.Equals(DownloadStatus.Canceled_low_quality.ToString()))
                        {
                            //throw new Exception(status);
                            download.Status = DownloadStatus.Canceled_low_quality;
                        }
                        else if (status.Equals(DownloadStatus.Canceled_too_short.ToString()))
                        {
                            //throw new Exception(status);
                            download.Status = DownloadStatus.Canceled_too_short;
                        }
                        else if (status.Equals(DownloadStatus.Canceled_too_long.ToString()))
                        {
                            //throw new Exception(status);
                            download.Status = DownloadStatus.Canceled_too_long;
                        }
                    }
                    else
                    {
                        shouldDownload = true;
                    }
                }
                else
                {
                    shouldDownload = true;
                }

                if (shouldDownload)
                {
                    // continue checking
                    shouldDownload = false;
                    download.Status = DownloadStatus.Started;
                    var downloadOption = download.DownloadOption is not null ?
                        download.DownloadOption :
                        await _videoDownloader.GetBestDownloadOptionAsync(
                            download.Video!.Id,
                            download.DownloadPreference!,
                            download.CancellationToken
                        );
                    int? quality = downloadOption.VideoQuality?.MaxHeight;
                    bool hightQualityVideoDownloaded = false;
                    bool tooShort = false;
                    bool tooLong = false;
                    if (download.DownloadPreference is null || ((downloadOption.Container == YoutubeExplode.Videos.Streams.Container.WebM ||
                        downloadOption.Container == YoutubeExplode.Videos.Streams.Container.Mp4) &&
                        (download.DownloadPreference!.PreferredVideoQuality == VideoQualityPreference.Highest ||
                        download.DownloadPreference!.PreferredVideoQuality == VideoQualityPreference.UpTo1080p) &&
                        quality >= 720))
                    {
                        hightQualityVideoDownloaded = true;
                    }

                    bool lowQualityVideoDownloaded = false;
                    if (download.DownloadPreference is null || ((downloadOption.Container == YoutubeExplode.Videos.Streams.Container.WebM ||
                        downloadOption.Container == YoutubeExplode.Videos.Streams.Container.Mp4) &&
                        (download.DownloadPreference!.PreferredVideoQuality == VideoQualityPreference.UpTo720p ||
                        download.DownloadPreference!.PreferredVideoQuality == VideoQualityPreference.UpTo480p)))
                    {
                        lowQualityVideoDownloaded = true;
                    }

                    bool audioDownloaded = false;
                    if (downloadOption.Container.Name == YoutubeExplode.Videos.Streams.Container.Mp3.Name ||
                        downloadOption.Container.Name == "ogg" ||
                        downloadOption.Container.Name == "m4a")
                    {
                        audioDownloaded = true;
                    }
                    int SECONDS_IN_MINUTE = 60;

                    if (download.Video!.Duration?.TotalSeconds <= SECONDS_IN_MINUTE)
                    {
                        //shorter than 1 minute
                        // workaround to disable < 1 minute
                        tooShort = false;
                    }
                    else if (download.Video!.Duration?.TotalSeconds > 60 * SECONDS_IN_MINUTE)
                    {
                        // longer than 60 minutes
                        tooLong = true;
                    }

                    bool okLength = !tooShort && !tooLong;

                    shouldDownload = (hightQualityVideoDownloaded && okLength) || (lowQualityVideoDownloaded && okLength) || (audioDownloaded && okLength) || download.ForceDownload;

                    if (shouldDownload)
                    {
                        Console.WriteLine("Accepted!");
                        //
                        DirectoryEx.CreateDirectoryForFile(download.FilePath!);
                        File.WriteAllBytes(download.FilePath!, Array.Empty<byte>());
                        await _videoDownloader.DownloadVideoAsync(
                            download.FilePath!,
                            download.Video!,
                            downloadOption,
                            download.Progress.Merge(progress),
                            download.CancellationToken
                        );

                        if (_settingsService.ShouldInjectTags)
                        {
                            try
                            {
                                await _mediaTagInjector.InjectTagsAsync(
                                    download.FilePath!,
                                    download.Video!,
                                    download.CancellationToken
                                );
                            }
                            catch
                            {
                                // Media tagging is not critical
                            }
                        }

                        // download banner & avatar channel if need
                        var dirPath = Path.GetDirectoryName(download.FilePath!);
                        DownloadAvatar(dirPath);
                        DownloadBanner(dirPath);
                        download.Status = DownloadStatus.Completed;
                    }
                    else
                    {
                        if (okLength)
                        {
                            Console.WriteLine("Canceled_low_quality");
                            //throw new Exception("Canceled_low_quality");
                            download.Status = DownloadStatus.Canceled_low_quality;
                        }
                        else if (tooShort)
                        {
                            Console.WriteLine("Canceled_too_short");
                            //throw new Exception("Canceled_too_short");
                            download.Status = DownloadStatus.Canceled_too_short;
                        }
                        else if (tooLong)
                        {
                            Console.WriteLine("Canceled_too_long");
                            //throw new Exception("Canceled_too_long");
                            download.Status = DownloadStatus.Canceled_too_long;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                download.Status = ex is OperationCanceledException
                ? DownloadStatus.Canceled
                : DownloadStatus.Failed;

                // Short error message for YouTube-related errors, full for others
                download.ErrorMessage = ex is YoutubeExplodeException
                    ? ex.Message
                    : ex.ToString();
            }
            finally
            {
                if (videoInfo != null)
                {
                    string currentStatus = download.Status.ToString();
                    if (!currentStatus.Equals(videoInfo.DownloadStatus))
                    {
                        // status changed, then Database need updated
                        videoInfo.DownloadStatus = download.Status.ToString();
                        // make sure only 1 thread can access Database at the same time
                        mut.WaitOne();
                        Database.InsertOrUpdate(videoInfo);
                        // just work-around to optimize: if too much videos are deleted, then GUI is very slow
                        if (Database.Count() < 200 || !videoInfo.DownloadStatus.Equals(DownloadStatus.Deleted.ToString()))
                        {
                            Database.Save();
                        }
                        mut.ReleaseMutex();
                    }
                }
                progress.ReportCompletion();
                download.Dispose();
            }
        });
        Downloads.Insert(position, download);
    }

    private static void DownloadAvatar(string? dirPath)
    {
        if (YTChannelPaser.GetInstance().Channel != null)
        {
            string dst = dirPath + "/[0000]-[Avatar-Kenh]-[" +
            YoutubeDownloader.Core.Utils.PathEx.EscapeFileName(YTChannelPaser.GetInstance().Channel!.Name) + "].jpg";
            if (!YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded &&
                !File.Exists(dst))
            {
                if (YTChannelPaser.GetInstance().Channel!.Avatar == "")
                {
                    if (!YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded)
                    {
                        try
                        {
                            string src = Directory.GetCurrentDirectory() + "/avatar_trang.jpg";
                            File.Copy(src, dst);
                            YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    using (WebClient webClient = new WebClient())
                    {
                        bool downloadSuccess = true;
                        byte[] dataArr = new byte[1];
                        try
                        {
                            dataArr = webClient.DownloadData(YTChannelPaser.GetInstance().Channel!.Avatar);
                        }
                        catch (Exception)
                        {
                        }
                        mut.WaitOne();
                        try
                        {
                            if (downloadSuccess)
                            {
                                if (!YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded)
                                {
                                    File.WriteAllBytes(dst, dataArr);
                                    YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded = true;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            mut.ReleaseMutex();
                        }
                    }
                }
            }
        }
    }
    private static void DownloadBanner(string? dirPath)
    {
        if (YTChannelPaser.GetInstance().Channel != null)
        {
            string dst = dirPath + "/[0000]-[Banner-Kenh]-[" +
                YoutubeDownloader.Core.Utils.PathEx.EscapeFileName(YTChannelPaser.GetInstance().Channel!.Name) + "].jpg";
            if (YTChannelPaser.GetInstance().Channel != null &&
                !YTChannelPaser.GetInstance().Channel!.isBannerDownloaded &&
                !File.Exists(dst))
            {
                if (YTChannelPaser.GetInstance().Channel!.Banner == "")
                {
                    if (!YTChannelPaser.GetInstance().Channel!.isBannerDownloaded)
                    {
                        try
                        {
                            string src = Directory.GetCurrentDirectory() + "/banner_trang.jpg";
                            File.Copy(src, dst);
                            YTChannelPaser.GetInstance().Channel!.isAvatarDownloaded = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    using (WebClient webClient = new WebClient())
                    {
                        bool downloadSuccess = true;
                        byte[] dataArr = new byte[1];
                        try
                        {
                            dataArr = webClient.DownloadData(YTChannelPaser.GetInstance().Channel!.Banner);
                        }
                        catch (Exception)
                        {
                            downloadSuccess = false;
                        }
                        mut.WaitOne();
                        try
                        {
                            if (downloadSuccess)
                            {
                                if (!YTChannelPaser.GetInstance().Channel!.isBannerDownloaded)
                                {
                                    File.WriteAllBytes(dst, dataArr);
                                    YTChannelPaser.GetInstance().Channel!.isBannerDownloaded = true;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            mut.ReleaseMutex();
                        }
                    }
                }
            }
        }
    }

    public void DeleteQuery()
    {
        if (!IsBusy && !string.IsNullOrWhiteSpace(Query))
        {
            Query = "";
        }
    }

    public void PasteQuery()
    {
        if (!IsBusy)
        {
            Query = System.Windows.Clipboard.GetText();
            ProcessQuery();
        }

    }

    public bool CanProcessQuery => !IsBusy && !string.IsNullOrWhiteSpace(Query);
    public async void ProcessQuery()
    {
        if (string.IsNullOrWhiteSpace(Query))
            return;

        IsBusy = true;

        // Small weight to not offset any existing download operations
        var progress = _progressMuxer.CreateInput(0.01);

        try
        {
            var result = await _queryResolver.ResolveAsync(
                Query.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                progress
            );

            // Single video
            if (result.Videos.Count == 1)
            {
                var video = result.Videos.Single();
                var downloadOptions = await _videoDownloader.GetDownloadOptionsAsync(video.Id);

                var download = await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateDownloadSingleSetupViewModel(video, downloadOptions)
                );

                if (download is null)
                    return;

                EnqueueDownload(download);
            }
            // Multiple videos
            else if (result.Videos.Count > 1)
            {
                var downloads = await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateDownloadMultipleSetupViewModel(
                        result.Title,
                        result.Videos,
                        // Pre-select videos if they come from a single query and not from search
                        result.Kind is not QueryResultKind.Search
                    )
                );

                if (downloads is null)
                    return;

                foreach (var download in downloads)
                    EnqueueDownload(download);
            }
            // No videos found
            else
            {
                await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateMessageBoxViewModel(
                        "Không tìm thấy",
                        "Không tìm thấy video nào từ đường link hoặc từ khóa bạn cung cấp"
                    )
                );
            }
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel(
                    "Error",
                    // Short error message for YouTube-related errors, full for others
                    ex is YoutubeExplodeException
                        ? ex.Message
                        : ex.ToString()
                )
            );
        }
        finally
        {
            progress.ReportCompletion();
            IsBusy = false;
        }
    }

    public void RemoveDownload(DownloadViewModel download)
    {
        Downloads.Remove(download);
        download.Cancel();
        download.Dispose();
    }

    public void RemoveSuccessfulDownloads()
    {
        foreach (var download in Downloads.ToArray())
        {
            if (download.CanShowFile)
                RemoveDownload(download);
        }
    }

    public void RemoveInactiveDownloads()
    {
        foreach (var download in Downloads.ToArray())
        {
            if (download.Status is DownloadStatus.Completed or
             DownloadStatus.Completed_Already or
             DownloadStatus.Failed or
             DownloadStatus.Canceled or
             DownloadStatus.Canceled_low_quality or
             DownloadStatus.Canceled_too_short or
             DownloadStatus.Canceled_too_long or
             DownloadStatus.Deleted)
                RemoveDownload(download);
        }
    }

    public void RestartDownload(DownloadViewModel download)
    {
        var position = Math.Max(0, Downloads.IndexOf(download));
        RemoveDownload(download);

        var newDownload = download.DownloadOption is not null
            ? _viewModelFactory.CreateDownloadViewModel(
                download.Video!,
                download.DownloadOption,
                download.FilePath!
            )
            : _viewModelFactory.CreateDownloadViewModel(
                download.Video!,
                download.DownloadPreference!,
                download.FilePath!
            );
        newDownload.ForceDownload = true;
        EnqueueDownload(newDownload, position);
    }

    public void RestartFailedDownloads()
    {
        foreach (var download in Downloads.ToArray().Reverse())
        {
            if (download.Status == DownloadStatus.Failed)
                RestartDownload(download);
        }
    }

    public void RestartCancelDownloads()
    {

        foreach (var download in Downloads.ToArray().Reverse())
        {
            if (download.Status == DownloadStatus.Canceled)
                RestartDownload(download);
        }
    }


    public void CancelAllDownloads()
    {
        foreach (var download in Downloads.Reverse())
        {
            download.Cancel();
        }
        IsBusy = false;
    }

    public void Dispose()
    {
        _downloadSemaphore.Dispose();
    }


}