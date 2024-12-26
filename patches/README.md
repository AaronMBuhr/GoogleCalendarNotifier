# WSL/Linux File Patching System Guide

## Overview
This guide describes how to systematically modify text-based files (code, configuration, documentation, etc.) using WSL/Linux bash scripting. This approach is more reliable and efficient than Windows-based solutions for text processing.

## Setup Requirements
1. Windows Subsystem for Linux (WSL) installed with any Linux distribution
2. Git installed in Windows (recommended for change tracking)
3. Basic knowledge of bash scripting and text processing tools

## Directory Structure
```
project_root/
├── patches/
│   ├── README.md
│   ├── apply-fixes.sh
│   └── patch-fixes.log
└── [project files]
```

## Basic Script Template
Create `patches/apply-fixes.sh`:
```bash
#!/bin/bash
set -e  # Exit on error

# Set up logging with tee
exec 1> >(tee -a "patch-fixes.log")
exec 2> >(tee -a "patch-fixes.log" >&2)

echo "Starting modifications at $(date)"
echo "============================="

# Convert Windows path to WSL path if needed
PROJ_DIR=$(wslpath -u "path/to/project")
cd "$PROJ_DIR"

# File modifications go here. Examples:

# 1. Simple replacements with sed
sed -i 's/old_text/new_text/g' filename.ext

# 2. Complex file rewrites
cat > filename.ext.tmp << 'EOL'
// New content here
EOL
mv filename.ext.tmp filename.ext

echo "============================="
echo "Completed modifications at $(date)"
```

## Usage
1. Navigate to project directory in WSL terminal:
```bash
cd /path/to/project  # Use appropriate path
```

2. Make script executable (first time only):
```bash
chmod a+x patches/apply-fixes.sh
```

3. Run script:
```bash
patches/apply-fixes.sh
```

4. Review patch-fixes.log for results

## Best Practices
1. Always log changes using tee
2. Use 'EOL' for heredocs to prevent variable expansion
3. Create backups of important files before modification
4. Use meaningful commit messages when committing changes
5. Test changes in a development environment first
6. Handle errors gracefully with set -e
7. Document expected outcomes in comments

## Common Operations

### Text Replacement
```bash
# Simple replacement
sed -i 's/old/new/g' file.ext

# Multiple replacements
sed -i -e 's/old1/new1/g' -e 's/old2/new2/g' file.ext

# Replace with regex
sed -i 's/pattern[0-9]*/replacement/g' file.ext

# Replace across multiple files
find . -type f -name "*.ext" -exec sed -i 's/old/new/g' {} +
```

### File Creation/Modification
```bash
# Create/overwrite file
cat > filename.ext << 'EOL'
content here
EOL

# Append to file
cat >> filename.ext << 'EOL'
content here
EOL

# Safe file creation using temporary file
cat > filename.ext.tmp << 'EOL'
content here
EOL
mv filename.ext.tmp filename.ext
```

### Directory Operations
```bash
# Create directory if it doesn't exist
mkdir -p directory/path

# Process files in directory
for file in directory/*.ext; do
    sed -i 's/old/new/g' "$file"
done
```

### Backup Operations
```bash
# Create backup before modifying
cp filename.ext filename.ext.bak

# Create timestamped backup
cp filename.ext filename.ext.$(date +%Y%m%d_%H%M%S).bak
```

## Troubleshooting
1. Line ending issues:
   - Use `dos2unix` for Windows files
   - Use `unix2dos` if output needs Windows endings
   - Use consistent line endings in heredocs
2. Permission issues:
   - Check file permissions with `ls -l`
   - Use `chmod` to modify permissions
   - Run with sudo if needed (carefully!)
3. Path issues:
   - Use `wslpath` to convert Windows paths
   - Use absolute paths when unsure
   - Quote paths containing spaces
4. Encoding issues:
   - Specify encoding in tools that support it
   - Use `iconv` for encoding conversion
   - Default to UTF-8 when possible

## Example Script Output
```
Starting modifications at Thu Dec 26 06:26:32 AM EST 2024
=============================
Working directory: /path/to/project
[modification details]
=============================
Completed modifications at Thu Dec 26 06:26:32 AM EST 2024
```

## Maintenance
- Keep scripts in a dedicated patches/ directory
- Maintain logs for change history
- Document changes in version control
- Test modifications before applying to production
- Consider creating reusable functions for common operations

## Additional Tools
- `awk`: Advanced text processing
- `grep`: Pattern matching and searching
- `find`: File location and batch processing
- `diff`: File comparison
- `patch`: Apply diff files
- `tr`: Character translation or deletion
- `cut`/`paste`: Column operations
- `sort`/`uniq`: Line operations