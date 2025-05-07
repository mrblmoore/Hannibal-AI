#!/bin/bash

# Script to verify that the HannibalAI mod structure is correct

echo "Verifying HannibalAI mod structure..."

# Check if bin directory exists
if [ ! -d "bin" ]; then
    echo "❌ bin directory not found. Please build the mod first using ./build.sh"
    exit 1
fi

# Check for required files
required_files=("bin/HannibalAI.dll" "bin/SubModule.xml")

all_files_present=true
for file in "${required_files[@]}"; do
    if [ ! -f "$file" ]; then
        echo "❌ Missing required file: $file"
        all_files_present=false
    else
        echo "✅ Found required file: $file"
    fi
done

if [ "$all_files_present" = false ]; then
    echo "Some required files are missing. Please rebuild the mod."
    exit 1
fi

echo "Checking code stub implementations..."

# Check for stub implementations
stub_files=(
    "stubs/TaleWorlds/Engine/Vec3.cs"
    "stubs/TaleWorlds/GauntletUI/ViewModel.cs"
    "stubs/TaleWorlds/MountAndBlade/FormationOrder.cs"
)

all_stubs_present=true
for file in "${stub_files[@]}"; do
    if [ ! -f "$file" ]; then
        echo "❌ Missing stub implementation: $file"
        all_stubs_present=false
    else
        echo "✅ Found stub implementation: $file"
    fi
done

if [ "$all_stubs_present" = false ]; then
    echo "Some stub implementations are missing. This may cause build issues."
fi

# Check DLL references
echo "Checking DLL references in lib directory..."

dll_references=(
    "lib/TaleWorlds.Library.dll"
    "lib/TaleWorlds.Core.dll"
    "lib/TaleWorlds.Engine.dll"
    "lib/TaleWorlds.MountAndBlade.dll"
    "lib/TaleWorlds.GauntletUI.dll"
    "lib/TaleWorlds.InputSystem.dll"
    "lib/TaleWorlds.CampaignSystem.dll"
)

any_dlls_present=false
for file in "${dll_references[@]}"; do
    if [ -f "$file" ]; then
        echo "✅ Found DLL reference: $file"
        any_dlls_present=true
    else
        echo "⚠️ Missing DLL reference: $file (will use stub implementation)"
    fi
done

if [ "$any_dlls_present" = false ]; then
    echo "⚠️ No DLL references found. The mod is using only stub implementations."
    echo "This is fine for basic development but may not accurately represent game behavior."
    echo "You can download DLLs using ./download-dependencies.sh or add them manually."
fi

echo ""
echo "Verification complete."
if [ "$all_files_present" = true ]; then
    echo "✅ The mod structure appears to be valid."
    echo "To use with Bannerlord, copy the contents of the bin directory to your Bannerlord Modules/HannibalAI folder."
else
    echo "❌ The mod structure has issues that need to be resolved."
fi
