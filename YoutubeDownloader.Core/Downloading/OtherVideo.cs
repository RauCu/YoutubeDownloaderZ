using AngleSharp.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Core.Downloading;

/// <summary>
/// Metadata associated with a  video.
/// </summary>
public class OtherVideo : IVideo
{
    /// <inheritdoc />
    public VideoId Id { get; }

    public string otherId { get; }

    /// <inheritdoc />
    public string Url { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public Author Author { get; }

    /// <summary>
    /// Video upload date.
    /// </summary>
    public DateTimeOffset UploadDate { get; }

    /// <summary>
    /// Video description.
    /// </summary>
    public string Description { get; }

    /// <inheritdoc />
    public TimeSpan? Duration { get; }

    public int DurationInSecond { get; }

    /// <inheritdoc />
    public IReadOnlyList<Thumbnail> Thumbnails { get; }

    /// <summary>
    /// Available search keywords for the video.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; }

    /// <summary>
    /// Engagement statistics for the video.
    /// </summary>
    public Engagement Engagement { get; }
    public static string OTHER_VIDEO = "FFFFFFFFFFF";

    /// <summary>
    /// Initializes an instance of <see cref="Video" />.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public OtherVideo(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        string link,
        string id,
        string title,
        string duration,
        string thumbnail
        )
    {
        Url = link;
        Id = OTHER_VIDEO;
        otherId =  id;
        Title = title;
        Duration = stringToTimeSpan(duration);
        List<Thumbnail> listData = new List<Thumbnail>();
        listData.Add(new Thumbnail(thumbnail, new Resolution(1, 1)));
        Thumbnails = listData.AsReadOnly();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Author = null;
        UploadDate = new DateTimeOffset();
        Description = null;
        Keywords = null;
        Engagement = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Video ({Title})";

    TimeSpan stringToTimeSpan(string value)
    {

        string[] parts = value.Split(":");
        Array.Reverse(parts);
        int totalSecond = 0;

        int[] mul = { 1 , 60, 60 * 60, 60 * 60 * 60, 60 * 60 * 60 * 60 };
        for(int i = 0; i < parts.Length; i++)
        {
            totalSecond+= Int32.Parse(parts[i]) * mul[i];
        }

        return TimeSpan
            .FromSeconds(totalSecond);
    }
}