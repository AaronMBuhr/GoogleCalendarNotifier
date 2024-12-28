# Patch script to update calendar styles in MainWindow.xaml
$filePath = Join-Path $PSScriptRoot ".." "MainWindow.xaml"

# Define the old style block pattern with looser matching
$oldStylePattern = '(?s)<Style x:Key="EventDayStyle".*?</Style>'

# New style content
$newStyle = @'
        <Style x:Key="EventDayStyle" TargetType="{x:Type Calendar}">
            <Setter Property="Background" Value="{StaticResource CalendarDarkBrush}"/>
            <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
            <Setter Property="Foreground" Value="{StaticResource CalendarLightBrush}"/>
            <Style.Resources>
                <!-- Day Buttons: Inverted colors + underline if HasEvents -->
                <Style TargetType="CalendarDayButton">
                    <!-- Current-month days: light background, dark text -->
                    <Setter Property="Background" Value="{StaticResource CalendarLightBrush}"/>
                    <Setter Property="Foreground" Value="{StaticResource CalendarDarkBrush}"/>
                    <Setter Property="Template">
                        <Setter.Value>
                            <ControlTemplate TargetType="CalendarDayButton">
                                <Grid>
                                    <Border Background="{TemplateBinding Background}"
                                            BorderBrush="{TemplateBinding BorderBrush}"
                                            BorderThickness="{TemplateBinding BorderThickness}">
                                        <TextBlock Text="{TemplateBinding Content}"
                                                 HorizontalAlignment="Center"
                                                 VerticalAlignment="Center"
                                                 Foreground="{TemplateBinding Foreground}">
                                            <TextBlock.Style>
                                                <Style TargetType="TextBlock">
                                                    <Style.Triggers>
                                                        <DataTrigger Binding="{Binding (local:CalendarDayButtonExtensions.HasEvents), RelativeSource={RelativeSource TemplatedParent}}" Value="True">
                                                            <Setter Property="TextDecorations" Value="Underline"/>
                                                        </DataTrigger>
                                                    </Style.Triggers>
                                                </Style>
                                            </TextBlock.Style>
                                        </TextBlock>
                                    </Border>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                    <Style.Triggers>
                        <!-- Other-month days (IsEnabled=false): dark background, light text -->
                        <DataTrigger Binding="{Binding IsEnabled, RelativeSource={RelativeSource Self}}" Value="False">
                            <Setter Property="Background" Value="{StaticResource CalendarDarkBrush}"/>
                            <Setter Property="Foreground" Value="{StaticResource CalendarLightBrush}"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>

                <!-- Keep default layout for CalendarItem (no flipping) -->
                <Style TargetType="CalendarItem">
                    <Setter Property="Background" Value="{StaticResource CalendarDarkBrush}"/>
                    <Setter Property="Foreground" Value="{StaticResource CalendarLightBrush}"/>
                    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
                </Style>

                <!-- Month/Year header -->
                <Style TargetType="CalendarButton">
                    <Setter Property="Background" Value="{StaticResource CalendarDarkBrush}"/>
                    <Setter Property="Foreground" Value="{StaticResource CalendarLightBrush}"/>
                </Style>
            </Style.Resources>
        </Style>
'@

try {
    # Read the current content
    $content = Get-Content -Path $filePath -Raw

    if ($content -match $oldStylePattern) {
        Write-Output "Found existing style block. Replacing..."
        $updatedContent = $content -replace $oldStylePattern, $newStyle
        
        # Write the updated content back to the file
        Set-Content -Path $filePath -Value $updatedContent
        Write-Output "Successfully updated the style in $filePath"
    } else {
        Write-Error "Could not find the EventDayStyle block in the XAML file"
    }
} catch {
    Write-Error "An error occurred: $_"
}
