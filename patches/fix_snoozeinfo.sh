#!/bin/bash

# Define the directory of the project
PROJECT_DIR="."

# Remove the `SnoozeInfo` definition from ConfigData.cs
sed -i '/class SnoozeInfo/,/}/d' "$PROJECT_DIR/ConfigData.cs"

# Replace all references to the `SnoozeInfo` properties in the project
find "$PROJECT_DIR" -type f -name "*.cs" | while read -r file; do
    sed -i 's/SnoozeUntil/UntilTime/g' "$file"
done

echo "Duplicate SnoozeInfo class issue resolved."
