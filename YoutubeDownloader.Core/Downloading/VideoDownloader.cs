using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gress;
using YoutubeDownloader.Core.Utils;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Common;
using System.Net;
using System.Linq;
using CliWrap;

namespace YoutubeDownloader.Core.Downloading;

public class VideoDownloader
{
    private readonly YoutubeClient _youtube = new(Http.Client);

    public async Task<IReadOnlyList<VideoDownloadOption>> GetDownloadOptionsAsync(
        VideoId videoId,
        CancellationToken cancellationToken = default)
    {
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(videoId, cancellationToken);
        return VideoDownloadOption.ResolveAll(manifest);
    }

    public async Task<VideoDownloadOption> GetBestDownloadOptionAsync(
        VideoId videoId,
        VideoDownloadPreference preference,
        CancellationToken cancellationToken = default)
    {
        var options = await GetDownloadOptionsAsync(videoId, cancellationToken);

        return
            preference.TryGetBestOption(options) ??
            throw new InvalidOperationException("No suitable download option found.");
    }

    public async Task DownloadVideoAsync(
        string filePath,
        IVideo video,
        VideoDownloadOption downloadOption,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default)
    {

        File.Delete(filePath);
        var dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
            Directory.CreateDirectory(dirPath);

        String[] qualityThumbnails = new String[] { "maxresdefault.jpg", "sddefault.jpg", "hqdefault.jpg", "mqdefault.jpg", "default.jpg" };
        using (WebClient webClient = new WebClient())
        {
            int i = 0;
            String bestQualityThumbnail = qualityThumbnails[i];

            String thumbnailURL;
            while (i < qualityThumbnails.Length)
            {
                bool downloadSuccess = true;
                bestQualityThumbnail = qualityThumbnails[i];
                thumbnailURL = "https://img.youtube.com/vi/" + video.Id + "/" + bestQualityThumbnail;
                if(Http.isOtherVideo(video))
                {
                    thumbnailURL = video.Thumbnails.ElementAt(0).Url;
                }
                //Console.WriteLine(thumbnailURL);
                byte[] dataArr = new byte[1];

                try
                {
                    dataArr = webClient.DownloadData(thumbnailURL);
                }
                catch (WebException ex)
                {
                    // (HttpWebResponse)ex.Response).StatusCode
                    //Console.WriteLine("DownloadImage", ex.Message + " " + ex.InnerException + "URL: " + thumbnailURL + "Response: " + ((HttpWebResponse)ex.Response).StatusCode.ToString(), "Image");
                    downloadSuccess = false;
                    i++;
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    //Console.WriteLine("DONE");
                }

                if (downloadSuccess)
                {
                    //save file to local
                    string thumbnailFileName = Http.RemoveTitle(System.IO.Path.GetFileNameWithoutExtension(filePath) + ".jpg");
                    string thumbnailFileName_2 = Http.RemoveTitle(System.IO.Path.GetFileNameWithoutExtension(filePath) + "_2.jpg");
                    string thumbnailPath = dirPath + "/" + thumbnailFileName ;
                    string thumbnailPath_2 = dirPath + "/" + thumbnailFileName_2;
                    File.WriteAllBytes(thumbnailPath, dataArr);
                    if (Http.isOtherVideo(video))
                    {
                        
                        // fix lỗi hình ảnh tải về của 1 số trang khi đăng lên GJW không được,
                        // vi 1 lý do nào đó, nên dùng ffmpeg để convert lại:
                        //  ./ffmpeg.exe -i .\hinh-goc.jpg hinh-up-len-gjw-ok.jpg
                        string appPath = "\"" + Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe") + "\"";
                        string arguments = "-i " + "\"" + thumbnailPath + "\"" + " " + "\"" + thumbnailPath_2 + "\"";
                        var result = await Cli.Wrap(appPath).WithArguments(arguments).WithValidation(CommandResultValidation.None).ExecuteAsync();

                        if (File.Exists(thumbnailPath_2))
                        {
                            Thread.Sleep(1000);
                            try
                            {
                                File.Delete(thumbnailPath);
                                File.Move(thumbnailPath_2, thumbnailPath);
                            }catch(Exception ex) { }
                        }


                    }
                    //Console.WriteLine(thumbnailURL);
                    break;
                }
            }
        }
        if (Http.isOtherVideo(video))
        {
            OtherVideo otherVideo = (OtherVideo)video;
#pragma warning disable CS8604 // Possible null reference argument.
            Download download = new Download(otherVideo.Url, otherVideo.Duration, filePath, progress?.ToDoubleBased(),
                cancellationToken);
#pragma warning restore CS8604 // Possible null reference argument.

            await download.Start().ConfigureAwait(false);
        }
        else
        {
            int? quality = downloadOption.VideoQuality?.MaxHeight;

            // If the target container supports subtitles, embed them in the video too
            var trackInfos = !downloadOption.Container.IsAudioOnly
                ? (await _youtube.Videos.ClosedCaptions.GetManifestAsync(video.Id, cancellationToken)).Tracks
                : Array.Empty<ClosedCaptionTrackInfo>();

            try
            {
                await _youtube.Videos.DownloadAsync(
                     downloadOption.StreamInfos,
                     trackInfos,
                     new ConversionRequestBuilder(filePath)
                         .SetContainer(downloadOption.Container)
                         .SetPreset(ConversionPreset.Medium)
                         .Build(),
                     progress?.ToDoubleBased(),
                     cancellationToken
                 );
            }
            catch (Exception)
            {
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable IDE0090 // Use 'new(...)'
                Download download = new Download (video.Url, video.Duration, filePath, progress?.ToDoubleBased(),
                    cancellationToken);
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning restore CS8604 // Possible null reference argument.

                await download.Start().ConfigureAwait(false);
            }
        }

    }
}