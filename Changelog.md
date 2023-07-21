### v1.9.111 (21-Jul-2023)
Sửa lỗi không tự động đăng video được (lần 3)

### v1.9.110 (21-Jul-2023)
Sửa lỗi không tự động đăng video được, do trên Edge cần phải zoom nhỏ lại thì mới thấy nút đăng video

### v1.9.109 (21-Jul-2023)
Sửa lỗi không tự động đăng video được

### v1.9.108 (02-Jul-2023)
1. Khi lấy danh sách video của 1 kênh Youtube thì:
	- lấy thêm được các video ngắn (short)
	- lấy thêm được thời lượng video, hiện thị dưới dạng giờ:phút:giây, hoặc tổng giây (có thể sắp xếp theo cột thời lượng tính bằng giây)
2. Thêm tính năng hẹn 24 giờ sau mới đăng (dành cho video thường, còn video ngắn thì GJW ko hỗ trợ chức năng hẹn giờ đăng)

### v1.9.107 (28-Jun-2023)
- hỗ trợ lấy danh sách video kênh Youtube cho Windows 7

### v1.9.106 (27-Jun-2023)
- sửa lỗi không tự động đăng nhập GJW ở một số máy
- giảm kích thước của file YoutubeDownloader/LayDanhSachVideoKenhYT.exe

### v1.9.105 (24-Jun-2023)
bây giờ sau khi lấy danh sách video của 1 kênh Youtube thì:
- có thể mở file kết quả trực tiếp bằng Excel (không cần cài LibreOffice nữa)
- có thể sắp xếp theo lượt xem hoặc ngày đăng 

### v1.9.104 (21-Jun-2023)
- thêm chức năng lấy danh sách video của 1 kênh Youtube
- thêm đường dẫn đến trang lấy hình avatar và banner của 1 kênh Youtube (https://imageyoutube.com/?)

### v1.9.103 (19-Jun-2023)
- tăng thời gian chờ khi login

### v1.9.102 (18-Jun-2023)
- sửa lồi lấy sai tiêu đề của video instagram, đảo ngược thứ tự up video, bây giờ, video nào được tải trước thì sẽ được up trước

### v1.9.101 (04-Jun-2023)
- Sửa lỗi thỉnh thoảng tiêu đề video bị sai

### v1.9.100 (31-May-2023)
- Sửa lỗi không tự động đăng video ở trên máy tính có màn hình nhỏ

### v1.9.99 (30-May-2023)
- Cập nhật logic upload video vì GJW thay đổi giao diện

### v1.9.98 (5-May-2023)
- Đảm bảo là đang ở tab Video chứ không phải ở tab Short thì mới có thể thấy là đang Uploading hay đang Transcoding
- Bỏ bước notify khỏi CI pipeline

### v1.9.97 (4-May-2023)
- Sửa một số lỗi nhỏ

### v1.9.96 (4-May-2023)
- Không dùng tùy chọn EmbedThumbnail, vì nếu dùng thì sẽ cần file ffprobe.exe, nặng 77MB

### v1.9.95 (3-May-2023)
- Cập nhật lên YoutubeExplode v6.2.14 để sửa lỗi "Response status code does not indicate success: 403 (Forbidden)",
Closed https://github.com/Tyrrrz/YoutubeDownloader/issues/333
- Hỗ trợ tải video từ Facebook, Tiktok, pinterest, .... 
Danh sách đấy đủ các trang được hỗ trợ: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md

### v1.9.94 (30-April-2023)
- Fix lỗi các file trong thư mục "DownloadVideoFolder/tmp" không được xóa toàn bộ khi tải xong

### v1.9.93 (30-April-2023)
- hiện thông báo khi có bản mới lâu hơn (10 phút thay vì 5 giây như trước đây)

### v1.9.92 (30-April-2023)
- thay đổi thư mục temp từ C:\Users\ADMIN\AppData\Local\Temp sang "DownloadVideoFolder/tmp"

### v1.9.91 (29-April-2023)
- Fix lỗi không tải được video https://www.youtube.com/watch?v=HxjFpyzwkjo do phần mềm lưu 
ở đường dẫn có khoảng trắng

### v1.9.90 (29-April-2023)
- Fix lỗi không tải được video https://www.youtube.com/watch?v=HxjFpyzwkjo
- Sử dụng logic tải về như trước, khi có lỗi thì mới dùng yt-dl

### v1.9.89 (29-April-2023)
- Fix lỗi thông báo sai: không tải được những vẫn báo là tải thành công

### v1.9.88 (29-April-2023)
- Fix lỗi ko tải được video "Response status code does not indicate success: 403 (Forbidden)"

### v1.9.87 (13-April-2023)
- Fix lỗi ko up video lên được do thay đổi giao diện ở mục tiêu đề

### v1.9.86 (22-Mar-2023)
- Cho phép chọn thư mục chứa file mp4 đã tải về
- Bỏ qua video bị xóa trên Youtube

### v1.9.85 (21-Mar-2023)
- Fixed cannot press add video button for short video

### v1.9.84 (15-Mar-2023)
- Allow downloading video with the length longer than 1 hour

### v1.9.83 (13-Mar-2023)
- Fix bug: upload failed

### v1.9.82 (13-Mar-2023)
- Fix bug: upload failed

### v1.9.81 (11-Mar-2023)
- Fix bug: upload failed

### v1.9.80 (11-Mar-2023)
- Fix bug: upload failed

### v1.9.79 (10-Mar-2023)
- Fix bug: data of previous download should not saved to [0000]-[Database].dat when downloading to new directory
- Fix bug: when a dialog show, then browser is closed, if user click to button on dialog to close or show browser again ==> catch exception now
- When there is no video selected to upload, instead of showing "TẢI LÊN (0)", now showing "ĐĂNG NHẬP GJW"
- Remove somes unused functions

### v1.9.78 (09-Mar-2023)
- Fix bug: cannot upload video on some computer by set maximum the size / scale down browser
- Correct the way get width & height of short video
- Correct wrong path in deploy stage in workflow

### v1.9.77 (08-Mar-2023)
- Fix bug: app is hanging when detect width & height of this video (https://www.youtube.com/watch?v=zi0iSLxuWVs)
- cannot detect upload video button due to HTML changed from GJW. (Haizz ...)

### v1.9.76 (08-Mar-2023)
- Fix bug: normal video but detect as a short video (https://www.youtube.com/watch?v=5RYb-tf3E3o)
- Change the size of browser to smaller when showing a dialog

### v1.9.75 (05-Mar-2023)
- Fix bug: download and upload video slowly
- Add button to select all video to upload
- Login to GJW via studio.ganjing.com, instead of ganjing.com/signin
- Fix bug: cannot upload thumnail with too long file name (70-75 characters) 
  contains chinese characters (e.g: https://www.youtube.com/watch?v=KfRfVPFz_5s)
- Show Dialog after uploading done ==> let user choose to close browser

### v1.9.74 (01-Mar-2023)
- Fix bug: cannot upload normal video after upload short video
- Ignore, don't upload thumnail for short video

### v1.9.73 (28-Feb-2023)
- Fix bug: hang when upload video
- Add paste button on the search box
- Move upload result checkbox to the end
- Can login more quickly: paste email-password; login by just only 1 click to the button "DÁN VÀ ĐĂNG NHẬP"

### v1.9.72 (27-Feb-2023)
- Add view count
- Auto upload multiple video
- Add number to button

### v1.9.71 (23-Jan-2023)

- Sync to up_stream
- Use YoutubeExplodeZ 6.2.52
- Keep using .NET 6.0

### v1.9.7 (11-Jan-2023)

- Switched from .NET 6.0 to .NET 7.0. The application should update the required prerequisites automatically.
- Fixed an issue where the grid showing the list of downloads failed to resize properly as new items were added. (Thanks [@Jeremy](https://github.com/jerry08))
- Fixed an issue where trying to close the same dialog twice in a short time crashed the application.

### v1.9.63 (20-Jan-2023)

- Fixed cannot auto upload to GJW
- Now can paste email & password of GJW with 1 time

### v1.9.62 (10-Jan-2023)

- Fixed cannot auto upload to GJW
- Show buttons: copy video title, show file in Explorer

### v1.9.61 (12-Dec-2022)

- Merged to Upstream version v1.9.6
- Added download most viewed video only (top 50) & all video with only once search channel query
- Added caching data to local file
- Added download video thumbnails, channel avatar & banner
- Added download filter : ignore low quality video (<720p), shorter than 1 mintue, longer than 1 hour
- Used "[order_number]-[title]-[id]-[DownloadStatus]" format for file name
- Added 2 upgrade modes: release, preview
- Added support Window 32bit (by using the version 32bit of ffmpeg.exe)
- Added "Watch on Youtube", "Check on GJW", "Delete", "Copy title", "Help", "Upload" button
- Changed "restared download" order

### v1.9.6 (06-Dec-2022)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.2.5.

### v1.9.5 (05-Nov-2022)

- Added support for looking up channels by custom handle URLs.

### v1.9.4 (16-Sep-2022)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.2.2.

### v1.9.3 (29-Jun-2022)

- Added support for looking up channels by user page URLs (e.g. https://www.youtube.com/user/BlenderFoundation) and custom channel URLs (e.g. https://www.youtube.com/c/BlenderFoundation).

### v1.9.2 (16-Apr-2022)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.1.2.

### v1.9.1 (15-Apr-2022)

- Re-added the option to disable media tagging in settings.

### v1.9 (10-Apr-2022)

- Improved the accuracy of automatically resolved metadata. Also expanded the list of injected media tags to include some additional information.
- Added `$id` file name template token. It resolves to the ID of the video. (Thanks [@Cole](https://github.com/Cannon-Cole))
- Added auto-detection for dark mode. If your system is configured to prefer dark mode in applications, YoutubeDownloader will use it by default instead of light mode.
- Removed the subtitle selection drop down shown when downloading videos. Subtitles are now downloaded automatically and embedded inside the video file.
- Removed the "inject media tags" option. Media tags are now always injected.
- Removed the "excluded formats" option due to low usefulness.
- Fixed an issue which occasionally prevented video thumbnails from being injected into video files properly.

> **Warning**:
> Some settings may be lost when upgrading from v1.8.x.

### v1.8.7 (07-Mar-2022)

- Actually fixed it this time.

### v1.8.6 (07-Mar-2022)

- Fixed an issue where the application silently failed to run if the system didn't have .NET Runtime 6.0.2 installed. If you continue seeing this issue, please uninstall all existing .NET runtimes from your computer and then try running the application again.

### v1.8.5 (06-Mar-2022)

- Changed target runtime of the application from .NET 3.1 to .NET 6. Application should install the necessary prerequisites automatically. In case you're having trouble launching the app, you can try to install .NET 6 runtime manually [from here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime) (look for the "Run desktop apps" section and choose the distribution appropriate for your system).
- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.1.
- Added messages about war in Ukraine.

### v1.8.4 (18-Dec-2021)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.0.7.
- Fixed various issues related to the runtime bootstrapper. Updated to DotnetRuntimeBootstrapper v2.0.2.

### v1.8.3 (29-Jul-2021)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.0.5.

### v1.8.2 (22-Jun-2021)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.0.3.
- Application will now detect if the required .NET Runtime or any of its prerequisites are missing and prompt the user to download and install them automatically.

### v1.8.1 (21-May-2021)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.0.1.

### v1.8 (18-Apr-2021)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v6.0.
- Removed video upload date from UI and from file name templates.

### v1.7.16 (29-Nov-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.9.
- Fixed an issue where excluded formats were ignored when downloading multiple videos. (Thanks [@derech1e](https://github.com/derech1e))

### v1.7.15 (25-Oct-2020)

- Added subtitle download option when downloading single videos. (Thanks [@beawolf](https://github.com/beawolf))
- Added format exclusion list. You can configure in settings a list of containers which you would like to not see, and they will be filtered out in the format selection dropdown. (Thanks [@beawolf](https://github.com/beawolf))
- Added dark mode. You can enable it in settings. (Thanks [@Andrew Kolos](https://github.com/andrewkolos))
- Added video quality preference selection when downloading multiple videos. (Thanks [@Bartłomiej Rogowski](https://github.com/brogowski))
- Added circular progress bars for each individual active download.
- Added meta tag injection for mp4 files. This adds channel and upload date information, as well as thumbnail. (Thanks [@beawolf](https://github.com/beawolf))
- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.8.

### v1.7.14 (29-Sep-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.6.
- Changed the order in which new downloads appear in the list so that newest downloads are at the top. (Thanks [@Max](https://github.com/badijm))

### v1.7.13 (12-Sep-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.5.

### v1.7.12 (29-Jul-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.3.

### v1.7.11 (21-Jul-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.2.

### v1.7.10 (06-Jul-2020)

- Fixed an issue where mp4 download options took much longer to download due to unnecessary transcoding.

### v1.7.9 (02-Jul-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.1.1.

### v1.7.8 (10-May-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.0.4.

### v1.7.7 (07-May-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.0.3.
- Fixed an issue where conversion progress was not correctly reported. Updated to YoutubeExplode.Converter v1.5.1.

### v1.7.6 (13-Apr-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v5.0.1.
- Improved media tagging. Now it's less reliant on MusicBrainz and should attempt to tag files more often.
- Fixed some issues related to auto-update functionality.

### v1.7.5 (16-Mar-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v4.7.16.

### v1.7.4 (11-Mar-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v4.7.14.

### v1.7.3 (10-Feb-2020)

- Fixed various YouTube-related issues. Updated to YoutubeExplode v4.7.13.

### v1.7.2 (15-Dec-2019)

- Fixed an issue where some videos didn't have any download options. Updated to YoutubeExplode v4.7.11.

### v1.7.1 (15-Nov-2019)

- Fixed an issue where trying to download a single video resulted in an error.

### v1.7 (14-Nov-2019)

- Migrated to .NET Core 3. You will need to install .NET Core runtime in order to run this application starting from this version. You can download it [here](https://dotnet.microsoft.com/download/dotnet-core/current/runtime).
- Added setting "Skip downloads for files that already exist" which, when enabled, skips downloading videos that already have a matching file in the destination directory. Thanks [@mostafa901](https://github.com/mostafa901).
- Changed default file name template to `$title`. You can change it in settings.
- Fixed an issue where the number token in file name template didn't get replaced properly for single-video downloads.

### v1.6.1 (22-Sep-2019)

- Fixed an issue where starting new downloads was not possible if there were already active downloads.

### v1.6 (14-Sep-2019)

- Added support for processing multiple queries in one go. Separate multiple URLs/IDs/searches with new lines (Shift+Enter) to specify multiple queries.
- Added file name template which is used when generating file names for downloaded videos. You can configure it in settings. Refer to the tooltip text for information on what each variable does.
- Added automatic media tagging for downloaded videos (currently only audio files). Tags are resolved from MusicBrainz based on video title. This feature can be disabled in settings.
- Added a context menu button to remove all successfully finished downloads.
- Added a context menu button to restart all failed downloads.
- Added a context menu button to copy title in download setup dialog.
- Starting a new download that overwrites an existing download will now remove the latter from the list.

### v1.5.7 (15-Aug-2019)

- Fixed an issue where some videos failed to download. Updated to YoutubeExplode v4.7.9.

### v1.5.6 (30-Jul-2019)

- Fixed an issue where all videos failed to download. Updated to YoutubeExplode v4.7.7.

### v1.5.5 (27-Jul-2019)

- Fixed an issue where some videos failed to download.

### v1.5.4 (10-Jul-2019)

- Fixed an issue where an attempt to download any video resulted in an error. Updated to YoutubeExplode v4.7.6.

### v1.5.3 (04-Jul-2019)

- Fixed an issue where an attempt to download from channel always resulted in an error. Updated to YoutubeExplode v4.7.5

### v1.5.2 (29-Jun-2019)

- Fixed an issue where some videos were missing from playlists. Updated to YoutubeExplode v4.7.4.
- Fixed an issue where the application crashed when pressing the "play" button if there is no program associated with that file type. An error message is now shown instead.
- Added a context menu button to remove specific download from the list.

### v1.5.1 (21-Jun-2019)

- Fixed an issue where most videos failed to download due to recent YouTube changes. Updated to YoutubeExplode v4.7.3.
- Popups can now be closed by clicking away.
- Default max concurrent download count is now 2 instead of being devised from processor count. You can still tweak it as you want in settings.

### v1.5 (15-Jun-2019)

- Changed the presentation of active downloads to use a data grid.
- Added a context menu button to clear all finished downloads from the list.
- Improved UI by making the general style more consistent.
- Fixed an issue where a download sometimes failed due to a race condition in progress reporting. Updated to Gress v1.1.1.

### v1.4 (13-Jun-2019)

- Fixed an issue where the application crashed when an active download failed. Failure will now be reported in the UI with the option to restart download.
- Fixed an issue where the application crashed when trying to download an unavailable video. Popup with the error message will now be shown instead.
- Fixed an issue where the application crashed due to unknown encoding in some videos. Updated to YoutubeExplode v4.7.2.

### v1.3.2 (12-May-2019)

- Fixed an issue where the application crashed when trying to download videos. Updated to YoutubeExplode v4.7.

### v1.3.1 (03-Mar-2019)

- Fixed an issue where channel URLs were not recognized in some cases. The underlying issue was fixed in YoutubeExplode v4.6.5.
- Fixed an issue where the application would crash sometimes because the progress reported was too high. The underlying issue was fixed in YoutubeExplode.Converter v1.4.1.

### v1.3 (14-Feb-2019)

- Added ability to download videos by channel ID or URL.
- Added ability to download videos by user URL.
- Aggregated progress of all downloads is now shown in the main progress bar and in the taskbar.
- Downloads that have been queued up but not yet started now show "Pending..." instead of "0.00%".
- Selection list for multiple videos now uses Ctrl/Shift to select multiple items.

### v1.2 (19-Jan-2019)

- Added video quality selection when dowloading a single video. For playlists and search results, the highest video quality available for selected format is used.
- Added support for `ogg` format.
- Added support for `webm` format when dowloading a single video. May not always be available.
- Updated the app icon to make it more distinct from YoutubeExplode.
- Fixed an issue where child FFmpeg processes would not exit after the user closes the app while there are active downloads.
- Fixed an issue where the app could sometimes crash when checking for updates.
- Fixed an issue where it was possible to start multiple downloads to the same file path.

### v1.1.1 (22-Dec-2018)

- The list of downloads is now always sorted chronologically.
- When adding multiple downloads, the application will try to ensure unique file names by appending suffixes in order to avoid accidental overwrites.
- Fixed an issue where adding multiple downloads would sometimes cause the application to crash.

### v1.1 (21-Dec-2018)

- Improved UI in all screens.
- Limited the number of concurrent downloads and added an option in settings to configure it.
- Last used download format is now persisted so you don't have to select it all the time.
- Fixed an issue where temporary files were not deleted after the download was canceled. The underlying issue was fixed in YoutubeExplode.Converter v1.0.3 and CliWrap v2.2.
- Fixed an issue where starting multiple downloads would cause them to be added in the wrong order to the list of downloads.