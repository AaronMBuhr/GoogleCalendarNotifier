
#!/bin/bash

# Verify and synchronize x:Class in XAML and code-behind
echo "Verifying x:Class consistency between .xaml and .xaml.cs files..."

find . -name "*.xaml" | while read -r xaml_file; do
  # Extract x:Class value from XAML file
  xclass=$(grep -oP 'x:Class="\K[^"]+' "$xaml_file")
  
  # Check for corresponding .cs file
  cs_file="${xaml_file%.xaml}.xaml.cs"
  if [[ -f "$cs_file" ]]; then
    # Verify namespace/class consistency in code-behind
    if ! grep -q "namespace ${xclass%.*}" "$cs_file"; then
      echo "Fixing namespace in $cs_file"
      sed -i "1s/^/namespace ${xclass%.*} {\n/" "$cs_file"
      echo "}" >> "$cs_file"
    fi
  else
    echo "Warning: No code-behind file found for $xaml_file"
  fi
done

# Ensure MainCalendar and RefreshCalendarMonth references
echo "Ensuring MainCalendar and RefreshCalendarMonth references in code-behind..."

find . -name "*.xaml.cs" | while read -r cs_file; do
  if [[ "$(basename "$cs_file")" == "MainWindow.xaml.cs" ]]; then
    # Add missing method or variable declarations if needed
    if ! grep -q "MainCalendar" "$cs_file"; then
      echo "Adding MainCalendar definition to $cs_file"
      sed -i '/public partial class MainWindow/a\
      private System.Windows.Controls.Calendar MainCalendar;' "$cs_file"
    fi

    if ! grep -q "RefreshCalendarMonth" "$cs_file"; then
      echo "Adding RefreshCalendarMonth stub to $cs_file"
      echo -e "\n\
      private async System.Threading.Tasks.Task RefreshCalendarMonth() {\n\
          // TODO: Add implementation\n\
      }" >> "$cs_file"
    fi
  fi
done

# Generate checksums for verification
echo "Generating checksums for modified files..."
find . -name "*.xaml" -o -name "*.xaml.cs" | while read -r file; do
  md5sum "$file"
  wc "$file"
done

echo "Script execution completed. Please rebuild the project manually and provide the build output and file checksums."
