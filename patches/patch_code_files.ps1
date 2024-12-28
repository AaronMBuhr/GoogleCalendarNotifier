# PowerShell Script to Patch Google Calendar Notifier Project Files

# Define the target files
$filesToPatch = @(
    "MainWindow.xaml",
    "MainWindow.xaml.cs"
)

# Define backup directory
$backupDir = "backup"

# Ensure backup directory exists
if (!(Test-Path -Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}
}

# Backup and modify each file
foreach ($file in $filesToPatch) {
    if (Test-Path -Path $file) {
        # Backup the original file
        Write-Output "Backing up $file to $backupDir"
        Copy-Item -Path $file -Destination "$backupDir\$file"

        # Read the content of the file
        $content = Get-Content -Path $file

        # Apply modifications based on the file name
        switch ($file) {
            "MainWindow.xaml" {
                Write-Output "Applying patch to $file"

                # Update the calendar day button styles
                $content = $content -replace "(?<=<Style TargetType=\"CalendarDayButton\">.*?<Setter Property=\"Background\" Value=\")(#[A-Fa-f0-9]{6}|\w+)(?=\" />)", "#E2E8F0" # Light gray for current month
                $content = $content -replace "(?<=<Style TargetType=\"CalendarDayButton\">.*?<Setter Property=\"Foreground\" Value=\")(#[A-Fa-f0-9]{6}|\w+)(?=\" />)", "#1E293B" # Dark gray for non-current month

                # Add underline to days with events
                $content = $content -replace "(<Rectangle x:Name=\"EventMark\".*?Visibility=\")Collapsed(\")", "$1Visible$2"
            }

            "MainWindow.xaml.cs" {
                Write-Output "Applying patch to $file"

                # Update logic to mark days with events
                $content = $content -replace "(?<=CalendarDayButtonExtensions.SetHasEvents\(dayButton, )(false)(?=\);)", "true"
            }
        }

        # Write the modified content back to the file
        Set-Content -Path $file -Value $content
    } else {
        Write-Output "File $file not found. Skipping."
    }
}

# Generate checksums for verification
$checksums = @()
foreach ($file in $filesToPatch) {
    if (Test-Path -Path $file) {
        $md5 = Get-FileHash -Path $file -Algorithm MD5 | Select-Object -ExpandProperty Hash
        $lines = (Get-Content -Path $file).Count
        $words = (Get-Content -Path $file | ForEach-Object { $_ -split "\s+" }).Count
        $checksums += "$file > MD5: $md5, #lines: $lines, #words: $words"
    }
}

# Output checksums for verification
Write-Output "Checksums for verification:"
$checksums | ForEach-Object { Write-Output $_ }
