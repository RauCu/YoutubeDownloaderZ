﻿using YoutubeDownloader.Core.Utils;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Core.Downloading;

public class FileNameTemplate
{
    public static string Apply(
        string template,
        IVideo video,
        Container container,
        string? number = null) =>
        PathEx.EscapeFileName(
            template
                .Replace("$num", number is not null ? $"{number}" : "xx")
                .Replace("$title", video.Title)
                .Replace("$id", Http.getVideoID(video))
                .Replace("$uploadDate", (video as Video)?.UploadDate.ToString("yyyy-MM-dd") ?? "")
                .Trim() + '.' + container.Name
        );
}