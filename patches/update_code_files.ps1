# update_code_files_2.ps1
# This script patches the MainWindow.xaml file to resolve namespace-related issues and ensure proper configuration for styles.

# --- 1. Backup Original File ---
Write-Host "Backing up the original MainWindow.xaml..."
$originalFile = "./MainWindow.xaml"
$backupFile = "$originalFile.bak"
if (Test-Path $originalFile) {
    Copy-Item -Path $originalFile -Destination $backupFile -Force
    Write-Host "Backup created for $originalFile as $backupFile"
} else {
    Write-Host "File not found: $originalFile"
    exit 1
}

# --- 2. Update MainWindow.xaml ---
Write-Host "Patching MainWindow.xaml with updated content..."
@"
<Window x:Class="GoogleCalendarNotifier.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="Calendar Notifications"
        Height="700" Width="900">

    <Window.Resources>
        <Style TargetType="{x:Type CalendarDayButton}">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type CalendarDayButton}">
                        <Grid>
                            <Border x:Name="Background"
                                    Background="Transparent"
                                    BorderBrush="{StaticResource CustomAccentBrush}"
                                    BorderThickness="0"
                                    CornerRadius="4"/>
                            <ContentPresenter x:Name="Content"
                                              HorizontalAlignment="Center"
                                              VerticalAlignment="Center"
                                              Margin="1"/>
                            <Border x:Name="EventIndicator"
                                    Height="2"
                                    Background="{StaticResource CustomAccentBrush}"
                                    VerticalAlignment="Bottom"
                                    Margin="2,0,2,2"
                                    Visibility="Collapsed"/>
                        </Grid>
                        <ControlTemplate.Triggers>
                            <Trigger Property="Tag" Value="HasEvents">
                                <Setter TargetName="EventIndicator" Property="Visibility" Value="Visible"/>
                            </Trigger>
                            <Trigger Property="Tag" Value="IsToday">
                                <Setter TargetName="Background" Property="Background" Value="Yellow"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <Grid>
        <!-- UI Elements Here -->
    </Grid>

</Window>
"@ > $originalFile
Write-Host "MainWindow.xaml updated successfully."

# --- 3. Generate Checksums ---
Write-Host "Generating checksums for MainWindow.xaml..."
$md5 = Get-FileHash -Path $originalFile -Algorithm MD5 | Select-Object -ExpandProperty Hash
$wc = Get-Content $originalFile | Measure-Object -Line -Word -Character
Write-Host "MD5: $md5 for $originalFile"
Write-Host "WC: Lines=$($wc.Lines), Words=$($wc.Words), Characters=$($wc.Characters)"

Write-Host "Script execution completed. Rebuild the project and verify functionality."
