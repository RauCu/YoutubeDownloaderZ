$ytdlpFilePath = "$PSScriptRoot\yt-dlp.exe"

# Check if already exists
if (Test-Path $ytdlpFilePath) {
    Write-Host "Skipped downloading yt-dlp.exe, file already exists."
    exit
}

Write-Host "Downloading yt-dlp.exe..."

# Download the exe file
$url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$wc = New-Object System.Net.WebClient
$wc.DownloadFile($url, "$ytdlpFilePath")
$wc.Dispose()

Write-Host "Done downloading yt-dlp.exe."
