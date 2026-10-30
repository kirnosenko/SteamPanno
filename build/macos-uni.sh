#!/bin/bash

clear
echo "====================="
echo "   MACOS EXPORTER"
echo "====================="

GODOT_PATH="/Applications/Godot_mono.app/Contents/MacOS/Godot"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/../src/SteamPanno"
EXPORT_PATH="$SCRIPT_DIR/../dist/macos-uni"

echo "Exporting..."

# Create export directory
mkdir -p "$EXPORT_PATH"
if [ ! -d "$EXPORT_PATH" ]; then
    echo "ERROR: Failed to create directory: $EXPORT_PATH"
    exit 1
fi

# Run Godot export
"$GODOT_PATH" --verbose --headless --path "$PROJECT_PATH" --export-release "macOS" "$EXPORT_PATH/SteamPanno.app"
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to export project!"
    exit 1
fi

echo "Copying files..."

# Create translations directory
TRANSLATION_DIR="$EXPORT_PATH/custom-assets/translations"
mkdir -p "$TRANSLATION_DIR"
if [ ! -d "$TRANSLATION_DIR" ]; then
    echo "ERROR: Failed to create directory: $TRANSLATION_DIR"
    exit 1
fi

# Copy translation files
echo "Copying translation files..."
cp "$PROJECT_PATH/assets/translations/"* "$TRANSLATION_DIR"
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to copy translation files!"
    exit 1
fi

# Copy Steam dependencies
echo "Copying steam dependencies..."
cp "$PROJECT_PATH/addons/steam/macos/libsteam_api.dylib" "$EXPORT_PATH/libsteam_api.dylib"
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to copy steam dependencies!"
    exit 1
fi

echo "Export completed!"