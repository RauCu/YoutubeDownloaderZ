using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using AngleSharp.Text;
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
    public static string[] supportedLanguages = {
                    "English",
                    "中文",
                    "日本",
                    "한국어",
                    "Deutsch",
                    "Español",
                    "Français",
                    "Bahasa Indonesia",
                    "Italiano",
                    "Русский",
                    "Tiếng Việt",
                    "Others"
                };

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

    public static string language = "";
    public static string GetLangluageChannell(IWebDriver driver)
    {
        string language = "";
        if (driver != null)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            int maxLenErrorMsg = 400;

            // Channel Settings menu
            string channelSettingMenuBtn = "/html/body/div[2]/div[1]/div/div[2]/div[7]";
                   channelSettingMenuBtn = "/html/body/div[2]/div[1]/div/div[2]/div[6]/div[2]";

            bool clickChannelSettingMenuBtnSuccess = false;
            int MAX_TRY_CLICK_BTN = 15;
            for (int i = 0; i <= MAX_TRY_CLICK_BTN && clickChannelSettingMenuBtnSuccess == false; i++)
            {
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(channelSettingMenuBtn)));
                    IWebElement elementChannelSettingMenuBtn = driver.FindElement(By.XPath(channelSettingMenuBtn));
                    elementChannelSettingMenuBtn.Click();
                    clickChannelSettingMenuBtnSuccess = true;
                }
                catch (Exception ex)
                {
                    if (i == MAX_TRY_CLICK_BTN)
                    {
                        string msgError = "Error on: elementChannelSettingMenuBtn: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        //throw new Exception(first100Chars);//propage this error
                    }
                }
                Thread.Sleep(1000);
            }
            //

            if (clickChannelSettingMenuBtnSuccess)
            {
                string channelNameStringXpath = "/html/body/div[2]/div[3]/div/div[3]/div[2]/div[1]/div[2]/div[1]/label";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(channelNameStringXpath)));
                    IWebElement elementChannelNameText = driver.FindElement(By.XPath(channelNameStringXpath));
                    elementChannelNameText.Click();
                }
                catch (Exception)
                {
                }
                InputSimulator sim = new InputSimulator();
               

                sim.Keyboard.KeyPress(VirtualKeyCode.END);
                // Language TextField
                string languageTextField = "/html/body/div[2]/div[3]/div/div[3]/div[2]/div[1]/div[2]/div[3]/div/div";

                bool languageTextFieldFound= false;

                for (int i = 0; i <= MAX_TRY_CLICK_BTN && languageTextFieldFound == false; i++)
                {
                    try
                    {
                        wait.Until(driver => driver.FindElement(By.XPath(languageTextField)));
                        IWebElement elementLanguageTextField = driver.FindElement(By.XPath(languageTextField));
                        language = elementLanguageTextField.GetAttribute("innerHTML");
                        languageTextFieldFound = true;
                    }
                    catch (Exception ex)
                    {
                        if (i == MAX_TRY_CLICK_BTN)
                        {
                            string msgError = "Error on: elementLanguageTextField: " + ex.ToString();
                            var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                            Console.WriteLine(msgError);
                            //throw new Exception(first100Chars);//propage this error
                        }
                    }
                    Thread.Sleep(1000);
                }

            }

        }

        return language;
    }

    public static IWebDriver SignInGJW(string email, string pass, out bool login_success)
    {
        login_success = false;
        IWebDriver? driver = GetDriver();
        if (driver != null)
        {
            driver.Navigate().GoToUrl("https://www.ganjingworld.com/");
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            driver.Manage().Window.Maximize();

            //
            int maxLenErrorMsg = 400;
            string signInBtn = "/html/body/div[1]/div/header/div/div[1]/div/button[2]/span";
                    signInBtn = "/html/body/div/div/header/div/div[3]/div/button[2]/span";
                    signInBtn = "/html/body/div[1]/div/header/div/div[3]/div/button[1]/span";

            bool clickSignInBtnSuccess = false;
            int MAX_TRY_SIGNIN_BTN = 15;
            for (int i = 0; i <= MAX_TRY_SIGNIN_BTN && clickSignInBtnSuccess == false; i++)
            {
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(signInBtn)));
                    IWebElement elementSignInBtn = driver.FindElement(By.XPath(signInBtn));
                    elementSignInBtn.Click();
                    clickSignInBtnSuccess = true;
                }
                catch (Exception ex)
                {
                    if (i == MAX_TRY_SIGNIN_BTN)
                    {
                        string msgError = "Error on: signInBtn: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                }
                Thread.Sleep(1000);

            }
            //

            try
            {
                string emailCSSSelector = "input[placeholder='Email address*']";
                // wait maximum 10 seconds
                wait.Until(driver => driver.FindElement(By.CssSelector(emailCSSSelector)));

                IWebElement elementTxtBoxEmail = driver.FindElement(By.CssSelector(emailCSSSelector));
                elementTxtBoxEmail.SendKeys(email);

                IWebElement elementTxtBoxPass = driver.FindElement(By.CssSelector("input[placeholder='Password*']"));
                elementTxtBoxPass.SendKeys(pass);
                elementTxtBoxPass.Submit();
            }catch(Exception)
            {
                string emailCSSSelector = "//*[@id=\"__next\"]/main/div/div[2]/div/div[1]/form/div[1]/div/input";
                // wait maximum 10 seconds
                wait.Until(driver => driver.FindElement(By.XPath(emailCSSSelector)));

                IWebElement elementTxtBoxEmail = driver.FindElement(By.XPath(emailCSSSelector));
                elementTxtBoxEmail.SendKeys(email);

                IWebElement elementTxtBoxPass = driver.FindElement(By.XPath("//*[@id=\"__next\"]/main/div/div[2]/div/div[1]/form/div[2]/div/input"));
                elementTxtBoxPass.SendKeys(pass);
                elementTxtBoxPass.Submit();
            }

            //driver.Navigate().GoToUrl("https://studio.ganjing.com");
            Thread.Sleep(5000);

            int MAX_RETRY = 5;
            int retry_count = 0;
            string userInfoXpath = "/html/body/div[1]/div/header/div/div[3]/div";
            bool clickUserInfoBtnSuccess = false;
            while (true)
            {
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(userInfoXpath)));
                    IWebElement elementUserInfoBtn = driver.FindElement(By.XPath(userInfoXpath));
                    elementUserInfoBtn.Click();
                    clickUserInfoBtnSuccess = true;
                }
                catch (Exception ex)
                {

                    string msgError = "Error on: elementUserInfoBtn: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    clickUserInfoBtnSuccess = false;
                }
                Thread.Sleep(500);

                if (clickUserInfoBtnSuccess)
                {
                    login_success = true;
                    driver.Navigate().GoToUrl("https://studio.ganjing.com");
                    Http.language = "";
                    Http.language = GetLangluageChannell(driver);

                    driver.Navigate().GoToUrl("https://studio.ganjing.com");
                    break;
                }
                else
                {
                    if (retry_count >= MAX_RETRY)
                    {
                        language = "";
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
    public static bool UploadVideo(IWebDriver driver, bool isShortVideo, string path, string title, bool scheduleEnabled, bool unlistedEnabled, int SelectedCategoryIndex,  string language)
    {
        count++;
        bool result = false;
        int maxLenErrorMsg = 400;
        if (driver != null)
        {
            driver.Manage().Window.Maximize();
            Thread.Sleep(1000);
            InputSimulator sim = new InputSimulator();
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.NUMPAD0);
            Thread.Sleep(1000);
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.SUBTRACT);
            Thread.Sleep(1000);
            sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);

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

            string uploadVideoXpath = "//*[@id=\"table-videos\"]/table/tbody[1]/tr/td/div/button";
            //uploadVideoXpath = "/html/body/div[2]/div[3]/div/div[2]/div[3]/div/table/tbody[1]/tr/td[2]/div/button";

            if (isShortVideo)
            {
                uploadVideoXpath = "//*[@id=\"table-shorts\"]/table/tbody[1]/tr/td/div/button";
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

            // select Language (only for New Channel, e.g: Minneapolis News
            // https://www.ganjingworld.com/channel/1ff02d08oli5W1X7GnCzOyKs11500c )
            selectLanguageForNewChannel(driver, sim, wait2Second, isShortVideo, supportedLanguages);

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
            Thread.Sleep(5000);
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
            string selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[1]/div[2]/div/div/div/label/button";
                   selectThumnailBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[1]/div[2]/div/div/label/button";

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
                Thread.Sleep(5000);
                //System.Windows.Clipboard.SetText(System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg");
                sim.Keyboard.TextEntry(Http.RemoveTitle(System.IO.Path.GetDirectoryName(path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(path) + ".jpg"));
                //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }

            // title
            Thread.Sleep(1000);
            string titleXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[2]/div/div[1]/div/div/div/div/input";
                // titleXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[2]/div/div[1]/div/div/div/div/input";
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
            //WindowsClipboard.SetText(newtitle);
            //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            sim.Keyboard.TextEntry(newtitle);


            // description; 
            // cần click vào ô description để ẩn đi khung gợi ý hashtag nếu có
            //Thread.Sleep(1000);

            //string descriptionXpath = "/html/body/div[3]/div[3]/div/div/div[1]/div/div[2]/div/div[2]/div/div/div[1]";
            string descriptionXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[1]/div/div[2]/div/div/div";

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
                string descriptionLabelXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[1]/div/div[2]/div/label";
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

                // Category
                selectCategory(driver, sim, wait, isShortVideo, SelectedCategoryIndex);

                // Language
                selectLanguage(driver, sim, wait2Second, isShortVideo, supportedLanguages);
                sim.Keyboard.KeyPress(VirtualKeyCode.END);

                // next button
                Thread.Sleep(1000);
                sim.Keyboard.KeyPress(VirtualKeyCode.END);
                string selectNextBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/button[2]";
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
                string selectNext2BtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/button[2]";
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
                //

                selectUnlisted(driver, sim, wait2Second, isShortVideo, unlistedEnabled);
                //

                if (scheduleEnabled)
                {
                    
                    Thread.Sleep(1000);
                    string scheduleEnabledBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[4]/div/div[3]/div[1]/span/span[1]/input";
                           scheduleEnabledBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[4]/div/div[2]/div[1]/span/span[1]/input";
                    try
                    {
                        wait.Until(driver => driver.FindElement(By.XPath(scheduleEnabledBtnXpath)));
                        IWebElement elementBtnScheduleEnabled = driver.FindElement(By.XPath(scheduleEnabledBtnXpath));
                        Thread.Sleep(1000);
                        elementBtnScheduleEnabled.Click();
                    }
                    catch (Exception ex)
                    {
                        string msgError = "Error on: scheduleEnabledBtnXpath: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                    //
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);                    
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(100);

                    /*
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(100);
                    */
                    /*for (int i = 0; i < 11; i++) // 11 giờ sau mới đăng
                    {
                        
                        sim.Keyboard.KeyPress(VirtualKeyCode.UP);
                        sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        Thread.Sleep(200);
                    }*/

                }
                // Done button
                Thread.Sleep(1000);
                string selectDoneBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[2]/button[2]";
                bool clickDoneBtnSuccess = false;
                int MAX_TRY_DONE = 3;
                for (int i = 0; i <= MAX_TRY_DONE && clickDoneBtnSuccess == false; i++)
                {

                    try
                    {
                        wait.Until(driver => driver.FindElement(By.XPath(selectDoneBtnXpath)));
                        IWebElement elementBtnDone = driver.FindElement(By.XPath(selectDoneBtnXpath));
                        Thread.Sleep(1000);
                        elementBtnDone.Click();
                        clickDoneBtnSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        if (i == MAX_TRY_DONE)
                        {
                            string msgError = "Error on: elementBtnDone: " + ex.ToString();
                            var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                            Console.WriteLine(msgError);
                            throw new Exception(first100Chars);//propage this error
                        }
                    }
                }
            }
            else
            {
                // Unlisted
                selectUnlisted(driver, sim, wait2Second, isShortVideo, unlistedEnabled);
                // Language
                selectLanguage(driver, sim, wait2Second, isShortVideo, supportedLanguages);

                //Category
                selectCategory(driver, sim, wait, isShortVideo, SelectedCategoryIndex);
                sim.Keyboard.KeyPress(VirtualKeyCode.END);

                if (scheduleEnabled)
                {
                    
                    Thread.Sleep(1000);
                    string scheduleEnabledBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div[1]/div[2]/div/div[3]/div/div[5]/div[1]/span/span[1]/input";
                           
                    try
                    {
                        wait.Until(driver => driver.FindElement(By.XPath(scheduleEnabledBtnXpath)));
                        IWebElement elementBtnScheduleEnabled = driver.FindElement(By.XPath(scheduleEnabledBtnXpath));
                        Thread.Sleep(1000);
                        elementBtnScheduleEnabled.Click();
                    }
                    catch (Exception ex)
                    {
                        string msgError = "Error on: scheduleEnabledBtnXpath: " + ex.ToString();
                        var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                        Console.WriteLine(msgError);
                        throw new Exception(first100Chars);//propage this error
                    }
                    //
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    Thread.Sleep(100);                    
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(100);

                }

                // Publish button
                Thread.Sleep(1000);
                string selectPublishBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div[2]/div/button[2]"; // PUBLISH
                //selectPublishBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div[2]/button[1]"; // CANCEL

                bool clickDoneBtnSuccess = false;
                int MAX_TRY_DONE = 1;
                for (int i = 0; i <= MAX_TRY_DONE && clickDoneBtnSuccess == false; i++)
                {
                    try
                    {
                        wait.Until(driver => driver.FindElement(By.XPath(selectPublishBtnXpath)));
                        IWebElement elementBtnPublish = driver.FindElement(By.XPath(selectPublishBtnXpath));
                        Thread.Sleep(1000);
                        elementBtnPublish.Click();
                        clickDoneBtnSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        if (i == MAX_TRY_DONE) {
                            string msgError = "Error on: elementBtnPublish: " + ex.ToString();
                            var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                            Console.WriteLine(msgError);
                            throw new Exception(first100Chars);//propage this error
                        }
                    }
                }
            }
            result = true;
        }
        return result;

        static void selectUnlisted(IWebDriver driver, InputSimulator sim, WebDriverWait wait, bool isShortVideo, bool unlistedEnabled)
        {
            if (!unlistedEnabled) return;
            int maxLenErrorMsg = 400;
            if (isShortVideo)
            {
                Thread.Sleep(1000);
                
                string unlistedXpath = "/html/body/div[3]/div[3]/div/div/div/div[1]/div[2]/div/div[3]/div/div[1]/div/div/div";

                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(unlistedXpath)));
                    IWebElement unlistedElement = driver.FindElement(By.XPath(unlistedXpath));

                    unlistedElement.Click();
                    for (int i = 0; i < 2; i++) // Unlisted
                    {
                        sim.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                        //Thread.Sleep(200);
                    }

                    Thread.Sleep(200);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: unlistedElement: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
            }
            else
            {
                Thread.Sleep(1000);
                string unlistedEnabledBtnXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[4]/div/div[1]/div/div/label[3]/span[2]/div";
                try
                {
                    wait.Until(driver => driver.FindElement(By.XPath(unlistedEnabledBtnXpath)));
                    IWebElement elementCheckBoxUnlistedEnabled = driver.FindElement(By.XPath(unlistedEnabledBtnXpath));
                    Thread.Sleep(1000);
                    elementCheckBoxUnlistedEnabled.Click();
                }
                catch (Exception ex)
                {
                    string msgError = "Error on: elementCheckBoxUnlistedEnabled: " + ex.ToString();
                    var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                    Console.WriteLine(msgError);
                    throw new Exception(first100Chars);//propage this error
                }
            }
        }
       
        static void selectLanguage(IWebDriver driver, InputSimulator sim, WebDriverWait wait, bool isShortVideo, string[] supportedLanguages)
        {
            // language
            Thread.Sleep(1000);
            string languageXpath = "/html/body/div[3]/div[3]/div/div/div/div[1]/div[2]/div/div[3]/div/div[2]/div/div/div";
            if (!isShortVideo)
            {
                languageXpath = "//*[@id=\"lang\"]/div";
               
            }
            int maxLenErrorMsg = 400;
            try
            {
                wait.Until(driver => driver.FindElement(By.XPath(languageXpath)));
                IWebElement languageElement = driver.FindElement(By.XPath(languageXpath));
                string selectedLanguage = languageElement.GetAttribute("innerHTML");
                if (selectedLanguage == null || selectedLanguage.Equals("") || selectedLanguage.Equals("<div style=\"color: var(--text-secondary);\">Select</div>") ||
                    (!selectedLanguage.Equals("") && !supportedLanguages.Contains(selectedLanguage)))
                {
                    languageElement.Click();
                    int languageIndex = 0;
                    for (int i = 0; i < supportedLanguages.Length; i++)
                    {
                        if (string.Equals(supportedLanguages.ElementAt(i), Http.language, StringComparison.OrdinalIgnoreCase))
                        {
                            languageIndex = i;
                            break;
                        }
                    }
                    //
                    for (int i = 0; i < languageIndex; i++)
                    {
                        sim.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                        Thread.Sleep(200);
                    }
                    Thread.Sleep(200);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                }
                //sim.Keyboard.KeyPress(VirtualKeyCode.END);
            }
            catch (Exception ex)
            {
                string msgError = "Error on: languageElement: " + ex.ToString();
                var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                Console.WriteLine(msgError);
                //throw new Exception(first100Chars);//propage this error
            }
        }

        static void selectLanguageForNewChannel(IWebDriver driver, InputSimulator sim, WebDriverWait wait, bool isShortVideo, string[] supportedLanguages)
        {
            if (isShortVideo)
            {
                return;
            }
            
            // language
            Thread.Sleep(1000);

            string languageXpath = "//*[@id=\"lang\"]/div";
                   languageXpath = "//*[@id=\"lang\"]";
            int maxLenErrorMsg = 400;
            try
            {
                wait.Until(driver => driver.FindElement(By.XPath(languageXpath)));
                IWebElement languageElement = driver.FindElement(By.XPath(languageXpath));
                string selectedLanguage = languageElement.GetAttribute("innerHTML");
                if (selectedLanguage == null || selectedLanguage.Equals("") || selectedLanguage.Equals("<div style=\"color: var(--text-secondary);\">Language</div>") ||
                    (!selectedLanguage.Equals("") && !supportedLanguages.Contains(selectedLanguage)))
                {
                    languageElement.Click();
                    int languageIndex = 0;
                    for (int i = 0; i < supportedLanguages.Length; i++)
                    {
                        if (string.Equals(supportedLanguages.ElementAt(i), Http.language, StringComparison.OrdinalIgnoreCase))
                        {
                            languageIndex = i;
                            break;
                        }
                    }
                    //
                    for (int i = 0; i < languageIndex; i++)
                    {
                        sim.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(200);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                }
                //sim.Keyboard.KeyPress(VirtualKeyCode.END);
            }
            catch (Exception ex)
            {
                string msgError = "Error on: languageElement: " + ex.ToString();
                var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                Console.WriteLine(msgError);
                //throw new Exception(first100Chars);//propage this error
            }
        }

        static void selectCategory(IWebDriver driver, InputSimulator sim, WebDriverWait wait, bool isShortVideo, int selectedCategoryIndex)
        {
            // language
            Thread.Sleep(1000);
            int maxLenErrorMsg = 400;
            string categoryXpath = "/html/body/div[3]/div[3]/div/div/div/div[1]/div[2]/div/div[3]/div/div[3]/div/div/div";
            if (!isShortVideo)
            {
                categoryXpath = "/html/body/div[3]/div[3]/div/div/div/div/div[1]/div[2]/div[2]/div/div[3]/div/div[1]/div";
                //"//*[@id=\"dialog_1691141263158\"]/div[3]/div/div/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div[1]/div";              
            }
            try
            {
                wait.Until(driver => driver.FindElement(By.XPath(categoryXpath)));
                IWebElement categoryElement = driver.FindElement(By.XPath(categoryXpath));

                categoryElement.Click();
                for (int i = 0; i < 50; i++) // reset
                {
                    sim.Keyboard.KeyPress(VirtualKeyCode.UP);
                    //Thread.Sleep(200);
                }
                for (int i = 0; i < selectedCategoryIndex ; i++) // New & Politics
                {
                    sim.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                    //Thread.Sleep(200);
                }

                Thread.Sleep(200);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            catch (Exception ex)
            {
                string msgError = "Error on: categoryElement: " + ex.ToString();
                var first100Chars = msgError.Length <= maxLenErrorMsg ? msgError : msgError.Substring(0, maxLenErrorMsg);
                Console.WriteLine(msgError);
                throw new Exception(first100Chars);//propage this error
            }
            
        }
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
            bool success = false;

            EdgeDriverService? edgeDriverService = null;
            ChromeDriverService? chromeDriverService = null;
            InternetExplorerDriverService? internetExplorerDriverService = null;
            try {
                if (chromeInstalled)
                {
                    // https://www.nuget.org/packages/WebDriverManager/
                    new DriverManager().SetUpDriver(new ChromeConfigOverride(), VersionResolveStrategy.MatchingBrowser);

                    // hide black windows
                    chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;
                    //
                    ChromeOptions options = new ChromeOptions();
                    options.AddArgument("--ignore-certificate-errors");
                    // Open Chrome
                    driver = new ChromeDriver(chromeDriverService, options);
                    success = true;
                }
            }
            catch (Exception)
            {

                success = false;
            }
            try
            {

                if (!success && edgeInstalled)
                {
                    // https://www.nuget.org/packages/WebDriverManager/
                    new DriverManager().SetUpDriver(new EdgeConfig(), VersionResolveStrategy.MatchingBrowser);

                    // hide black windows
                    edgeDriverService = EdgeDriverService.CreateDefaultService();
                    edgeDriverService.HideCommandPromptWindow = true;
                    //
                    EdgeOptions options = new EdgeOptions();
                    options.AddArgument("--ignore-certificate-errors");
                    options.AddArgument("--guest");

                    // Open MS Edge
                    driver = new EdgeDriver(edgeDriverService, options);
                    success = true;
                }
            }
            catch (Exception)
            {

                success = false;
            }
            try
            {
                if (!success && internetExplorerInstalled)
                {
                    // https://www.nuget.org/packages/WebDriverManager/
                    new DriverManager().SetUpDriver(new InternetExplorerConfig(), VersionResolveStrategy.MatchingBrowser);

                    // hide black windows
                    internetExplorerDriverService = InternetExplorerDriverService.CreateDefaultService();
                    internetExplorerDriverService.HideCommandPromptWindow = true;

                    // Open InternetExplorer
                    driver = new InternetExplorerDriver(internetExplorerDriverService);
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
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