using System;
using System.IO;
using System.Net;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace YoutubeDownloader.Core.Utils
{
    public partial class ChromeConfigOverride : ChromeConfig
    {

        private const string BaseVersionPatternUrl = "https://chromedriver.storage.googleapis.com/<version>/";
        private const string LatestReleaseVersionUrl = "https://chromedriver.storage.googleapis.com/LATEST_RELEASE";

        private const string ExactReleaseVersionPatternUrl =
            "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_<version>";

        private bool isHigher115Version = false;
        private string myRawChromeBrowserVersion = "";

        override public string GetName()
        {
            return "Chrome";
        }

        override public string GetUrl32()
        {
            return GetUrl();
        }

        override public string GetUrl64()
        {
            return GetUrl();
        }

        private string GetUrl()
        {
#if NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var architectureExtension =
                    RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64
                        ? "_arm64"
                        : "64";
                return $"{BaseVersionPatternUrl}chromedriver_mac{architectureExtension}.zip";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"{BaseVersionPatternUrl}chromedriver_linux64.zip";
            }
#endif
            if (isHigher115Version)
            {
                return "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/" + myRawChromeBrowserVersion + "/win32/chromedriver-win32.zip";
            }
            return $"{BaseVersionPatternUrl}chromedriver_win32.zip";
        }

        override public string GetBinaryName()
        {
#if NETSTANDARD
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            var isWindows = true;
#endif
            var suffix = isWindows ? ".exe" : string.Empty;
            return $"chromedriver{suffix}";
        }

        override public string GetLatestVersion()
        {
            return GetLatestVersion(LatestReleaseVersionUrl);
        }

        private static string GetLatestVersion(string url)
        {
            var uri = new Uri(url);
            var webRequest = WebRequest.Create(uri);
            using (var response = webRequest.GetResponse())
            {
                using (var content = response.GetResponseStream())
                {
                    if (content == null) throw new ArgumentNullException($"Can't get content from URL: {uri}");
                    using (var reader = new StreamReader(content))
                    {
                        var version = reader.ReadToEnd().Trim();
                        return version;
                    }
                }
            }
        }

        override public string GetMatchingBrowserVersion()
        {
            var rawChromeBrowserVersion = GetRawBrowserVersion();
            if (string.IsNullOrEmpty(rawChromeBrowserVersion))
            {
                throw new Exception("Not able to get chrome version or not installed");
            }

            var chromeBrowserVersion = VersionHelper.GetVersionWithoutRevision(rawChromeBrowserVersion);

            var url = ExactReleaseVersionPatternUrl.Replace("<version>", chromeBrowserVersion);
            Version chromeVersion = new Version(chromeBrowserVersion);

            if (chromeVersion >= new Version(115, 0, 0))
            {
                isHigher115Version = true;
                myRawChromeBrowserVersion = rawChromeBrowserVersion;
                return rawChromeBrowserVersion;
               // url = "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/" + rawChromeBrowserVersion + "/win32/chromedriver-win32.zip";
            }

            return GetLatestVersion(url);
        }

        private string GetRawBrowserVersion()
        {
#if NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RegistryHelper.GetInstalledBrowserVersionOsx("Google Chrome", "--version");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RegistryHelper.GetInstalledBrowserVersionLinux(
                    "google-chrome", "--product-version",
                    "chromium", "--version",
                    "chromium-browser", "--version");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RegistryHelper.GetInstalledBrowserVersionWin("chrome.exe");
            }

            throw new PlatformNotSupportedException("Your operating system is not supported");
#else
            return RegistryHelper.GetInstalledBrowserVersionWin("chrome.exe");
#endif
        }

    }
}
