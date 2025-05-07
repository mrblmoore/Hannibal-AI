#!/bin/bash

# Script to download Bannerlord dependencies for HannibalAI mod
# This is a helper script for development in Replit

echo "HannibalAI - Dependency Download Script"
echo "======================================="
echo "This script will attempt to download Bannerlord DLL dependencies."
echo "Note: These are for development purposes only."

# Create lib directory if it doesn't exist
mkdir -p lib
cd lib

# Base URL for downloading dependencies - example only, replace with actual source
DEPENDENCY_URL="https://github.com/mrblmoore/Hannibal-AI/releases/download/dependencies"

# List of dependencies to download
declare -a DEPENDENCIES=(
    "TaleWorlds.Library.dll"
    "TaleWorlds.Core.dll"
    "TaleWorlds.Engine.dll"
    "TaleWorlds.MountAndBlade.dll"
    "TaleWorlds.GauntletUI.dll"
    "TaleWorlds.InputSystem.dll"
    "TaleWorlds.CampaignSystem.dll"
)

# Download dependencies
echo "Downloading dependencies..."
for dependency in "${DEPENDENCIES[@]}"; do
    if [ ! -f "$dependency" ]; then
        echo "Downloading $dependency..."
        wget -q "$DEPENDENCY_URL/$dependency" || {
            echo "Error: Failed to download $dependency"
            echo "You may need to manually obtain this DLL from your Bannerlord installation."
        }
    else
        echo "$dependency already exists, skipping download."
    fi
done

# Check if any DLLs were downloaded
if [ "$(ls -A *.dll 2>/dev/null)" ]; then
    echo "Dependencies downloaded successfully."
    echo "You can now build the project using ./build.sh"
else
    echo "No dependencies were downloaded."
    echo "You will need to manually add the required DLLs to the lib directory."
    echo "See lib/README.md for more information."
fi

cd ..

echo "======================================="
echo "Dependency download process complete."
