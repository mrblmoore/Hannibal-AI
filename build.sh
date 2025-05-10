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

# Create proper directory structure first
mkdir -p bin/Win64_Shipping_Client/

# Build will place DLLs in bin/ by default - copy to correct location and then remove from root
echo "Moving files to Win64_Shipping_Client directory..."
cp bin/HannibalAI.dll bin/Win64_Shipping_Client/
cp bin/HannibalAI.pdb bin/Win64_Shipping_Client/

# Copy SubModule.xml to bin root directory (required) and client directory
echo "Copying SubModule.xml to bin directory..."
cp SubModule.xml bin/
cp SubModule.xml bin/Win64_Shipping_Client/

# Copy GUI folder to bin directory if it exists
if [ -d "GUI" ]; then
    echo "Copying GUI files..."
    mkdir -p bin/GUI
    cp -r GUI/* bin/GUI/
fi

# Copy ModuleData folder to bin directory if it exists
if [ -d "ModuleData" ]; then
    echo "Copying ModuleData files..."
    mkdir -p bin/ModuleData
    cp -r ModuleData/* bin/ModuleData/
fi

# Remove duplicate DLLs from bin root to avoid confusion (leave only SubModule.xml in root)
echo "Cleaning up duplicate files..."
rm -f bin/HannibalAI.dll bin/HannibalAI.pdb

# Ensure Win64_Shipping_Client directory exists
mkdir -p bin/Win64_Shipping_Client

# Copy DLL and PDB to shipping directory
cp bin/HannibalAI.dll bin/Win64_Shipping_Client/
cp bin/HannibalAI.pdb bin/Win64_Shipping_Client/

echo "Build completed successfully."
echo "Output files are located in bin/Win64_Shipping_Client"
echo ""
echo "To use this mod with Bannerlord, copy the contents of the bin directory"
echo "to your Bannerlord Modules/HannibalAI folder."

# Verify the structure
echo "Verifying mod structure..."
if [ -f bin/Win64_Shipping_Client/HannibalAI.dll ] && [ -f bin/SubModule.xml ]; then
    echo "✅ Mod structure verified. Basic files are present."
else
    echo "❌ Mod structure is incomplete. Please check the build output."
fi