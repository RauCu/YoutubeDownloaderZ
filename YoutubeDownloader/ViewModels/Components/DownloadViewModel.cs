using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Gress;
using OpenQA.Selenium;
using Stylet;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Utils;
using YoutubeDownloader.Utils;
using YoutubeDownloader.ViewModels.Dialogs;
using YoutubeDownloader.ViewModels.Framework;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.ViewModels.Components;

public class DownloadViewModel : PropertyChangedBase, IDisposable
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly DialogManager _dialogManager;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public IVideo? Video { get; set; }

    public VideoDownloadOption? DownloadOption { get; set; }

    public VideoDownloadPreference? DownloadPreference { get; set; }

    public string? FilePath { get; set; }

    public string? FileName => Path.GetFileName(FilePath);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    public string? FileNameShort => FileName.Substring(0, FileName.Length - 18);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    public ProgressContainer<Percentage> Progress { get; } = new();

    public bool IsProgressIndeterminate => Progress.Current.Fraction is <= 0 or >= 1;

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public DownloadStatus Status { get; set; } = DownloadStatus.Enqueued;
    public ContentStatus ContentStatus { get; set; } = ContentStatus.New;

    public bool IsCanceledOrFailed => Status is DownloadStatus.Canceled or
     DownloadStatus.Canceled_low_quality or
     DownloadStatus.Canceled_too_short or
     DownloadStatus.Canceled_too_long or
     DownloadStatus.Failed or
     DownloadStatus.Deleted;

    public static bool ScheduleEnabled { get; set; } = false;
    public static bool UnlistedEnabled { get; set; } = false;
    public static int SelectedCategoryIndex { get; set; } = 21; // News & Politics

    public bool SelectedToUpload { get; set; } = false;
    public bool UploadDone { get; set; } = false;
    public bool AlreadyUploaded { get; set; } = false;

    public bool UploadError { get; set; } = false;

    public string? ErrorMessage { get; set; }

    public DownloadViewModel(IViewModelFactory viewModelFactory, DialogManager dialogManager)
    {
        _viewModelFactory = viewModelFactory;
        _dialogManager = dialogManager;

        Progress.Bind(o => o.Current, (_, _) => NotifyOfPropertyChange(() => IsProgressIndeterminate));
    }

    public bool CanCancel => Status is DownloadStatus.Enqueued or DownloadStatus.Started;

    public bool ForceDownload { get; set; } = false;

    public void Cancel()
    {
        if (!CanCancel)
            return;
        try
        {
            DeleteFileInternal();
        }
        catch (Exception)
        {
            // ignore
        }
        _cancellationTokenSource.Cancel();
    }

    public string ContentStatusText()
    {
        if (ContentStatus == ContentStatus.New)
            return "MỚI";
        if (ContentStatus == ContentStatus.MostView)
            return "Xem nhiều";
        return "Cũ";
    }


    public bool CanShowFile => (Status == DownloadStatus.Completed || Status == DownloadStatus.Completed_Already);

    public void AlreadyUploadedChanged()
    {

    }

    public async void ShowFile()
    {
        if (!CanShowFile)
            return;

        try
        {
            // Navigate to the file in Windows Explorer
            ProcessEx.Start("explorer", new[] { "/select,",  FilePath! });
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }

    public async void CopyVideoPath()
    {
        if (!CanShowFile)
            return;

        try
        {
            Clipboard.SetText(FilePath!);
            //MessageBox.Show("Tên video đã được sao chép (copy)!", "Sao chép tên video", MessageBoxButton.OK, MessageBoxImage.Information);
            ToolTip tooltip = new ToolTip { Content = "Đường dẫn video đã được sao chép" };
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;
            DispatcherTimer vTimer = new DispatcherTimer();
            vTimer.Interval = new TimeSpan(0, 0, 3);
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tick += new EventHandler(vTimer_Tick);
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tag = tooltip;
            vTimer.Start();
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }

    public async void CopyThumbnailPath()
    {
        if (!CanShowFile)
            return;

        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            String thumbnailPath = Http.RemoveTitle(FilePath.Substring(0, FilePath.Length - 4) + ".jpg");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            Clipboard.SetText(thumbnailPath);
            //MessageBox.Show("Tên video đã được sao chép (copy)!", "Sao chép tên video", MessageBoxButton.OK, MessageBoxImage.Information);
            ToolTip tooltip = new ToolTip { Content = "Đường dẫn hình thumbnail đã được sao chép" };
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;

            DispatcherTimer vTimer = new DispatcherTimer();
            vTimer.Interval = new TimeSpan(0, 0, 3);
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tick += new EventHandler(vTimer_Tick);
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tag = tooltip;
            vTimer.Start();
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }


    public async void CopyVideoTitle()
    {
        if (!CanShowFile)
            return;

        try
        {
            Clipboard.SetText(Video!.Title);
            //MessageBox.Show("Tên video đã được sao chép (copy)!", "Sao chép tên video", MessageBoxButton.OK, MessageBoxImage.Information);
            ToolTip tooltip = new ToolTip { Content = "Tiêu đề video đã được sao chép" };
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;

            DispatcherTimer vTimer = new DispatcherTimer();
            vTimer.Interval = new TimeSpan(0, 0, 3);
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tick += new EventHandler(vTimer_Tick);
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            vTimer.Tag = tooltip;
            vTimer.Start();
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }

    void vTimer_Tick(object sender, EventArgs e)
    {
        DispatcherTimer? vTimer = sender as DispatcherTimer;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        vTimer.Stop();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        ToolTip? vTip = vTimer.Tag as ToolTip;
        if (vTip != null)
            vTip.IsOpen = false;

    }

    private static string RemoveEmptyLines(string lines)
    {
        return Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
    }

    public static string ShowDialog(string text, string error, string caption)
    {
        System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
        {
            Width = 700,
            Height = 280 + 30,
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        };

        Font LargeFont = new Font("Arial", 12);

        System.Windows.Forms.Label email_passLabel = new System.Windows.Forms.Label() { Left = 50, Top = 15, Width = 600, Text = text };
        email_passLabel.Font = LargeFont;
        System.Windows.Forms.Label email_passLabelError = new System.Windows.Forms.Label() { Left = 50, Top = 15 + email_passLabel.Height + 5, Width = 600, Text = error };
        email_passLabelError.Font = new Font(new Font("Arial", 12), System.Drawing.FontStyle.Italic);


        email_passLabelError.AutoSize = false;
        email_passLabelError.Size = new System.Drawing.Size(600, email_passLabelError.Height * 2);
        email_passLabelError.ForeColor = Color.Red;


        System.Windows.Forms.TextBox email_passTextBox = new System.Windows.Forms.TextBox() { Left = 50, Top = 100, Width = 600, Height = 80 };
        email_passTextBox.Multiline = true;
        email_passTextBox.Font = LargeFont;


        System.Windows.Forms.CheckBox checkBoxScheduleEnable = new System.Windows.Forms.CheckBox()
        {
            AutoSize = false,
            Text = "HẸN 24 TIẾNG\nSAU MỚI ĐĂNG",
            Left = 50,
            Top = 190,
            Width = 350,
            Height = 40 + 30 + 30,
            Font = LargeFont,
            ForeColor = Color.Red,
            Size = new System.Drawing.Size(225, 50)
        };

        ScheduleEnabled = false;
        checkBoxScheduleEnable.Checked = ScheduleEnabled;
        checkBoxScheduleEnable.Click += (sender, e) => { ScheduleEnabled = checkBoxScheduleEnable.CheckState == System.Windows.Forms.CheckState.Checked ? true : false; };

        //
        System.Windows.Forms.ComboBox categoryComboBox = new System.Windows.Forms.ComboBox();
        categoryComboBox.Location = new System.Drawing.Point(50, 235);
        categoryComboBox.Text = "CHỌN THỂ LOẠI";
        categoryComboBox.Size = new System.Drawing.Size(225, 200);
        categoryComboBox.Font = LargeFont;
        categoryComboBox.BackColor = System.Drawing.Color.Orange;
        categoryComboBox.ForeColor = System.Drawing.Color.Black;

        //
        string[] supportedCategories = {
            "Architecture & Engineering",
            "Arts & Design",
            "Autos",
            "Business",
            "Career",
            "Classical Music",
            "Dance",
            "Education",
            "Entertainment",
            "Fashion & Beauty",
            "Food",
            "Gaming",
            "Government",
            "Health & Fitness",
            "History & Culture",
            "Kids",
            "Life Hacks",
            "Lifestyle",
            "Military",
            "Mindfulness",
            "Nature",
            "News & Politics",
            "Nonprofit",
            "Pets",
            "Popular Music",
            "Real Estate & Finance",
            "Relationship",
            "Science & Tech",
            "Spirituality",
            "Sports",
            "Supernatural",
            "Travel"
        };
        foreach (string category in supportedCategories)
        {
            categoryComboBox.Items.Add(category);
        }
        SelectedCategoryIndex = 21;
        categoryComboBox.SelectedIndexChanged += (sender, e) => { SelectedCategoryIndex = categoryComboBox.SelectedIndex; };

        //

        System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button()
        {
            Text = "DÁN VÀ ĐĂNG NHẬP",
            Left = 275,
            Width = 150,
            Height = 40 + 30,
            Top = 190,
            DialogResult = System.Windows.Forms.DialogResult.OK
        };

        //

        System.Windows.Forms.CheckBox checkBoxUnlistedEnable = new System.Windows.Forms.CheckBox()
        {
            AutoSize = false,
            Text = "ĐĂNG VIDEO Ở\nCHẾ ĐỘ UNLISTED",
            Left = 275 + 150 + 50,
            Top = 190,
            Width = 350,
            Height = 40 + 30 + 30,
            Font = LargeFont,
            ForeColor = Color.Red,
            Size = new System.Drawing.Size(225, 50)
        };

        UnlistedEnabled = false;
        checkBoxUnlistedEnable.Checked = UnlistedEnabled;
        checkBoxUnlistedEnable.Click += (sender, e) => { UnlistedEnabled = checkBoxUnlistedEnable.CheckState == System.Windows.Forms.CheckState.Checked ? true : false; };

        //

        confirmation.Font = LargeFont;
        confirmation.Click += (sender, e) => { email_passTextBox.Text = Clipboard.GetText(); prompt.Close(); };
        prompt.Controls.Add(email_passLabel);
        prompt.Controls.Add(email_passLabelError);
        prompt.Controls.Add(email_passTextBox);
        prompt.Controls.Add(categoryComboBox);
        prompt.Controls.Add(checkBoxScheduleEnable);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(checkBoxUnlistedEnable);
        
        prompt.AcceptButton = confirmation;

        string email_passText = "";
        if (prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            email_passTextBox.Text = RemoveEmptyLines(email_passTextBox.Text);
            if (email_passTextBox.Lines.Length >= 2)
            {
                email_passTextBox.Text = email_passTextBox.Lines[0] + Environment.NewLine + email_passTextBox.Lines[1];
                email_passText = email_passTextBox.Lines[0] + " " + email_passTextBox.Lines[1];
            }
            else if (email_passTextBox.Lines.Length == 1)
            {
                email_passText = email_passTextBox.Lines[0];
            }

            email_passText = Regex.Replace(email_passText, @"\s+", " ");
        }
        else
        {
            email_passText = "close";
        }
        return email_passText;
    }

    int videoWidth = 0; int videoHeight = 0;

    public void GetVideoInfo(string videoPath)
    {
        videoWidth = 0;
        videoHeight = 0;
        try
        {
            var ffProbe = new NReco.VideoInfo.FFProbe();
            var videoInfo = ffProbe.GetMediaInfo(videoPath);
            NReco.VideoInfo.MediaInfo.StreamInfo[] s = videoInfo.Streams;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Width != 0 && s[i].Height != 0)
                {
                    videoWidth = s[i].Width;
                    videoHeight = s[i].Height;
                    break;
                }
            }
        }
        catch (Exception)
        {
           /* var media = new MediaInfoWrapper(videoPath, new Logger<DownloadViewModel>(new NullLoggerFactory()));
            if (media.Success)
            {
                videoWidth = media.Width;
                videoHeight = media.Height;
            }*/
        }
    }

    public static IWebDriver? SignInGJWStatic(out bool login_success)
    {
        login_success = false;
        IWebDriver? driver = null;
        try
        {
            string email_pass = ShowDialog("Email và mật khẩu của kênh GJW", "", "Đăng nhập kênh GJW");
        re_enter:
            if (email_pass == "close")
            {
                return null;
            }

            string[] parts = email_pass.Trim().Split(" ");
            string email = "";
            string pass = "";
            if (parts.Length == 2)
            {
                email = parts[0];
                pass = parts[1];
            }
            else
            {
                email_pass = ShowDialog("Email và mật khẩu sai định dạng, vui lòng nhập lại.",
                    "  Email và mật khẩu phải ở 2 dòng khác nhau; hoặc có thể ở chung 1 dòng nhưng phải cách nhau bởi dấu cách!", "Đăng nhập kênh GJW");
                goto re_enter;
            }

            if (email != "" && pass != "")
            {
                driver = Http.SignInGJW(email, pass, out login_success);
            }
        }
        catch (Exception)
        {

        }
        return driver;

    }

    public IWebDriver? SignInGJW(out bool login_success)
    {
        login_success = false;
        IWebDriver? driver = null;
        if (!CanShowFile)
            return null;

        try
        {
            string? path = Path.GetDirectoryName(FilePath!);
            string videoPath = FilePath!;
            string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    videoPath = files[i];
                    break;
                }
            }

            string email_pass = ShowDialog("Email và mật khẩu của kênh GJW", "", "Đăng nhập kênh GJW");
        re_enter:
            if (email_pass == "close")
            {
                return null;
            }

            string[] parts = email_pass.Trim().Split(" ");
            string email = "";
            string pass = "";
            if (parts.Length == 2)
            {
                email = parts[0];
                pass = parts[1];
            }
            else
            {
                email_pass = ShowDialog("Email và mật khẩu sai định dạng, vui lòng nhập lại.",
                    "  Email và mật khẩu phải ở 2 dòng khác nhau; hoặc có thể ở chung 1 dòng nhưng phải cách nhau bởi dấu cách!", "Đăng nhập kênh GJW");
                goto re_enter;
            }

            if (email != "" && pass != "")
            {
                driver = Http.SignInGJW(email, pass, out login_success);
            }
        }
        catch (Exception)
        {

        }
        return driver;
    }

    public void UploadOnly(IWebDriver driver)
    {
        if (!CanShowFile)
            return;

        string? path = Path.GetDirectoryName(FilePath!);
        string videoPath = FilePath!;
        string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
        {
            for (int i = 0; i < files.Length; i++)
            {
                videoPath = files[i];
                break;
            }
        }

        bool isShortVideo = false;
        if (Video!.Duration?.TotalSeconds <= 60)
        {
            GetVideoInfo(videoPath);
            if (videoHeight > videoWidth)
            {
                isShortVideo = true;
            }
        }
        UploadDone = false;
        UploadError = false;
        Http.UploadVideo(driver, isShortVideo, videoPath, Video!.Title, ScheduleEnabled, UnlistedEnabled, SelectedCategoryIndex, Http.language);
    }



    public async void Upload()
    {
        bool login_success = false;
        IWebDriver? driver = null;
        if (!CanShowFile)
            return;

        // reset previous upload status
        UploadDone = false;
        UploadError = false;

        //
        bool errorOccur = false;
        try
        {
            string? path = Path.GetDirectoryName(FilePath!);
            string videoPath = FilePath!;
            string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    videoPath = files[i];
                    break;
                }
            }

            string email_pass = ShowDialog("Email và mật khẩu của kênh GJW", "", "Đăng nhập kênh GJW");
        re_enter:
            if (email_pass == "close")
            {
                return;
            }

            string[] parts = email_pass.Trim().Split(" ");
            string email = "";
            string pass = "";
            if (parts.Length == 2)
            {
                email = parts[0];
                pass = parts[1];
            }
            else
            {
                email_pass = ShowDialog("Email và mật khẩu sai định dạng, vui lòng nhập lại.",
                    "  Email và mật khẩu phải ở 2 dòng khác nhau; hoặc có thể ở chung 1 dòng nhưng phải cách nhau bởi dấu cách!", "Đăng nhập kênh GJW");
                goto re_enter;
            }

            

            if (email != "" && pass != "")
            {
                driver = Http.SignInGJW(email, pass, out login_success);
                if (!login_success)
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
                    try
                    {
                        if (confirm == MessageBoxResult.OK)
                        {
                            if (driver != null)
                            {
                                driver.Manage().Window.Size = currentSize;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    bool isShortVideo = false;
                    if (Video!.Duration?.TotalSeconds <= 60)
                    {

                        GetVideoInfo(videoPath);
                        if (videoHeight > videoWidth)
                        {
                            isShortVideo = true;
                        }
                    }
                    Http.UploadVideo(driver, isShortVideo, videoPath, Video!.Title, ScheduleEnabled, UnlistedEnabled, SelectedCategoryIndex, Http.language);
                }
            }
        }
        catch (Exception ex)
        {
            errorOccur = true;
            // await _dialogManager.ShowDialogAsync(
            //     _viewModelFactory.CreateMessageBoxViewModel("Lỗi", "Đăng video lỗi: " + FileNameShort + "\n\n\n" + ex.Message)
            // );
            System.Drawing.Size currentSize = new System.Drawing.Size(480, 320);
            if (driver != null)
            {
                currentSize = driver.Manage().Window.Size;
                driver.Manage().Window.Size = new System.Drawing.Size(480, 320);
            }
            string msg = "Đăng video lỗi, đang lỗi ở video: " + Video!.Title + "\n\n\nBạn có thể đăng video bị lỗi này thủ công," +
            "\nbằng cách dùng các nút 3-4-5 để sao chép các thông tin như tiêu đề, đường dẫn video, hình, rồi dán vào trình duyệt!\n\n\n" + ex.Message;

            MessageBoxResult confirm = System.Windows.MessageBox.Show(msg,
                "Lỗi",
                MessageBoxButton.OK,
                MessageBoxImage.Question);
            try
            {
                if (confirm == MessageBoxResult.OK)
                {
                    if (driver != null)
                    {
                        driver.Manage().Window.Size = currentSize;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        finally
        {
            if (errorOccur == false)
            {
                UploadDone = true;
                SelectedToUpload = false;
            }
            else
            {
                UploadError = true;
            }
        }
    }
    public void WathOnYoutube()
    {
        // if (MessageBox.Show("Bạn có muốn xem video này trên Youtube không?",
        //                     "Xem trên Youtube",
        //                     MessageBoxButton.YesNo,
        //                     MessageBoxImage.Question) == MessageBoxResult.Yes)
        // {
        string url = "https://www.youtube.com/watch?v=" + Video!.Id;
        if(!Video!.Url.Contains("youtube.com"))
        {
            url = Video!.Url;
        }
        // System.Diagnostics.Process.Start(url);
        Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
        // IWebDriver? driver = null;
        // try
        // {
        //      driver = Http.GetDriver();
        //      driver.Navigate().GoToUrl(url);
        // }
        // catch (Exception ex)
        // {
        //     await _dialogManager.ShowDialogAsync(
        //         _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
        //     );
        // }
        // }
    }

    public void SearchOnGJW()
    {
        // if (MessageBox.Show("Bạn có muốn kiểm tra xem video này đã được đăng lên GJW chưa?",
        //                     "Kiểm tra trùng",
        //                     MessageBoxButton.YesNo,
        //                     MessageBoxImage.Question) == MessageBoxResult.Yes)
        // {
        string[] parts = Video!.Title.Split(" ");
        string newTitle = "";
        for (int i = 1; i < parts.Length - 1; i++)
        {
            newTitle += parts[i] + " ";
        }

        // TODO: bỏ 2 từ đầu và 2 từ cuối
        string url = "https://www.ganjing.com/search?s=" + newTitle + "&type=video";
        // System.Diagnostics.Process.Start(url);
        Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
        // IWebDriver? driver = null;
        // try
        // {
        //     driver = Http.GetDriver();
        //     driver.Navigate().GoToUrl(url);
        // }
        // catch (Exception ex)
        // {
        //     await _dialogManager.ShowDialogAsync(
        //         _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
        //     );
        // }
        // }
    }

    public bool CanOpenFile => (Status == DownloadStatus.Completed || Status == DownloadStatus.Completed_Already);

    public async void OpenFile()
    {
        if (!CanOpenFile)
            return;

        try
        {
            string? path = Path.GetDirectoryName(FilePath!);
            string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(Video) + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                ProcessEx.StartShellExecute(files[0]);
            }

        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }

    private void DeleteFileInternal()
    {
        string? path = Path.GetDirectoryName(FilePath!);
        string[] files = System.IO.Directory.GetFiles(path!, "*" + Http.getVideoID(Video) + "*.*", System.IO.SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
        {
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }

        }
        Status = DownloadStatus.Deleted;

        // UpdateNumberVideoNeedToUpload
        SelectedToUpload = false;
    }
    public async void DeleteFile()
    {
        if (!CanOpenFile)
            return;
        if (MessageBox.Show("Bạn có chắc là muốn xóa video này?",
                            "Xóa video",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                DeleteFileInternal();
            }
            catch (Exception ex)
            {
                await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
                );
            }
        }
    }


    public void Dispose() => _cancellationTokenSource.Dispose();
}

public static class DownloadViewModelExtensions
{
    public static DownloadViewModel CreateDownloadViewModel(
        this IViewModelFactory factory,
        IVideo video,
        VideoDownloadOption downloadOption,
        string filePath)
    {
        var viewModel = factory.CreateDownloadViewModel();

        viewModel.Video = video;
        viewModel.DownloadOption = downloadOption;
        viewModel.FilePath = filePath;

        return viewModel;
    }

    public static DownloadViewModel CreateDownloadViewModel(
        this IViewModelFactory factory,
        IVideo video,
        VideoDownloadPreference downloadPreference,
        string filePath)
    {
        var viewModel = factory.CreateDownloadViewModel();

        viewModel.Video = video;
        viewModel.DownloadPreference = downloadPreference;
        viewModel.FilePath = filePath;

        return viewModel;
    }
}