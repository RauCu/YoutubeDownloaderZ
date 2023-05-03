using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Common;
using Gress;
using SharpCompress.Common;
using YoutubeDLSharp;
using YoutubeDownloader.Core.Downloading;
using YoutubeDownloader.Core.Models;
using YoutubeDownloader.Core.Utils;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Core.Resolving;

public class QueryResolver
{
    private readonly YoutubeClient _youtube = new(Http.Client);

    public async Task<QueryResult> ResolveAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Only consider URLs when parsing IDs.
        // All other queries should be treated as search queries.
        var isUrl = Uri.IsWellFormedUriString(query, UriKind.Absolute);

        // Playlist
        if (isUrl && PlaylistId.TryParse(query) is { } playlistId)
        {
            var playlist = await _youtube.Playlists.GetAsync(playlistId, cancellationToken);
            var videos = await _youtube.Playlists.GetVideosAsync(playlistId, cancellationToken);
            return new QueryResult(QueryResultKind.Playlist, $"Danh sách phát: {playlist.Title}", videos);
        }

        // Video
        if (isUrl && VideoId.TryParse(query) is { } videoId)
        {
            var video = await _youtube.Videos.GetAsync(videoId, cancellationToken);
            return new QueryResult(QueryResultKind.Video, video.Title, new[] { video });
        }

        // Channel
        if (isUrl && ChannelId.TryParse(query) is { } channelId)
        {
            var channel = await _youtube.Channels.GetAsync(channelId, cancellationToken);
            var videos = await _youtube.Channels.GetUploadsAsync(channelId, cancellationToken);
            return new QueryResult(QueryResultKind.Channel, $"Kênh: {channel.Title}", videos);
        }

        // Channel (by handle)
        if (isUrl && ChannelHandle.TryParse(query) is { } channelHandle)
        {
            var channel = await _youtube.Channels.GetByHandleAsync(channelHandle, cancellationToken);
            var videos = await _youtube.Channels.GetUploadsAsync(channel.Id, cancellationToken);
            return new QueryResult(QueryResultKind.Channel, $"Kênh: {channel.Title}", videos);
        }

        // Channel (by username)
        if (isUrl && UserName.TryParse(query) is { } userName)
        {
            var channel = await _youtube.Channels.GetByUserAsync(userName, cancellationToken);
            var videos = await _youtube.Channels.GetUploadsAsync(channel.Id, cancellationToken);
            return new QueryResult(QueryResultKind.Channel, $"Kênh: {channel.Title}", videos);
        }

        // Channel (by slug)
        if (isUrl && ChannelSlug.TryParse(query) is { } channelSlug)
        {
            var channel = await _youtube.Channels.GetBySlugAsync(channelSlug, cancellationToken);
            var videos = await _youtube.Channels.GetUploadsAsync(channel.Id, cancellationToken);
            return new QueryResult(QueryResultKind.Channel, $"Kênh: {channel.Title}", videos);
        }

        {
            if (query.Contains("https://"))
            {
                string result = await Download.FetchVideoInfoAsync3(query);
                List<string> list = new List<string>(
                           result.Split(new string[] { "\n" },
                           StringSplitOptions.RemoveEmptyEntries));

                List<string> link = new List<string>(
                          query.Split(new string[] { " " },
                          StringSplitOptions.RemoveEmptyEntries));

                List<IVideo> listData = new List<IVideo>();
                if (list.Count % 4 == 0)
                {   
                    for (int i = 0; i < list.Count /4; i++)
                    {
                        string title = list[4*i + 0];
                        string id = list[4 * i + 1];
                        string thumbnail = list[4 * i + 2];
                        string duration = list[4 * i + 3];

                        Console.WriteLine("\n\n ==> link: " + link[i]);
                        Console.WriteLine("id: " + id);
                        Console.WriteLine("title: " + title);
                        Console.WriteLine("duration: " + duration);
                        Console.WriteLine("thumbnail: " + thumbnail);
                        
                        IVideo video = new OtherVideo(link[i], id, title, duration, thumbnail);
                        listData.Add(video);
                    }
                }
                return new QueryResult(QueryResultKind.Other, query, listData.AsReadOnly());

            }
        }

        // Search
        {
            var videos = await _youtube.Search.GetVideosAsync(query, cancellationToken).CollectAsync(20);
            return new QueryResult(QueryResultKind.Search, $"Từ khóa: {query}", videos);
        }
    }

    public async Task<QueryResult> ResolveAsync(
        IReadOnlyList<string> queries,
        IProgress<Percentage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (queries.Count == 1)
            return await ResolveAsync(queries.Single(), cancellationToken);

        var videos = new List<IVideo>();
        var videoIds = new HashSet<string>();

        var completed = 0;
        bool other = false;
        string fullQuery = "";

        foreach (var query in queries)
        {
            if(query.Contains("https://") && query.Contains("youtube.com"))
            {
                
            }
            else
            {
                other = true;
            }
            fullQuery += query + " ";
        }


        if (other)
        {
            var result = await ResolveAsync(fullQuery, cancellationToken);
            foreach (var video in result.Videos)
            {
                if (videoIds.Add(video.Url))
                    videos.Add(video);
            }
        }
        else
        {
            foreach (var query in queries)
            {
                var result = await ResolveAsync(query, cancellationToken);
                foreach (var video in result.Videos)
                {
                    if (videoIds.Add(video.Id))
                        videos.Add(video);
                }

                progress?.Report(
                    Percentage.FromFraction(1.0 * ++completed / queries.Count)
                );
            }

        }

   


        return new QueryResult(other? QueryResultKind.Other : QueryResultKind.Aggregate, $"{queries.Count} truy vấn", videos);
    }
}