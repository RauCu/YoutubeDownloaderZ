using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Helpers;
using YoutubeDLSharp.Options;
using YoutubeDownloader.Core.Models;
using System.Text;
using System.Text.Unicode;

namespace YoutubeDownloader.Core.Downloading;

internal static partial class YoutubeDLExtension
{
#nullable disable
    /// <summary>
    /// Modified from YoutubeDL.RunVideoDataFetch()
    /// </summary>
    /// <param name="ytdl"></param>
    /// <param name="url"></param>
    /// <param name="ct"></param>
    /// <param name="flat"></param>
    /// <param name="overrideOptions"></param>
    /// <returns></returns>
    /*#pragma warning disable CA1068 // CancellationToken 參數必須位於最後
        public static async Task<RunResult<YtdlpVideoData>> RunVideoDataFetch_Alt(this YoutubeDLSharp.YoutubeDL ytdl, string url, CancellationToken ct = default, bool flat = true, OptionSet overrideOptions = null)
    #pragma warning restore CA1068 // CancellationToken 參數必須位於最後
        {
            OptionSet optionSet = new()
            {
                IgnoreErrors = ytdl.IgnoreDownloadErrors,
                IgnoreConfig = true,
                NoPlaylist = true,
                HlsPreferNative = true,
                ExternalDownloaderArgs = "-nostats -loglevel 0",
                Output = Path.Combine(ytdl.OutputFolder, ytdl.OutputFileTemplate),
                RestrictFilenames = ytdl.RestrictFilenames,
                NoContinue = ytdl.OverwriteFiles,
                NoOverwrites = !ytdl.OverwriteFiles,
                NoPart = true,
                FfmpegLocation = YoutubeDLSharp.Utils.GetFullPath(ytdl.FFmpegPath),
                Exec = "echo {}"
            };
            if (overrideOptions != null)
            {
                optionSet = optionSet.OverrideOptions(overrideOptions);
            }

            optionSet.DumpSingleJson = true;
            optionSet.FlatPlaylist = flat;
            YtdlpVideoData videoData = null;
            YoutubeDLProcess youtubeDLProcess = new(ytdl.YoutubeDLPath);
            youtubeDLProcess.OutputReceived += (o, e) =>
            {
                // Workaround: Fix invalid json directly
                var data = e.Data.Replace("\"[{", "[{")
                                 .Replace("}]\"", "}]")
                                 .Replace("False", "false")
                                 .Replace("True", "true");
                // Change json string from 'sth' to "sth"
                data = data.Replace("'", "''");
                videoData = Newtonsoft.Json.JsonConvert.DeserializeObject<YtdlpVideoData>(data);
            };
            FieldInfo fieldInfo = typeof(YoutubeDLSharp.YoutubeDL).GetField("runner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField);
            (int code, string[] errors) = await (fieldInfo.GetValue(ytdl) as ProcessRunner).RunThrottled(youtubeDLProcess, new[] { url }, optionSet, ct);
            return new RunResult<YtdlpVideoData>(code == 0, errors, videoData);
        }
    #nullable enable*/

#pragma warning disable CA1068 // CancellationToken 參數必須位於最後
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public static async Task<RunResult<string>> RunVideoDataFetch_Alt(this YoutubeDLSharp.YoutubeDL ytdl, string url, CancellationToken ct = default, bool flat = true, OptionSet? overrideOptions = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CA1068 // CancellationToken 參數必須位於最後
    {
        OptionSet optionSet = new()
        {
            GetId = true,
            GetTitle = true,
            GetDuration = true,
            GetThumbnail = true,
            Encoding = "utf8"

        };
        if (overrideOptions != null)
        {
            optionSet = optionSet.OverrideOptions(overrideOptions);
        }

        string videoData = "";
        YoutubeDLProcess youtubeDLProcess = new(ytdl.YoutubeDLPath);
        youtubeDLProcess.UseWindowsEncodingWorkaround = true;
        youtubeDLProcess.OutputReceived += (o, e) =>
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            videoData += e.Data + "\n";
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        };
        List<string> link = new List<string>(
                          url.Split(new string[] { " " },
                          StringSplitOptions.RemoveEmptyEntries));
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        FieldInfo fieldInfo = typeof(YoutubeDLSharp.YoutubeDL).GetField("runner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        (int code, string[] errors) = await (fieldInfo.GetValue(ytdl) as ProcessRunner).RunThrottled(youtubeDLProcess, link.ToArray(), optionSet, ct);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        return new RunResult<string>(code == 0, errors, videoData);
    }
#nullable enable

    /* [GeneratedRegex("(?:[\\s:\\[\\{\\(])'([^'\\r\\n\\s]*)'(?:\\s,]}\\))")]
     private static partial Regex ChangeJsonStringSingleQuotesToDoubleQuotes();*/
}

