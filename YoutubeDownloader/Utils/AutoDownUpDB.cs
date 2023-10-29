using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YoutubeDownloader.ViewModels.Components;

namespace YoutubeDownloader.Utils
{
    public class AutoDownUpDB
    {
        //private static Mutex mut = new Mutex();
        // GJW email + pass as key
        // list video as values
        public static Dictionary<string, string> videos { get; set; } = new Dictionary<string, string>();
        private static string? DirPath;

        //Insert statement
      
        //Select statement
        public static string getAllVideo()
        {
            string videoURLs = "";
            foreach (var item in videos)
            {
                videoURLs += item.Value + "\n";
            }
            videoURLs = DownloadViewModel.RemoveEmptyLines(videoURLs);
            return videoURLs;
        }

        private static Dictionary<string, string> processLine(string line)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            line = DownloadViewModel.RemoveEmptyLines(line);
            if (line == "" || line.StartsWith('#')){
                result.Add("", "");
            }
            else if (line.StartsWith("TL:", System.StringComparison.OrdinalIgnoreCase))
            {
                line = line.Replace("TL:", "").Trim();
                result.Add("CATEGORY", line);
            }
            else if (line.StartsWith("https://"))
            {
                result.Add("LINK", line);
            }else if(Regex.IsMatch(line, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
            {
                result.Add("EMAIL", line);
            }
            else
            {
                string[] p = line.Split(' ');
                if (Regex.IsMatch(p[0], @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase))
                {
                    if (p.Length == 2) {
                        result.Add("EMAIL_PASS", p[0] + "::" + p[1]);
                    }
                }
                else
                {
                    result.Add("PASS", line);
                }
            }
            return result;
        }
        public static void Load(string filePath)
        {
            try
            {
                videos.Clear();
                List<string> lines = File.ReadAllLines(filePath).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToList();
                string GJWAccount = "";
                string listVideo = "";
                for (int i = 0; i < lines.Count; i++)
                {
                    Dictionary<string, string> r = processLine(lines[i]);
                    if (r.Keys.Contains("EMPTY"))
                    {

                    }
                    else if (r.Keys.Contains("EMAIL"))
                    {
                        GJWAccount = r.Values.First().ToString();
                        listVideo = "";
                    }
                    else if (r.Keys.Contains("PASS"))
                    {
                        GJWAccount += "::" + r.Values.First().ToString();
                        listVideo = "";
                    }
                    else if (r.Keys.Contains("LINK"))
                    {
                        listVideo += r.Values.First().ToString() + '\n';
                        if (videos.ContainsKey(GJWAccount))
                        {
                            videos.Remove(GJWAccount);
                        }
                        videos.Add(GJWAccount, listVideo);
                    }
                    else if (r.Keys.Contains("EMAIL_PASS"))
                    {
                        GJWAccount = r.Values.First().ToString();
                        listVideo = "";
                    }
                    else if (r.Keys.Contains("CATEGORY"))
                    {
                        GJWAccount += "::" + r.Values.First().ToString();
                        listVideo = "";
                    }
                }
            }
            catch (System.Exception)
            {
            }
            finally
            {
                //mut.ReleaseMutex();
            }
        }
    }
}
