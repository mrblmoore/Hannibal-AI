#!/bin/bash

# Script to build the HannibalAI mod for Mount & Blade II: Bannerlord

echo "Building HannibalAI mod..."

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null
then
    echo "Error: dotnet is not installed. Please install .NET SDK."
    exit 1
fi

# Check if the lib directory has DLLs
if [ ! "$(ls -A lib/*.dll 2>/dev/null)" ]; then
    echo "Warning: No DLLs found in lib directory."
    echo "Your build will use stub implementations, which may not be complete."
    echo "Run ./download-dependencies.sh to download dependencies or add them manually."
fi

# Create bin directory and Win64_Shipping_Client directory if they don't exist
mkdir -p bin/Win64_Shipping_Client

# Build the project
echo "Compiling C# code..."
dotnet build HannibalAI.csproj --configuration Release

# Check if build was successful
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

# Copy files to Win64_Shipping_Client directory (this is where Bannerlord looks for DLLs)
echo "Copying files to Win64_Shipping_Client directory..."
cp bin/HannibalAI.dll bin/Win64_Shipping_Client/
cp bin/HannibalAI.pdb bin/Win64_Shipping_Client/

# Copy SubModule.xml to bin root directory
echo "Copying SubModule.xml to bin directory..."
cp SubModule.xml bin/
cp SubModule.xml bin/Win64_Shipping_Client/

# Copy GUI folder to bin directory if it exists
if [ -d "GUI" ]; then
    echo "Copying GUI files..."
    mkdir -p bin/GUI
    cp -r GUI/* bin/GUI/
fi

echo "Build completed successfully."
echo "Output files are located in the bin directory."
echo "To use this mod with Bannerlord, copy the contents of the bin directory to your Bannerlord Modules/HannibalAI folder."

# Verify the structure
echo "Verifying mod structure..."
if [ -f bin/HannibalAI.dll ] && [ -f bin/SubModule.xml ]; then
    echo "✅ Mod structure verified. Basic files are present."
else
    echo "❌ Mod structure is incomplete. Please check the build output."
fi
