using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
        catch (Exception){
            // ignore
        }
        _cancellationTokenSource.Cancel();
    }

    public string ContentStatusText()
    {
        if(ContentStatus == ContentStatus.New)
            return "MỚI";
        if(ContentStatus == ContentStatus.MostView)
            return "Xem nhiều";
        return "Cũ";
    }


    public bool CanShowFile => (Status == DownloadStatus.Completed || Status == DownloadStatus.Completed_Already);

    public async void ShowFile()
    {
        if (!CanShowFile)
            return;

        try
        {
            // Navigate to the file in Windows Explorer
            ProcessEx.Start("explorer", new[] { "/select,", FilePath! });
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
            ToolTip tooltip = new ToolTip{ Content = "Tiêu đề video đã được sao chép" };
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;
            tooltip.StaysOpen = false;
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }

    public static string ShowDialog(string text, string error, string caption)
    {
        System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
        {
            Width = 700,
            Height = 280,
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        };

        Font LargeFont = new Font("Arial", 14);

        System.Windows.Forms.Label email_passLabel = new System.Windows.Forms.Label() { Left = 50, Top = 15, Width = 600, Text  = text };
        email_passLabel.Font = LargeFont;
        System.Windows.Forms.Label email_passLabelError = new System.Windows.Forms.Label() { Left = 50, Top = 15 + email_passLabel.Height + 5, Width = 600, Text = error };
        email_passLabelError.Font = new Font(new Font("Arial", 12), System.Drawing.FontStyle.Italic);
        
     
        email_passLabelError.AutoSize = false;
        email_passLabelError.Size = new System.Drawing.Size(600, email_passLabelError.Height * 2);
        email_passLabelError.ForeColor = Color.Red;
        

        System.Windows.Forms.TextBox email_passTextBox = new System.Windows.Forms.TextBox() { Left = 50, Top = 100, Width = 600 , Height = 80 };
        email_passTextBox.Multiline = true;
        email_passTextBox.Font = LargeFont;


        System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button()
        {
            Text = "OK",
            Left = 275,
            Width = 150,
            Height = 40,
            Top = 190,
            DialogResult = System.Windows.Forms.DialogResult.OK
        };
        confirmation.Font = LargeFont;
        confirmation.Click += (sender, e) => { prompt.Close(); };
        prompt.Controls.Add(email_passLabel);
        prompt.Controls.Add(email_passLabelError);
        prompt.Controls.Add(email_passTextBox);


        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        // TODO: remove after testing done
        //string email = "raucuqua1002@gmail.com";
        //string pass = "Testing123";
        //emailTextBox.Text = email;
        //passswordTextBox.Text = pass;


        string email_passText = "";
        if (prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (email_passTextBox.Lines.Length > 2)
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
    public async void Upload()
    {
        if (!CanShowFile)
            return;

        try
        {
            string? path = Path.GetDirectoryName(FilePath!);
            string videoPath = FilePath!;
            string[] files = System.IO.Directory.GetFiles(path!, "*" + Video!.Id + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
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

            string category = "Entertainment";
            
            if (email != "" && pass != "")
            {
                bool result = Http.SignInGJW(email, pass, videoPath, Video!.Title, category);
                if (!result)
                {
                    await _dialogManager.ShowDialogAsync(
                        _viewModelFactory.CreateMessageBoxViewModel("Đăng nhập tự động thất bại.", "Hãy kiểm tra lại để đảm bảo rằng email và mật khẩu đúng. Hoặc hãy đăng nhập thủ công!")
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowDialogAsync(
                _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
            );
        }
    }
    public async void WathOnYoutube()
    {
        if (MessageBox.Show("Bạn có muốn xem video này trên Youtube không?",
                            "Xem trên Youtube",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            string url = "https://www.youtube.com/watch?v=" + Video!.Id;
            IWebDriver? driver = null;
            try
            {
                driver = Http.GetDriver();
                driver.Navigate().GoToUrl(url);
            }
            catch (Exception ex)
            {
                await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
                );
            }
        }
    }

    public async void SearchOnGJW()
    {
        if (MessageBox.Show("Bạn có muốn kiểm tra xem video này đã được đăng lên GJW chưa?",
                            "Kiểm tra trùng",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            string[] parts = Video!.Title.Split(" ");
            string newTitle = "";
            for (int i = 1; i < parts.Length -1 ; i++){
                newTitle += parts[i] + " ";
            }

            // TODO: bỏ 2 từ đầu và 2 từ cuối
            string url = "https://www.ganjing.com/search?s=" + newTitle + "&type=video";
            IWebDriver? driver = null;
            try
            {
                driver = Http.GetDriver();
                driver.Navigate().GoToUrl(url);
            }
            catch (Exception ex)
            {
                await _dialogManager.ShowDialogAsync(
                    _viewModelFactory.CreateMessageBoxViewModel("Lỗi", ex.Message)
                );
            }
        }
    }

    public bool CanOpenFile => (Status == DownloadStatus.Completed || Status == DownloadStatus.Completed_Already);

    public async void OpenFile()
    {
        if (!CanOpenFile)
            return;

        try
        {
            string? path = Path.GetDirectoryName(FilePath!);
            string[] files = System.IO.Directory.GetFiles(path!,"*" + Video!.Id + "*.mp4", System.IO.SearchOption.TopDirectoryOnly);
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

    private void DeleteFileInternal(){
        string? path = Path.GetDirectoryName(FilePath!);
        string[] files = System.IO.Directory.GetFiles(path!,"*" + Video!.Id + "*.*", System.IO.SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
        {
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
                
        }     
        Status =  DownloadStatus.Deleted;
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