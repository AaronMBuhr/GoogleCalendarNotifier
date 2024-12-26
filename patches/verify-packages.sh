#!/bin/bash
set -e  # Exit on error

echo "Verifying NuGet packages..."

# Convert Windows path to WSL path
PROJ_DIR=$(wslpath -u "E:/Source/Mine/GoogleCalendarNotifier")
cd "$PROJ_DIR"

# Required packages and versions
declare -A packages=(
    ["H.NotifyIcon.Wpf"]="2.0.124"
    ["MahApps.Metro"]="2.4.10"
    ["Microsoft.Toolkit.Uwp.Notifications"]="7.1.3"
)

# Read csproj file
while IFS= read -r line; do
    for pkg in "${!packages[@]}"; do
        if [[ $line =~ "<PackageReference Include=\"$pkg\"" ]]; then
            version=$(echo $line | grep -oP "Version=\"\K[^\"]+" || echo "not found")
            if [[ "$version" == "${packages[$pkg]}" ]]; then
                echo "✓ $pkg version $version - OK"
            else
                echo "✗ $pkg version mismatch: found $version, expected ${packages[$pkg]}"
            fi
        fi
    done
done < GoogleCalendarNotifier.csproj

echo
echo "Verification complete!"