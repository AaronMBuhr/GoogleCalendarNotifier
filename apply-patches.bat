@echo on
setlocal enabledelayedexpansion

echo Current directory: %CD%
echo.
echo Applying patches...
echo.

cd /d %~dp0
echo Changed to script directory: %CD%
echo.

set PATCH_DIR=patches
set ERROR_COUNT=0

for %%f in (%PATCH_DIR%\*.patch) do (
    echo.
    echo ========================================
    echo Processing patch file: %%f
    echo Full path: !CD!\%%f
    echo File contents:
    type "%%f"
    echo ========================================
    echo.
    echo Attempting to apply patch...
    git apply -v --ignore-whitespace --ignore-space-change --whitespace=fix "%%f"
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Failed to apply patch %%~nxf with error code !ERRORLEVEL!
        set /a ERROR_COUNT+=1
    ) else (
        echo Successfully applied patch %%~nxf
    )
    echo.
)

if !ERROR_COUNT! gtr 0 (
    echo.
    echo WARNING: !ERROR_COUNT! patches failed to apply.
    echo Please check the error messages above.
) else (
    echo.
    echo All patches were applied successfully!
)

echo.
pause