# TaleWorlds DLL References Directory

This directory is for storing Bannerlord DLL references needed for the HannibalAI mod.

## Required DLLs

Place the following DLLs from your Bannerlord installation here:

- TaleWorlds.Library.dll
- TaleWorlds.Core.dll 
- TaleWorlds.Engine.dll
- TaleWorlds.MountAndBlade.dll
- TaleWorlds.GauntletUI.dll
- TaleWorlds.InputSystem.dll
- TaleWorlds.CampaignSystem.dll

## Where to Find These Files

These files can typically be found in your Bannerlord installation directory at:
- `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\`
- `C:\Program Files\Epic Games\MountAndBladeIIBannerlord\bin\Win64_Shipping_Client\`

## Note About DLL References

For the mod to compile in Replit, you need to provide these DLLs. Due to licensing, these files are not included in the repository and must be sourced from your own Bannerlord installation.

Run the `download-dependencies.sh` script to attempt to download these files automatically, or manually place them in this directory.
