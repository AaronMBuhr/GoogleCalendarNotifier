# Set error action preference to stop on any error
$ErrorActionPreference = "Stop"

# Generate log filename
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "patch-fixes_$timestamp.log"
$fullLogPath = "$PWD\$logFile"

# Start logging
Start-Transcript -Path $logFile

try {
    Write-Host "Starting modifications at $(Get-Date)"
    Write-Host "============================="

    # Fix MainWindow.xaml.cs - use a more precise replacement
    $content = Get-Content MainWindow.xaml.cs -Raw
    $content = $content -replace '[^\S\r\n]+private async void MainCalendar_DisplayDateChanged\([^}]*}\r?\n    }', '    }'
    $content | Set-Content MainWindow.xaml.cs -Force -Encoding UTF8

    Write-Host "============================="
    Write-Host "Completed modifications at $(Get-Date)"

    # Calculate stats and copy to clipboard
    $stats = @{
        'MainWindow.xaml.cs' = @{
            MD5 = (Get-FileHash -Algorithm MD5 MainWindow.xaml.cs).Hash
            Words = (Get-Content MainWindow.xaml.cs -Raw | Select-String -Pattern '\w+' -AllMatches).Matches.Count
            Lines = (Get-Content MainWindow.xaml.cs).Count
        }
    }
    $statsStr = ($stats | ConvertTo-Json)
    $statsStr | Set-Clipboard
    Write-Host "File statistics copied to clipboard:`n$statsStr"
}
catch {
    Write-Host "Error occurred: $_"
    Write-Host "Stack Trace: $($_.ScriptStackTrace)"
    exit 1
}
finally {
    Stop-Transcript
}
