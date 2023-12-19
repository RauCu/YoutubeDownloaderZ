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
using System.ComponentModel;
using System.Windows;
using YoutubeExplode.Videos;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CliWrap;
using static System.Windows.Forms.LinkLabel;
using System.Text;
using System.Windows.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace YoutubeDownloader.ViewModels.Components;

public class DashboardViewModel : PropertyChangedBase, IDisposable
{
    private bool allItemsAreChecked;
    public event PropertyChangedEventHandler PropertyChanged;
    private String selectedFolder;
    private String selectedFile;

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
    int GJWChannelNumber = -1;
    public bool OpenMutileAccountVideo
    {
        get { return p_OpenMutileAccountVideo; }

        set
        {
            p_OpenMutileAccountVideo = value;
            if (p_OpenMutileAccountVideo)
            {
                GJWChannelNumber = 0;
            }
            else
            {
                GJWChannelNumber = -1;
            }
            base.NotifyOfPropertyChange("OpenMutileAccountVideo");
        }
    }

    private string p_TextOfUploadOrSignInButton;
    public string TextOfUploadOrSignInButton
    {
        get { return (NumberVideoNeedToUpload == 0 ? "ĐĂNG NHẬP GJW" : "TẢI LÊN (" + NumberVideoNeedToUpload + ")"); }

        set
        {
            p_TextOfUploadOrSignInButton = value;
            base.NotifyOfPropertyChange("TextOfUploadOrSignInButton");
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
        OpenMutileAccountVideo = false;
        

        // Subscribe to CollectionChanged event
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        Downloads.CollectionChanged += OnDownloadListChanged;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
    }
    public bool p_OpenMutileAccountVideo = false;

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

    public bool isBrowserClosed(IWebDriver driver)
    {
        bool isClosed = true;
        try
        {
            var title = driver.Title;
            //var h = driver.CurrentWindowHandle;
            //driver.FindElement(By.TagName("html"));
            isClosed = false;
        }
        catch (Exception)
        {
        }

        return isClosed;
    }

    IWebDriver? mDriver = null;
    bool login_success = false;
    string language = "";
    public async void UploadMultipleVideo()
    {
        if (!_settingsService.IsDarkModeEnabled)
        {
            DisableUpload();
            return;
        }

        // reset previous upload status
        foreach (var download in Downloads.ToArray())
        {
            download.UploadDone = false;
            download.UploadError = false;
        }
        //
        bool allSuccess = true;


#pragma warning disable CS8604 // Possible null reference argument.
        if (isBrowserClosed(mDriver))
        {
            bool isSignedInOnly = true;
            if (NumberVideoNeedToUpload > 0)
            {
                isSignedInOnly = false;
            }
            mDriver = DownloadViewModel.SignInGJWStatic(out login_success, isSignedInOnly, -1);
            if (!login_success)
            {
                // await _dialogManager.ShowDialogAsync(
                //     _viewModelFactory.CreateMessageBoxViewModel("Đăng nhập tự động thất bại.", "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!")
                // );
                System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                if (mDriver != null)
                {
                    currentSize = mDriver.Manage().Window.Size;
                    mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                }
                else
                {
                    return;
                }

                string msg = "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!";

                MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                    "Đăng nhập tự động thất bại.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Question);
                try
                {
                    if (confirm == MessageBoxResult.OK)
                    {
                        if (mDriver != null)
                        {
                            mDriver.Manage().Window.Size = currentSize;
                        }

                    }
                }
                catch (Exception)
                {

                }
            }
            else
            {
                if (NumberVideoNeedToUpload == 0)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    mDriver.Manage().Window.Maximize();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }

        }
        int waitingTime = 10000;
        if (NumberVideoNeedToUpload > 0 && login_success)
        {
            foreach (var download in Downloads.ToArray().Reverse())
            {
                if (download.CanShowFile && download.SelectedToUpload)
                {

                    bool errorOccur = false;
                    try
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        download.UploadOnly(mDriver);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    catch (Exception ex)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                        try
                        {
                            if (mDriver != null)
                            {
                                currentSize = mDriver.Manage().Window.Size;
                                mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                            }
                        }
                        catch (Exception)
                        {

                        }

                        if (!mDriver.PageSource.Contains("Upload Limit Reached"))
                        {
                            errorOccur = true;
                            // await _dialogManager.ShowDialogAsync(
                            //     _viewModelFactory.CreateMessageBoxViewModel("Lỗi", "Đăng video lỗi: " + download.FileNameShort + "\n\n\n" + ex.Message)
                            // );


                            string msg = "Đăng video lỗi, đang lỗi ở video: " + download.FileName + "\n\n\nVui lòng làm theo ĐÚNG 3 BƯỚC sau:\n\n" +
                                "- BƯỚC 1: trên trình duyệt, XOÁ hoặc đăng thủ công (dùng nút 3-4-5) video đang đăng bị lỗi và các video đang ở trạng thái \"Uploading 0%\" (nếu có).\n" +
                                "- BƯỚC 2: KHÔNG ĐƯỢC TẮT TRÌNH DUYỆT, quay lại giao diện phần mềm.\n" +
                                "- BƯỚC 3: bấm nút TẢI LÊN để tiếp tục đăng các video chưa được đăng.\n" +
                                "-       Lưu ý: ở bước 1, nếu đăng thủ công video lỗi thành công, thì nhớ bỏ tích chọn video đó, để không đăng lại nó nữa.\n" +
                                "\n\n\n\nThông tin lỗi chi tiết (để dành cho kỹ thuật phân tích lỗi):\n" + ex.Message;

                            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                                "Lỗi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Question);
                            try
                            {
                                if (confirm == MessageBoxResult.OK)
                                {
                                    if (mDriver != null)
                                    {
                                        mDriver.Manage().Window.Size = currentSize;
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            // stop upload
                            allSuccess = false;

                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        else
                        {
                            waitingTime = 1;
                            string msg = "Bạn đã đăng đủ video cho tài khoản GJW này trong hôm nay. \n" +
                                        "Hãy chuyển qua đăng cho tài khoản GJW khác, hoặc đợi sang ngày mai.";
                            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                                "Lỗi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Question);
                            try
                            {
                                if (confirm == MessageBoxResult.OK)
                                {
                                    if (mDriver != null)
                                    {
                                        mDriver.Manage().Window.Size = currentSize;
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            // stop upload
                            allSuccess = true;
                        }

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
            }
            if (mDriver != null && allSuccess)
            {
                // Đảm bảo là đang ở tab Video chứ không phải ở tab Short thì mới có thể thấy là đang Uploading hay đang Transcoding
                Http.openVideoTab(mDriver, false);

                Thread.Sleep(waitingTime);
                while (true)
                {
                    string pageSrc = mDriver.PageSource.Replace("Uploading 0%", "");
                    if (!isUploading(pageSrc))
                    {
                        System.Drawing.Size currentSize = mDriver.Manage().Window.Size;
                        mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);

                        string msg = "";
                        if (isTranscoding(pageSrc) || willTranscode(pageSrc))
                        {
                            msg = "Đang Transcoding ... ";
                        }
                        msg += "Có thể tắt trình duyệt được rồi! Đồng ý tắt?";
                        MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                            "Đã Upload xong video!",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        try
                        {
                            if (confirm == MessageBoxResult.Yes)
                            {
                                mDriver.Quit();
                                mDriver = null;
                            }
                            else
                            {
                                mDriver.Manage().Window.Size = currentSize;
                            }
                        }
                        catch (Exception)
                        {

                        }
                        break;
                    }
                    Thread.Sleep(3000);
                }
            }
        }
    }
    private bool needLogin(int GJWChannelNumber)
    {
        bool result = false;
        if (NumberVideoNeedToUpload > 0)
        {
            foreach (var download in Downloads.ToArray().Reverse())
            {
                if (download.CanShowFile && download.SelectedToUpload){
                    if (GJWChannelNumber != -1)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (AutoDownUpDB.videos.ElementAt(GJWChannelNumber).Value.Contains(download.Video.Url))
                        {
                            result = true;
                            break;
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    else
                    {
                        result = true;
                        break;
                    }
                }
            }
        }
        return result;
    }
    public async void DisableUpload()
    {
        await _dialogManager.ShowDialogAsync(
        _viewModelFactory.CreateMessageBoxViewModel(
            "Chức năng tự động đăng video tạm thời bị tắt!",
            "Thông báo: Chức năng tự động đăng video tạm thời bị tắt.\nVui lòng đăng video bằng cách thủ công! \n\nLý do: các bạn đăng video tự động không kiểm duyệt lại nội dung video trước khi đăng lên GJW dẫn tới rất nhiều nội dung không tốt được đưa lên nền tảng!"
            )
        );
    }
    public async void UploadMultipleVideoMultipleGJWChannel()
    {
        if (!_settingsService.IsDarkModeEnabled)
        {
            DisableUpload();
            return;
        }

        GJWChannelNumber = 0;
        bool showDialog = true;
        Up_More:

        // reset previous upload status
        foreach (var download in Downloads.ToArray())
        {
            download.UploadDone = false;
            download.UploadError = false;
        }
        //
        bool allSuccess = true;

#pragma warning disable CS8604 // Possible null reference argument.
        if (isBrowserClosed(mDriver))
        {
            if (needLogin(GJWChannelNumber))
            {
                bool isSignedInOnly = true;
                if (NumberVideoNeedToUpload > 0)
                {
                    isSignedInOnly = false;
                }
                mDriver = DownloadViewModel.SignInGJWStatic(out login_success, isSignedInOnly, GJWChannelNumber, showDialog);
                if (!login_success)
                {
                    // await _dialogManager.ShowDialogAsync(
                    //     _viewModelFactory.CreateMessageBoxViewModel("Đăng nhập tự động thất bại.", "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!")
                    // );
                    System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                    if (mDriver != null)
                    {
                        currentSize = mDriver.Manage().Window.Size;
                        mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                    }
                    else
                    {
                        return;
                    }

                    string msg = "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!";

                    MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                        "Đăng nhập tự động thất bại.",
                        MessageBoxButton.OK,
                        MessageBoxImage.Question);
                    try
                    {
                        if (confirm == MessageBoxResult.OK)
                        {
                            if (mDriver != null)
                            {
                                mDriver.Manage().Window.Size = currentSize;
                            }

                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {
                    if (NumberVideoNeedToUpload == 0)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        mDriver.Manage().Window.Maximize();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
            }
            else
            {
                if (GJWChannelNumber < AutoDownUpDB.videos.Keys.Count - 1)
                {
                    GJWChannelNumber++;
                  
                    goto Up_More;
                }
            }
        }
        int waitingTime = 10000;
        if (NumberVideoNeedToUpload > 0 && login_success)
        {
            foreach (var download in Downloads.ToArray().Reverse())
            {
                if (download.CanShowFile && download.SelectedToUpload)
                {   if(GJWChannelNumber !=-1)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (!AutoDownUpDB.videos.ElementAt(GJWChannelNumber).Value.Contains(download.Video.Url)){
                            // skip if not belong to current GJW Channel
                            continue;
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }

                    bool errorOccur = false;
                    try
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        download.UploadOnly(mDriver);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    catch (Exception ex)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
                        try
                        {
                            if (mDriver != null)
                            {
                                currentSize = mDriver.Manage().Window.Size;
                                mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);
                            }
                        }
                        catch (Exception)
                        {

                        }

                        if (!mDriver.PageSource.Contains("Upload Limit Reached"))
                        {
                            errorOccur = true;
                            // await _dialogManager.ShowDialogAsync(
                            //     _viewModelFactory.CreateMessageBoxViewModel("Lỗi", "Đăng video lỗi: " + download.FileNameShort + "\n\n\n" + ex.Message)
                            // );


                            string msg = "Đăng video lỗi, đang lỗi ở video: " + download.FileName + "\n\n\nVui lòng làm theo ĐÚNG 3 BƯỚC sau:\n\n" +
                                "- BƯỚC 1: trên trình duyệt, XOÁ hoặc đăng thủ công (dùng nút 3-4-5) video đang đăng bị lỗi và các video đang ở trạng thái \"Uploading 0%\" (nếu có).\n" +
                                "- BƯỚC 2: KHÔNG ĐƯỢC TẮT TRÌNH DUYỆT, quay lại giao diện phần mềm.\n" +
                                "- BƯỚC 3: bấm nút TẢI LÊN để tiếp tục đăng các video chưa được đăng.\n" +
                                "-       Lưu ý: ở bước 1, nếu đăng thủ công video lỗi thành công, thì nhớ bỏ tích chọn video đó, để không đăng lại nó nữa.\n" +
                                "\n\n\n\nThông tin lỗi chi tiết (để dành cho kỹ thuật phân tích lỗi):\n" + ex.Message;

                            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                                "Lỗi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Question);
                            try
                            {
                                if (confirm == MessageBoxResult.OK)
                                {
                                    if (mDriver != null)
                                    {
                                        mDriver.Manage().Window.Size = currentSize;
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            // stop upload
                            allSuccess = false;

                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        else
                        {
                            waitingTime = 1;
                            string msg = "Bạn đã đăng đủ video cho tài khoản GJW này trong hôm nay. \n" +
                                        "Hãy chuyển qua đăng cho tài khoản GJW khác, hoặc đợi sang ngày mai.";
                            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                                "Lỗi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Question);
                            try
                            {
                                if (confirm == MessageBoxResult.OK)
                                {
                                    if (mDriver != null)
                                    {
                                        mDriver.Manage().Window.Size = currentSize;
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }

                            // stop upload
                            allSuccess = true;
                        }

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
            }
            if (mDriver != null && allSuccess)
            {
                // Đảm bảo là đang ở tab Video chứ không phải ở tab Short thì mới có thể thấy là đang Uploading hay đang Transcoding
                Http.openVideoTab(mDriver, false);

                Thread.Sleep(waitingTime);
                while (true)
                {
                    string pageSrc = mDriver.PageSource.Replace("Uploading 0%", "");
                    if (!isUploading(pageSrc))
                    {
                        System.Drawing.Size currentSize = mDriver.Manage().Window.Size;
                        mDriver.Manage().Window.Size = new System.Drawing.Size(480, 320);

                        string msg = "";
                        if (isTranscoding(pageSrc) || willTranscode(pageSrc))
                        {
                            msg = "Đang Transcoding ... ";
                        }
                        msg += "Có thể tắt trình duyệt được rồi! Đồng ý tắt?";
                        if (GJWChannelNumber == -1 || GJWChannelNumber == AutoDownUpDB.videos.Keys.Count - 1)
                        {
                            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                                "Đã Upload xong video!",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);
                            try
                            {
                                if (confirm == MessageBoxResult.Yes)
                                {
                                    mDriver.Quit();
                                    mDriver = null;
                                }
                                else
                                {
                                    mDriver.Manage().Window.Size = currentSize;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                        else
                        {
                            if(GJWChannelNumber < AutoDownUpDB.videos.Keys.Count -1)
                            {
                                Thread.Sleep(3000);
                                showDialog = false;
                                GJWChannelNumber++;
                                mDriver.Quit();
                                mDriver = null;

                                goto Up_More;
                            }
                        }
                        break;
                    }
                    Thread.Sleep(3000);
                }
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
        //
        if(text.Contains("begins in"))
        {
            result = true;
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

    private bool willTranscode(string text)
    {
        bool result = false;
        string t = "Transcoding begins in";
        if (text.Contains(t))
        {
            result = true;
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
                videoInfo = Database.Find(Http.getVideoID(download.Video));
                if (!download.ForceDownload)
                {
                    if (videoInfo != null && videoInfo.DownloadStatus != "")
                    {
                        string status = videoInfo.DownloadStatus;
                        if (status.Equals(DownloadStatus.Completed.ToString()) ||
                            status.Equals(DownloadStatus.Completed_Already.ToString())
                            )
                        {
                            if (!File.Exists(download.FilePath))
                            {
                                string? path = Path.GetDirectoryName(download.FilePath!);
                                string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(download.Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
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
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (videoInfo.DownloadStatus == "")
                        {
                            if (!File.Exists(download.FilePath))
                            {
                                string? path = Path.GetDirectoryName(download.FilePath!);
                                string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(download.Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
                                if (files.Length == 0)
                                {
                                    // video deleted from FileExplorer
                                    shouldDownload = true;
                                }
                                else
                                {  // renamed
                                    download.Status = DownloadStatus.Completed_Already;
                                    shouldDownload = false;
                                }
                            }
                            else
                            {
                                download.Status = DownloadStatus.Completed_Already;
                                shouldDownload = false;
                            }
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    VideoDownloadOption downloadOption = download.DownloadOption;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (!Http.isOtherVideo(download.Video))
                    {
                        downloadOption = download.DownloadOption is not null ?
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

                        bool lowQualityVideoDownloaded = false; // disable check low qualiy
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
                            // workaround to disable > 60 minute
                            tooLong = false;
                        }

                        bool okLength = !tooShort && !tooLong;

                        shouldDownload = (hightQualityVideoDownloaded && okLength) || (lowQualityVideoDownloaded && okLength) || (audioDownloaded && okLength) || download.ForceDownload;
                    }
                    else
                    {
                        shouldDownload = true;
                        download.Status = DownloadStatus.Started;
                    }
                  
                    if (shouldDownload)
                    {
                        Console.WriteLine("Accepted!");
                        //
                        DirectoryEx.CreateDirectoryForFile(download.FilePath!);
                        try
                        {
                            File.WriteAllBytes(download.FilePath!, Array.Empty<byte>());
                        }
                        catch (Exception)
                        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(download.FilePath!);
                            fileName = fileName.Replace("....", "");
                            fileName = fileName.Replace("...", "");
                            fileName = fileName.Replace("..", "");
                            fileName = Regex.Replace(fileName, @"\s+", " ");
                            fileName = fileName.Replace(" ", "_");
                            fileName = fileName.Replace(",", "_");

                            fileName = Regex.Replace(fileName, @"[^\u0020-\u007E]", string.Empty);
                            fileName = fileName.Substring(0, 150) +"]-[" + Http.getVideoID(download.Video) +"]";
                            fileName = fileName.Replace("]]", "]");
                            download.FilePath = Path.GetDirectoryName(download.FilePath!) + "\\" + fileName + ".mp4";
                            try {
                                File.WriteAllBytes(download.FilePath!, Array.Empty<byte>());
                            }catch (Exception)
                            {
                                download.FilePath = Path.GetDirectoryName(download.FilePath!) + "\\" + Http.ReplaceTitleByID(System.IO.Path.GetFileNameWithoutExtension(download.FilePath) + ".mp4");
                                File.WriteAllBytes(download.FilePath!, Array.Empty<byte>());
                            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                        }
          
                        await _videoDownloader.DownloadVideoAsync(
                            download.FilePath!,
                            download.Video!,
                            downloadOption!,
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
                        //if (okLength)
                        {
                            Console.WriteLine("Canceled_low_quality");
                            //throw new Exception("Canceled_low_quality");
                            download.Status = DownloadStatus.Canceled_low_quality;
                        }
                        /*
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
                        }*/
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
                        mut.WaitOne(2000);
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
                        //mut.WaitOne();
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
                            //mut.ReleaseMutex();
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
                        //mut.WaitOne();
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
                            //mut.ReleaseMutex();
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

    public async void GetListVideoYTChannel()
    {
        if (!IsBusy)
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            string appPath = "\"" + Path.Combine(Environment.CurrentDirectory, "LayDanhSachVideoKenhYT.exe") + "\"";

            try
            {

                //var result = await Cli.Wrap("cmd").WithArguments($"/c chcp 65001 > null && {appPath}").WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer)).WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer)).ExecuteAsync();

                //Showing the loader  
                
                Spinner spinner = new Spinner();
                Form frm = new Form();
                frm.Size = new System.Drawing.Size((int)(spinner.Width * 1.8), (int)(spinner.Height * 1.8));
                spinner.Location = new System.Drawing.Point(frm.Width/2 - spinner.Width/2 -10, frm.Height / 2 - spinner.Height / 2 - 20);

                frm.StartPosition = FormStartPosition.CenterScreen;
                frm.Controls.Add(spinner);
                frm.Show();

                await Cli.Wrap(appPath).ExecuteAsync().ConfigureAwait(true);

                frm.Close();
                frm.Hide();
                frm.Dispose();
            }
            catch
            {

            }


        }
    }

    public async void OpenFolder()
    {
        if (!IsBusy)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    OpenMutileAccountVideo = false;
                    selectedFolder = dialog.SelectedPath;
                    try
                    {
                        Database.Load(selectedFolder);
                        string[] files = System.IO.Directory.GetFiles(selectedFolder!, "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
                        string youtubeURLBase = "https://www.youtube.com/watch?v=";
                        string queryTxt = "";
                        if (files.Length > 0)
                        {
                            HashSet<string> youtubeURLs = new HashSet<string>();
                            HashSet<string> otherURLs = new HashSet<string>();
                            for (int i = 0; i < files.Length; i++)
                            {
                                try
                                {
                                    VideoInfo videoInfo = VideoInfoParser.Parse(Path.GetFileName(files[i]));
                                    string videoID = videoInfo.Id.Substring(0, videoInfo.Id.Length - 5);

                                    VideoInfo? videoInfoDB = Database.Find(videoID);
                                    if (videoInfoDB != null)
                                    {
                                        if ((videoInfoDB.URL == "" || videoInfoDB.URL.Contains("youtube.com")))
                                        {
                                            youtubeURLs.Add(videoInfoDB.URL == "" ? (youtubeURLBase + videoID) : videoInfoDB.URL);

                                        }
                                        else
                                        {
                                            otherURLs.Add(videoInfoDB.URL);
                                        }
                                    }

                                }
                                catch (Exception) { }

                            }

                            bool hasVideo = false;
                            // other sites
                            queryTxt = "";
                            foreach (string otherURL in otherURLs)
                            {
                                queryTxt += otherURL + "\n";
                            }
                            if (queryTxt != "")
                            {
                                hasVideo = true;
                                Query = queryTxt;
                                ProcessQueryForFolder();
                            }

                            // youtube only, check first
                            queryTxt = "";
                            foreach (string youtubeURL in youtubeURLs)
                            {
                                queryTxt += youtubeURL + "\n";
                            }
                            if (queryTxt != "")
                            {
                                hasVideo = true;
                                Query = queryTxt;
                                ProcessQueryForFolder();
                            }

                            if (hasVideo == false)
                            {
                                await _dialogManager.ShowDialogAsync(
                                _viewModelFactory.CreateMessageBoxViewModel(
                                    "Không tìm thấy tệp MP4",
                                    "Không tìm thấy tệp MP4 DO PHẦN MỀM NÀY TẢI VỀ trong thư mục được chọn.\nĐường dẫn thư mục: " + selectedFolder
                                    )
                                );
                            }
                        }
                        else
                        {
                            await _dialogManager.ShowDialogAsync(
                            _viewModelFactory.CreateMessageBoxViewModel(
                                "Không tìm thấy tệp MP4",
                                "Không tìm thấy tệp MP4 nào trong thư mục được chọn.\nĐường dẫn thư mục: " + selectedFolder
                                )
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        await _dialogManager.ShowDialogAsync(
                            _viewModelFactory.CreateMessageBoxViewModel(
                                "LỖI",
                                "Mở thư mục bị lỗi!\nĐường dẫn thư mục: " + selectedFolder + "\n\n\n" +
                                "Mã lỗi chi tiết dành cho kỹ thuật:\n" + ex.ToString()
                            )
                        );
                    }
                }
            }
        }
    }

    public async void OpenFileMultiple()
    {
        if (!IsBusy)
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "Duyệt file chứa danh sách video và kênh GJW";
                dialog.DefaultExt = "txt";
                dialog.Multiselect = false;
                dialog.Filter = "txt files (*.txt)|*.txt";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    selectedFile = dialog.FileName;
#pragma warning disable CS8601 // Possible null reference assignment.
                    selectedFolder = Path.GetDirectoryName(selectedFile);
#pragma warning restore CS8601 // Possible null reference assignment.
                    try
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        Database.Load(selectedFolder);
#pragma warning restore CS8604 // Possible null reference argument.
                        AutoDownUpDB.Load(selectedFile);
                        Query = AutoDownUpDB.getAllVideo();
                        OpenMutileAccountVideo = true;
                        
                        ProcessQueryForFolder();
                    }
                    catch (Exception ex)
                    {
                        OpenMutileAccountVideo = false;
                        
                        await _dialogManager.ShowDialogAsync(
                            _viewModelFactory.CreateMessageBoxViewModel(
                                "LỖI",
                                "Mở thư mục bị lỗi!\nĐường dẫn thư mục: " + selectedFolder + "\n\n\n" +
                                "Mã lỗi chi tiết dành cho kỹ thuật:\n" + ex.ToString()
                            )
                        );
                    }
                }
            }
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
    public async void ProcessQueryForFolder()
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

                //var download = await _dialogManager.ShowDialogAsync(
                //    _viewModelFactory.CreateDownloadSingleSetupViewModel(video, downloadOptions)
                //);
                var viewModel = _viewModelFactory.CreateDownloadSingleSetupViewModel();

                viewModel.Video = video;
                viewModel.AvailableDownloadOptions = downloadOptions;
                viewModel.OnViewLoaded();
                viewModel.ConfirmPath(selectedFolder);
                var download = viewModel.DialogResult;
                if (download is null)
                    return;

                EnqueueDownload(download);
            }
            // Multiple videos
            else if (result.Videos.Count > 1)
            {
                /*var downloads = await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateDownloadMultipleSetupViewModel(
                        result.Title,
                        result.Videos,
                        // Pre-select videos if they come from a single query and not from search
                        result.Kind is not QueryResultKind.Search
                    )
                );*/

                var viewModel = _viewModelFactory.CreateDownloadMultipleSetupViewModel();
                viewModel.Title = result.Title;
                viewModel.AvailableVideos = result.Videos;
                viewModel.SelectedVideos = result.Kind is not QueryResultKind.Search
                    ? viewModel.AvailableVideos
                    : Array.Empty<IVideo>();

                viewModel.ConfirmPath(selectedFolder);
                var downloads = viewModel.DialogResult;
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
                    "Lỗi",
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

    public async void ProcessQuery()
    {
        bool needRetry = false;
    RetrySearching:
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

            if(result.Kind == QueryResultKind.Other)
            {
                // Single video
                if (result.Videos.Count == 1)
                {
                    var video = result.Videos.Single();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var download = await _dialogManager.ShowDialogAsync(
                        _viewModelFactory.CreateDownloadSingleSetupViewModel(video, null)
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

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
            else
            {
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
        
        }
        catch (Exception ex)
        {
            //Video 'RyTpqBNPRdo' is not available.
            if (ex.Message.Contains("is not available."))
            {
                // skip deleted video, e.g: https://www.youtube.com/watch?v=RyTpqBNPRdo
                String videoID = ex.Message.Replace("' is not available.", "").Replace("Video '", "");

                String lineToDelete = "https://www.youtube.com/watch?v=" + videoID;
                Query = Query.Replace(lineToDelete, "").Trim();
                if (Query.Contains("https://www.youtube.com/watch?v="))
                {
                    needRetry = true;
                }
                else
                {
                    needRetry = false;
                }
            }
            else
            {
                await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel(
                    "Lỗi",
                    // Short error message for YouTube-related errors, full for others
                    ex is YoutubeExplodeException
                        ? ex.Message
                        : ex.ToString()
                )
                );
            }

        }
        finally
        {
            progress.ReportCompletion();
            IsBusy = false;
        }
        if (needRetry)
        {
            goto RetrySearching;
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