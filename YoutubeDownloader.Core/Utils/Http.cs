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

namespace YoutubeDownloader.Core.Utils;

public static class Http
{
    public static string RemoveTitle(String input)
    {
        int start = input.IndexOf("]-[") + "]-[".Length;
        int end = input.LastIndexOf("]-[");
        return input.Remove(start - 2, end - start + 3);
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
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
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

    public static bool UploadVideo(IWebDriver driver, bool isShortVideo, string path, string title, string category)
    {
        bool result = false;
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
            for (int i = 0; i < 10 && clickVideoTabSuccess == false; i++)
            {
                try
                {
                    wait1Second.Until(driver => driver.FindElement(By.XPath(videoTab)));
                    IWebElement elementVideoTab = driver.FindElement(By.XPath(videoTab));
                    elementVideoTab.Click();
                    clickVideoTabSuccess = true;
                }
                catch (Exception)
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
                }
                Thread.Sleep(1000);
            }

            string uploadVideoXpath = "/html/body/div[2]/div[3]/div/div[2]/div[2]/div/table/tbody[1]/tr/td[2]/div/button";
            if (isShortVideo)
            {
                uploadVideoXpath = "/html/body/div[2]/div[3]/div/div[3]/div[2]/div/table/tbody[1]/tr/td[2]/div/button";
            }

            bool clickuploadVideoSuccess = false;
            int MAX_TRY = 5;
            for (int i = 0; i < MAX_TRY && clickuploadVideoSuccess == false; i++)
            {
                if (i == MAX_TRY - 1)
                { // last try
                    ((IJavaScriptExecutor)driver).ExecuteScript("document.body.style.transform='scale(0.8)';");
                }
                try
                {
                    wait1Second.Until(driver => driver.FindElement(By.XPath(uploadVideoXpath)));
                    IWebElement elementBtnUploadVideo = driver.FindElement(By.XPath(uploadVideoXpath));
                    elementBtnUploadVideo.Click();
                    clickuploadVideoSuccess = true;
                }
                catch (Exception)
                {
                    if (i == MAX_TRY - 1)
                    {
                        //sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                        Console.WriteLine("Error on: elementBtnSelectThumnail");
                        throw;//propage this error
                    }
                }
            }
            Thread.Sleep(1000);

            string selectFileBtnXpath = "//button[normalize-space() = 'Select File']";
            if (isShortVideo)
            {
                selectFileBtnXpath = "//button[normalize-space() = 'Select Short']";
            }
            wait.Until(driver => driver.FindElement(By.XPath(selectFileBtnXpath)));
            IWebElement elementBtnSelectFile = driver.FindElement(By.XPath(selectFileBtnXpath));
            elementBtnSelectFile.Click();

            // select video
            Thread.Sleep(2000);
            InputSimulator sim = new InputSimulator();
            //System.Windows.Clipboard.SetText(path);
            sim.Keyboard.TextEntry(path);
            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            // select 720
            try
            {
                WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                string select720BtnXpath = "/html/body/div[4]/div[3]/div/div[3]/button[2]";
                wait2.Until(driver => driver.FindElement(By.XPath(select720BtnXpath)));
                IWebElement elementBtnSelect720 = driver.FindElement(By.XPath(select720BtnXpath));
                elementBtnSelect720.Click();
                Thread.Sleep(2000);
            }
            catch (Exception)
            {
            }

            // select thumbnail
            string selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[1]/div/div[2]/div/div/div/div[1]/div/div/div/label/button";
            if (!isShortVideo)
            {
                //selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[1]/div/div/div/img";
                bool uploadThumbnailSuccess = false;
                int MAX_TRY_1 = 5;
                for (int i = 0; i < MAX_TRY_1 && uploadThumbnailSuccess == false; i++)
                {
                    try
                    {
                        wait2Second.Until(driver => driver.FindElement(By.XPath(selectThumnailBtnXpath)));
                        IWebElement elementBtnSelectThumnail = driver.FindElement(By.XPath(selectThumnailBtnXpath));
                        elementBtnSelectThumnail.Click();
                        uploadThumbnailSuccess = true;
                    }
                    catch (Exception)
                    {
                        if (i == MAX_TRY_1 - 1)
                        {
                            //sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                            Console.WriteLine("Error on: elementBtnSelectThumnail");
                            throw;//propage this error
                        }
                    }
                }
                Thread.Sleep(1000);
                //System.Windows.Clipboard.SetText(System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg");
                sim.Keyboard.TextEntry(Http.RemoveTitle(System.IO.Path.GetDirectoryName(path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg"));
                //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }

            // title
            Thread.Sleep(1000);
            string titleXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[1]/div/div/div/input";
            if (isShortVideo)
            {
                titleXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[1]/div/div/div";
            }

            wait.Until(driver => driver.FindElement(By.XPath(titleXpath)));
            IWebElement titleElement = driver.FindElement(By.XPath(titleXpath));
            titleElement.Click();
            Thread.Sleep(1000);
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
            sim.Keyboard.TextEntry(title.Substring(0, Math.Min(title.Length, 100 /*max 100 characters*/)));

            if (!isShortVideo)
            {
                // category
                Thread.Sleep(1000);
                string categoryXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[3]/div/div[1]/div/div/input";
                wait.Until(driver => driver.FindElement(By.XPath(categoryXpath)));
                IWebElement categoryElement = driver.FindElement(By.XPath(categoryXpath));
                string selectedCategory = categoryElement.GetAttribute("value");
                if (selectedCategory.Equals(""))
                {
                    categoryElement.SendKeys(category);
                }
            }
            // save button
            Thread.Sleep(1000);
            string selectSaveBtnXpath = "/html/body/div[3]/div[3]/div/div/div[2]/button[2]";
            wait.Until(driver => driver.FindElement(By.XPath(selectSaveBtnXpath)));
            IWebElement elementBtnSave = driver.FindElement(By.XPath(selectSaveBtnXpath));
            elementBtnSave.Click();

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