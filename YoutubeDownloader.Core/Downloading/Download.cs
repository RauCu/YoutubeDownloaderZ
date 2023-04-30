using Gress;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeDownloader.Core.Models;

namespace YoutubeDownloader.Core.Downloading
{
    internal class Download
    {
        private readonly string id;
        private string link
        {
            get => id.Contains('/')
                    ? id
                    : @$"https://youtu.be/{id}";
        }
        private readonly float start;
        private readonly float end;
        private readonly string format;
        private readonly string browser;
        public bool finished = false;
        public bool successed = false;
        public string outputFilePath;
        IProgress<double>? _progress = null;
        CancellationToken cancellationToken;

        public Download(string id,
            string outputFilePath,
            IProgress<double> progress,
            CancellationToken cancellationToken = default,
                        float start = 0,
                        float end = 0,
                        string format = "",
                        string browser = "")
        {
            this.id = id;
            this.outputFilePath = outputFilePath;
            this._progress = progress;
            this.cancellationToken = cancellationToken;
            this.start = start;
            this.end = end;
            this.format = format;
            this.browser = browser;
        }

        public async Task Start()
        {
            Log.Information("Start the download process...");
            successed = false;
            finished = false;

            string tempFilePath1 = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
            string tempFilePath2 = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
            Log.Debug("Create temporary files:");
            Log.Debug("{TempFilePath}", tempFilePath1);
            Log.Debug("{TempFilePath}", tempFilePath2);

            try
            {
                OptionSet optionSet = CreateOptionSet();

                YoutubeDL? ytdl = new()
                {
                    YoutubeDLPath = "\"" +Path.Combine(Environment.CurrentDirectory, "yt-dlp.exe") +"\"",
                    FFmpegPath = "\"" + Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe") + "\"",
                    //OutputFolder = outputDirectory.FullName,
                    OutputFileTemplate = tempFilePath1,
                    OverwriteFiles = true,
                    IgnoreDownloadErrors = true
                };

                int MAX_TRY = 3;
/*                int FetchVideoInfoAsyncCount = 0;

            RetryFetchVideoInfoAsync:

                YtdlpVideoData? videoData = await FetchVideoInfoAsync(ytdl, optionSet);
                if (null == videoData)
                {
                    FetchVideoInfoAsyncCount++;
                    if (FetchVideoInfoAsyncCount < MAX_TRY)
                    {
                        goto RetryFetchVideoInfoAsync;
                    }
                    else {
                        throw new Exception("Failed to get video information!");
                    }
                }*/

                //outputFilePath = "[" + videoData?.Title + "]" + "-[" + videoData?.Id + "].mp4";
                int DownloadVideoAsyncCount = 0;
            RetryDownloadVideoAsync:
                String errorMsg = await DownloadVideoAsync(ytdl, optionSet);
                if (errorMsg != "")
                {
                    DownloadVideoAsyncCount++;
                    if (DownloadVideoAsyncCount < MAX_TRY)
                    {
                        goto RetryDownloadVideoAsync;
                    }
                    else
                    {
                        throw new Exception(errorMsg);
                    }
                }

                if (end == 0)
                {
                    Log.Information("Move file to {filePath}", outputFilePath);
                    File.Move(tempFilePath1, outputFilePath, true);
                }
                
                Log.Information("Download completed:");
                Log.Information(outputFilePath);
                successed = true;
            }
            catch (Exception e)
            {
                Log.Error("vvvvvvv");
                Log.Error(e.Message);
                Log.Error("^^^^^^^");
                throw;
            }
            finally
            {
                File.Delete(tempFilePath1);
                File.Delete(tempFilePath2);
                File.Delete(Path.ChangeExtension(tempFilePath1, "tmp"));
                File.Delete(Path.ChangeExtension(tempFilePath2, "tmp"));
                Log.Information("Clean up temporary files.");
                Log.Information("Process ends.");
                finished = true;
            }
        }

        private OptionSet CreateOptionSet()
        {
            OptionSet optionSet = new()
            {
                NoCheckCertificate = true
            };
            optionSet.AddCustomOption("--extractor-args", "youtube:skip=dash");

            if (!string.IsNullOrEmpty(format))
            {
                optionSet.Format = format;
            }
            else
            {
                // Workaround for FFmpeg sometimes uses 251 as bestvideo
                optionSet.AddCustomOption("-S", "+codec:h264");
            }

            if (!string.IsNullOrEmpty(browser))
            {
                optionSet.AddCustomOption("--cookies-from-browser", browser);
            }

            if (end != 0)
            {
                optionSet.ExternalDownloader = "ffmpeg";
                optionSet.ExternalDownloaderArgs = $"ffmpeg_i:-ss {start} -to {end}";
            }

            return optionSet;
        }

        /// <summary>
        /// 取得影片資訊
        /// </summary>
        /// <param name="ytdl"></param>
        /// <returns></returns>
        private async Task<YtdlpVideoData?> FetchVideoInfoAsync(YoutubeDL ytdl, OptionSet optionSet)
        {
            Log.Information("Start getting video information...");
            RunResult<YtdlpVideoData> result_VideoData = await ytdl.RunVideoDataFetch_Alt(link, overrideOptions: optionSet);

            if (!result_VideoData.Success)
            {
                Log.Error("Failed to get video information! VideoId: {id}", id);
                Log.Error("Please make sure your network is working and you have permission to access the video.");
                return null;
            }

            float duration = result_VideoData.Data.Duration ?? 0;

            Log.Information("{title}", result_VideoData.Data.Title);
            Log.Information("{duration}", duration);

            if (result_VideoData.Data.Duration < start)
            {
                Log.Error("Segment input invalid!");
                Log.Error("Start, End time should be smaller then video duration.");
                return null;
            }

            return result_VideoData.Data;
        }

        /// <summary>
        /// 下載影片
        /// </summary>
        /// <param name="ytdl"></param>
        /// <param name="optionSet"></param>
        /// <returns></returns>
        private async Task<String> DownloadVideoAsync(YoutubeDL ytdl, OptionSet optionSet)
        {
            Log.Information("Start downloading video...");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var progress = new Progress<DownloadProgress>(p => _progress.Report(p.Progress
                ));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var result_string = await ytdl.RunVideoDownload(
                url: link,
                mergeFormat: DownloadMergeFormat.Mp4,
                ct: cancellationToken,
                progress: progress, 
                output: new Progress<string>(s => Log.Verbose(s)),
                overrideOptions: optionSet);

            if (!result_string.Success)
            {
                String errorMsg = "Failed to download video! Please try again later.";
                Log.Error("Failed to download video! Please try again later.");
                foreach (var str in result_string.ErrorOutput)
                {
                    Log.Information(str);
                    errorMsg += "\n" + str;
                }
                return errorMsg;
            }
            Log.Information("Video downloaded.");
            return "";
        }
    }

}
