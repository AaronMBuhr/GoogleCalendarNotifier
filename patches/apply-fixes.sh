#!/bin/bash
set -e  # Exit on error

exec 1> >(tee -a "patch-fixes.log")
exec 2> >(tee -a "patch-fixes.log" >&2)

echo "Starting fixes at $(date)"
echo "============================="

# Fix MainWindow.xaml.cs
sed -i '1i using Microsoft.Xaml.Behaviors.Wpf;' MainWindow.xaml.cs
sed -i 's/using H.NotifyIcon;/using H.NotifyIcon.Wpf;/' MainWindow.xaml.cs
sed -i 's/private INotifyIcon/private TaskbarIcon/' MainWindow.xaml.cs

echo "============================="
echo "Completed fixes at $(date)"