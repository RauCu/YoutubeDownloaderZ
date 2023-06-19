using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using WindowsInput;
using WindowsInput.Native;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Core.Utils;

public static class Http
{

    public static string getVideoID(IVideo? video)
    {
        if (video == null) { return ""; }
        try
        {
            OtherVideo otherVideo = (OtherVideo)video;
            Console.WriteLine("Conversion succeeded.");
            return otherVideo.otherId;
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("Conversion failed.");
            return video.Id.Value;
        }
    }

    public static bool isOtherVideo(IVideo? video)
    {
        if (video == null) { return false; }
        try
        {
            OtherVideo otherVideo = (OtherVideo)video;
            Console.WriteLine("Conversion succeeded.");
            return true;
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("Conversion failed.");
            return false;
        }
    }

    public static string RemoveTitle(String fileName)
    {
        int start = fileName.IndexOf("]-[") + "]-[".Length;
        int end = fileName.LastIndexOf("]-[");
        return fileName.Remove(start - 2, end - start + 3);
    }

    public static string ReplaceTitleByID(String fileName)
    {
        int start = fileName.IndexOf("]-[") + "]-[".Length;
        int end = fileName.LastIndexOf("]-[");
        string titleRmoved = fileName.Remove(start - 2, end - start + 3);

        string[] parts = titleRmoved.Split("]-[");
        string fileNameWithID = parts[0] + "]-[" + "GJW" + "]-[" + parts[1];
        return fileNameWithID;
    }
    public static HttpClient Client { get; } = new()
    {
        DefaultRequestHeaders =
        {
            // Required by some of the services we're using
            UserAgent =
            {
                new ProductInfoHeaderValue(
                    "YoutubeDownloader",
                    typeof(Http).Assembly.GetName().Version?.ToString(3)
                )
            }
        }
    };
    public static IWebDriver SignInGJW(string email, string pass, out bool login_success)
    {
        login_success = false;
        IWebDriver? driver = GetDriver();
        if (driver != null)
        {
            driver.Navigate().GoToUrl("https://studio.ganjing.com");
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));
            driver.SwitchTo().Frame("gjw_sso_page");
            string emailCSSSelector = "input[placeholder='Email address*']";
            // wait maximum 10 seconds
            wait.Until(driver => driver.FindElement(By.CssSelector(emailCSSSelector)));

            IWebElement elementTxtBoxEmail = driver.FindElement(By.CssSelector(emailCSSSelector));
            elementTxtBoxEmail.SendKeys(email);

            IWebElement elementTxtBoxPass = driver.FindElement(By.CssSelector("input[placeholder='Password*']"));
            elementTxtBoxPass.SendKeys(pass);
            elementTxtBoxPass.Submit();

            int MAX_RETRY = 15;
            int retry_count = 0;

            while (true)
            {
                if (!driver.PageSource.Contains("Don't have an account?"))
                {
                    login_success = true;
                    driver.SwitchTo().DefaultContent();
                    break;
                }
                else if (driver.PageSource.Contains("Email or password is incorrect."))
                {
                    login_success = false;
                    break;
                }
                else
                {
                    if (retry_count >= MAX_RETRY)
                    {
                        login_success = false;
                        break;
                    }
                    else
                    {
                        retry_count++;
                        Thread.Sleep(1000);
                    }
                }
            }
        }
#pragma warning disable CS8603 // Possible null reference return.
        return driver;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public static void openVideoTab(IWebDriver driver, bool isShortVideo)
    {
        int maxLenErrorMsg = 400;
        if (driver != null)
        {
            WebDriverWait wait2Second = new WebDriverWait(driver, TimeSpan.FromSeconds(2));

            string videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[1]";

            bool clickVideoTabSuccess = false;
            int MAX_TRY_VIDEO_TAB = 15;
            for (int i = 0; i <= MAX_TRY_VIDEO_TAB && clickVideoTabSuccess == false; i++)
            {
                try
                {
                    wait2Second.Until(driver => driver.FindElement(By.XPath(videoTab)));
                    IWebElement elementVideoTab = driver.FindElement(By.XPath(videoTab));
                    elementVideoTab.Click();
                    clickVideoTabSuccess = true;
                }
                catch (Exception ex)
                {
                    if (!isShortVideo)
                    {
                        if (i % 2 != 0)
                        {
                            videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[1]";
                        }
                        else
                        {
                            videoTab = "/html/body/div[2]/div[2]/div/div[1]/div/div/div/button[1]";
                        }
                    }
                    else
                    {
                        if (i % 2 != 0)
                        {
                            videoTab = "/html/body/div[2]/div[2]/div/div[1]/div/div/div/button[1]";
                        }
                        else
                        {
                            videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[2]";
                        }
                    }

                    if (i == MAX_TRY_VIDEO_TAB)
                    {
                        string msgError = "Error on: elementVideoTab: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }


    static int count = 0;
    static bool testEnabled = false;
    public static bool UploadVideo(IWebDriver driver, bool isShortVideo, string path, string title, string category)
    {
        count++;
        bool result = false;
        int maxLenErrorMsg = 400;
        if (driver != null)
        {
            driver.Manage().Window.Maximize();
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            WebDriverWait wait1Second = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
            WebDriverWait wait2Second = new WebDriverWait(driver, TimeSpan.FromSeconds(2));

            string videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[1]";
            if (isShortVideo)
            {
                videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[2]";
            }
            bool clickVideoTabSuccess = false;
            int MAX_TRY_VIDEO_TAB = 15;
            for (int i = 0; i <= MAX_TRY_VIDEO_TAB && clickVideoTabSuccess == false; i++)
            {
                try
                {
                    wait2Second.Until(driver => driver.FindElement(By.XPath(videoTab)));
                    IWebElement elementVideoTab = driver.FindElement(By.XPath(videoTab));
                    elementVideoTab.Click();
                    clickVideoTabSuccess = true;
                }
                catch (Exception ex)
                {
                    if (!isShortVideo)
                    {
                        if (i % 2 != 0)
                        {
                            videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[1]";
                        }
                        else
                        {
                            videoTab = "/html/body/div[2]/div[2]/div/div[1]/div/div/div/button[1]";
                        }
                    }
                    else
                    {
                        if (i % 2 != 0)
                        {
                            videoTab = "/html/body/div[2]/div[2]/div/div[1]/div/div/div/button[1]";
                        }
                        else
                        {
                            videoTab = "/html/body/div[2]/div[3]/div/div[1]/div/div/div/button[2]";
                        }
                    }

                    if (i == MAX_TRY_VIDEO_TAB)
                    {
                        string msgError = "Error on: elementVideoTab: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                }
                Thread.Sleep(1000);
            }

            string uploadVideoXpath = "//*[@id=\"table-videos\"]/table/tbody[1]/tr/td[2]/div/button";

            if (isShortVideo)
            {
                uploadVideoXpath = "//*[@id=\"table-shorts\"]/table/tbody[1]/tr/td[2]/div/button";
            }
            if (count == 2 && testEnabled)
            {
                uploadVideoXpath = "/html/body/div[2]/div[3]/div/div[300]/div[2]/div/table/tbody[1]/tr/td[2]/div/button";
            }

            bool clickUploadVideoSuccess = false;
            int MAX_TRY_UPLOAD_VIDEO_BTN = 10;
            for (int i = 0; i <= MAX_TRY_UPLOAD_VIDEO_BTN && clickUploadVideoSuccess == false; i++)
            {
                try
                {
                    wait2Second.Until(driver => driver.FindElement(By.XPath(uploadVideoXpath)));
                    IWebElement elementBtnUploadVideo = driver.FindElement(By.XPath(uploadVideoXpath));
                    elementBtnUploadVideo.Click();
                    clickUploadVideoSuccess = true;
                }
                catch (Exception ex)
                {
                    if (i == MAX_TRY_UPLOAD_VIDEO_BTN)
                    {
                        //sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                        string msgError = "Error on: elementBtnUploadVideo: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                }
            }
            Thread.Sleep(2000);

            string selectFileBtnXpath = "//button[normalize-space() = 'Select File']";
            if (isShortVideo)
            {
                selectFileBtnXpath = "//button[normalize-space() = 'Select Short']";
            }
            try
            {
                wait.Until(driver => driver.FindElement(By.XPath(selectFileBtnXpath)));
                IWebElement elementBtnSelectVideoFile = driver.FindElement(By.XPath(selectFileBtnXpath));
                elementBtnSelectVideoFile.Click();
            }
            catch (Exception ex)
            {
                string msgError = "Error on: elementBtnSelectVideoFile: " + ex.ToString();
                var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                Console.WriteLine(msgError);
                throw new Exception(first100Chars);//propage this error
            }
            // select video
            Thread.Sleep(4000);
            InputSimulator sim = new InputSimulator();
            //System.Windows.Clipboard.SetText(path);
            sim.Keyboard.TextEntry(path);
            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            // select 720
            string select720BtnXpath = "/html/body/div[4]/div[3]/div/div[3]/button[2]";

            bool btn720Success = false;
            int MAX_TRY_720 = 5;
            for (int i = 0; i <= MAX_TRY_720 && btn720Success == false; i++)
            {
                try
                {
                    wait1Second.Until(driver => driver.FindElement(By.XPath(select720BtnXpath)));
                    IWebElement elementBtnSelect720 = driver.FindElement(By.XPath(select720BtnXpath));
                    elementBtnSelect720.Click();
                    btn720Success = true;
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementBtnSelect720: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    //throw new Exception(first100Chars);//propage this error	
                }
            }
            Thread.Sleep(1000);

            // select thumbnail
            //string selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[1]/div/div[2]/div/div/div/div[1]/div/div/div/label/button";
            string selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/div[1]/div[2]/div/div/div/label/button";
            if (!isShortVideo)
            {
                //selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[1]/div/div/div/img";
                bool uploadThumbnailSuccess = false;
                int MAX_TRY_THUMBNAIL = 10;
                for (int i = 0; i <= MAX_TRY_THUMBNAIL && uploadThumbnailSuccess == false; i++)
                {
                    try
                    {
                        wait2Second.Until(driver => driver.FindElement(By.XPath(selectThumnailBtnXpath)));
                        IWebElement elementBtnSelectThumnail = driver.FindElement(By.XPath(selectThumnailBtnXpath));
                        elementBtnSelectThumnail.Click();
                        uploadThumbnailSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        if (i == MAX_TRY_THUMBNAIL)
                        {
                            string msgError = "Error on: elementBtnSelectThumnail: " + ex.ToString();
                            var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                            Console.WriteLine(msgError);
                            throw new Exception(first100Chars);//propage this error
                        }
                    }
                }
                Thread.Sleep(4000);
                //System.Windows.Clipboard.SetText(System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg");
                sim.Keyboard.TextEntry(Http.RemoveTitle(System.IO.Path.GetDirectoryName(path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg"));
                //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }

            // title
            Thread.Sleep(1000);
            string titleXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/div[1]/div/div/div";
            //titleXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[1]/div/div/div[1]";
            if (isShortVideo)
            {
                titleXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[1]/div/div/div";
            }

            bool copyTitleSuccess = false;
            int MAX_TRY_TITLE = 10;
            for (int i = 0; i <= MAX_TRY_TITLE && copyTitleSuccess == false; i++)
            {
                try
                {
                    wait2Second.Until(driver => driver.FindElement(By.XPath(titleXpath)));
                    IWebElement titleElement = driver.FindElement(By.XPath(titleXpath));
                    titleElement.Click();
                    copyTitleSuccess = true;

                }
                catch (Exception ex)
                {
                    if (i == MAX_TRY_TITLE)
                    {
                        string msgError = "Error on: titleElement: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                }
            }

            Thread.Sleep(2000);
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
            string newtitle = title.Substring(0, Math.Min(title.Length, 100 /*max 100 characters*/));
            /*newtitle = newtitle.Trim();
            string[] parts = newtitle.Split(" ");
            parts[parts.Length - 1] = parts[parts.Length - 1].Replace("#", "_");
            newtitle = String.Join(" ", parts);*/
            WindowsClipboard.SetText(newtitle);
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            //sim.Keyboard.TextEntry(newtitle);


            // description; 
            // cần click vào ô description để ẩn đi khung gợi ý hashtag nếu có
            //Thread.Sleep(1000);

            //string descriptionXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[2]/div/div/div[1]";
            string descriptionXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/div[2]/div/div/div";
            if (isShortVideo)
            {
                descriptionXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[2]/div/div/div[1]";
            }

            try
            {
                wait2Second.Until(driver => driver.FindElement(By.XPath(descriptionXpath)));
                IWebElement descriptionElement = driver.FindElement(By.XPath(descriptionXpath));
                Thread.Sleep(3000);
                descriptionElement.Click();

            }
            catch (Exception ex)
            {
                string msgError = "Error on: descriptionElement: " + ex.ToString();
                var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                Console.WriteLine(msgError);

                //
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                //throw new Exception(first100Chars);//propage this error
            }


            Thread.Sleep(1000);


            if (!isShortVideo)
            {
                string descriptionLabelXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/div[2]/div/label";
                try
                {
                    wait2Second.Until(driver => driver.FindElement(By.XPath(descriptionLabelXpath)));
                    IWebElement descriptionLabelElement = driver.FindElement(By.XPath(descriptionLabelXpath));
                    Thread.Sleep(3000);
                    descriptionLabelElement.Click();
                    sim.Keyboard.KeyPress(VirtualKeyCode.END);

                }
                catch (Exception ex)
                {
                    string msgError = "Error on: descriptionLabelElement: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);

                    //
                    sim.Keyboard.KeyPress(VirtualKeyCode.END);
                    //throw new Exception(first100Chars);//propage this error
                }
                // category
                Thread.Sleep(1000);
                //string categoryXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[3]/div/div[1]/div/div/input";
                string categoryXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div[1]/div/input";
                wait.Until(driver => driver.FindElement(By.XPath(categoryXpath)));
                IWebElement categoryElement = driver.FindElement(By.XPath(categoryXpath));
                string selectedCategory = categoryElement.GetAttribute("value");
                if (selectedCategory.Equals(""))
                {
                    categoryElement.SendKeys(category);
                }
                // next button
                Thread.Sleep(1000);
                string selectNextBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[3]/button[2]";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(selectNextBtnXpath)));
                    IWebElement elementBtnNext = driver.FindElement(By.XPath(selectNextBtnXpath));
                    Thread.Sleep(1000);
                    elementBtnNext.Click();
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementBtnNext: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
                // next2 button
                Thread.Sleep(1000);
                string selectNext2BtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[3]/button[2]";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(selectNext2BtnXpath)));
                    IWebElement elementBtnNext2 = driver.FindElement(By.XPath(selectNext2BtnXpath));
                    Thread.Sleep(1000);
                    elementBtnNext2.Click();
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementBtnNext2: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
                // Done button
                Thread.Sleep(1000);
                string selectDoneBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[3]/button[2]";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(selectDoneBtnXpath)));
                    IWebElement elementBtnDone = driver.FindElement(By.XPath(selectDoneBtnXpath));
                    Thread.Sleep(1000);
                    elementBtnDone.Click();
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementBtnDone: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
            }
            else
            {
                // Publish button
                Thread.Sleep(1000);
                string selectPublishBtnXpath = "/html/body/div[3]/div[3]/div/div/div[2]/button[2]";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(selectPublishBtnXpath)));
                    IWebElement elementBtnPublish = driver.FindElement(By.XPath(selectPublishBtnXpath));
                    Thread.Sleep(1000);
                    elementBtnPublish.Click();
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementBtnPublish: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
            }
            result = true;
        }
        return result;
    }
    public static IWebDriver GetDriver()
    {
        IWebDriver? driver = null;

        try
        {
            bool edgeInstalled = false;
            bool chromeInstalled = false;
            bool internetExplorerInstalled = false;
            try
            {
                foreach (Browser? browser in GetAllInstalledBrowsers.GetBrowsers())
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}: \n\tPath: {1} \n\tVersion: {2} \n\tIcon: {3}", browser.Name, browser.Path, browser.Version, browser.IconPath));
                    if (browser.Name!.Equals("Microsoft Edge"))
                    {
                        edgeInstalled = true;
                    }
                    else if (browser.Name!.Equals("Google Chrome"))
                    {
                        chromeInstalled = true;
                    }
                    else if (browser.Name!.Equals("Internet Explorer"))
                    {
                        internetExplorerInstalled = true;
                    }
                }
            }
            catch (Exception)
            {
                // default
                edgeInstalled = true;
            }

            EdgeDriverService? edgeDriverService = null;
            ChromeDriverService? chromeDriverService = null;
            InternetExplorerDriverService? internetExplorerDriverService = null;
            if (chromeInstalled)
            {
                // https://www.nuget.org/packages/WebDriverManager/
                new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

                // hide black windows
                chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;
                //
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--ignore-certificate-errors");
                // Open Chrome
                driver = new ChromeDriver(chromeDriverService, options);
            }
            else if (edgeInstalled)
            {
                // https://www.nuget.org/packages/WebDriverManager/
                new DriverManager().SetUpDriver(new EdgeConfig(), VersionResolveStrategy.MatchingBrowser);

                // hide black windows
                edgeDriverService = EdgeDriverService.CreateDefaultService();
                edgeDriverService.HideCommandPromptWindow = true;
                //
                EdgeOptions options = new EdgeOptions();
                options.AddArgument("--ignore-certificate-errors");

                // Open MS Edge
                driver = new EdgeDriver(edgeDriverService, options);
            }
            else if (internetExplorerInstalled)
            {
                // https://www.nuget.org/packages/WebDriverManager/
                new DriverManager().SetUpDriver(new InternetExplorerConfig(), VersionResolveStrategy.MatchingBrowser);

                // hide black windows
                internetExplorerDriverService = InternetExplorerDriverService.CreateDefaultService();
                internetExplorerDriverService.HideCommandPromptWindow = true;

                // Open InternetExplorer
                driver = new InternetExplorerDriver(internetExplorerDriverService);
            }
        }

        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {

        }
        return driver!;
    }

    // https://social.msdn.microsoft.com/Forums/sqlserver/en-US/42650aa1-abd8-48d5-97e3-801414e936c8/get-a-list-of-all-browsers-installed-and-their-versions-from-remote-desktop?forum=csharpgeneral
    class GetAllInstalledBrowsers
    {
        /*
        static void Main(string[] args)
        {
            foreach (Browser browser in GetBrowsers())
            {
                Console.WriteLine(string.Format("{0}: \n\tPath: {1} \n\tVersion: {2} \n\tIcon: {3}", browser.Name, browser.Path, browser.Version, browser.IconPath));
            }
            Console.ReadKey();
        }
        */
        internal static String StripQuotes(String s)
        {
            if (s.EndsWith("\"") && s.StartsWith("\""))
            {
                return s.Substring(1, s.Length - 2);
            }
            else
            {
                return s;
            }
        }
        public static List<Browser> GetBrowsers()
        {
            RegistryKey browserKeys;
            //on 64bit the browsers are in a different location
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");
            if (browserKeys == null)
            {
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string[] browserNames = browserKeys.GetSubKeyNames();
            var browsers = new List<Browser>();
            for (int i = 0; i < browserNames.Length; i++)
            {
                RegistryKey browserKey = browserKeys.OpenSubKey(browserNames[i]);
                string name = (string)browserKey.GetValue(null);
                if (name.Equals("Microsoft Edge") ||
                    name.Equals("Google Chrome") ||
                    name.Equals("Internet Explorer"))
                {
                    Browser browser = new Browser();
                    browser.Name = name;
                    RegistryKey browserKeyPath = browserKey.OpenSubKey(@"shell\open\command");
#pragma warning disable CS8604 // Possible null reference argument.
                    browser.Path = StripQuotes(browserKeyPath.GetValue(null).ToString());
                    RegistryKey browserIconPath = browserKey.OpenSubKey(@"DefaultIcon");
                    browser.IconPath = StripQuotes(browserIconPath.GetValue(null).ToString());
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    browsers.Add(browser);
                    if (browser.Path != null)
                        browser.Version = FileVersionInfo.GetVersionInfo(browser.Path).FileVersion;
                    else
                        browser.Version = "unknown";
                }
            }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            return browsers;
        }
    }

    class Browser
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? IconPath { get; set; }
        public string? Version { get; set; }
    }

}