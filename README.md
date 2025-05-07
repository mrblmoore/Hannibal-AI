# HannibalAI - Mount & Blade II: Bannerlord Mod

A custom AI system that controls battle formations in Bannerlord.

## Project Setup in Replit

This repository is configured for development in Replit with strategies for handling Bannerlord's external dependencies.

### Directory Structure

- `src/` - Contains the main mod code
- `lib/` - Place for Bannerlord game DLLs (must be added manually)
- `stubs/` - Stub implementations of game types for compilation
- `Properties/` - Assembly metadata
- `SubModule.xml` - Mod definition file for Bannerlord
- Build scripts for dependency management and compilation

### Development Workflow

1. **First-time setup**: 
   - Run `./download-dependencies.sh` to fetch compatible DLLs
   - Place the downloaded DLLs in the `lib/` directory

2. **Build the mod**:
   - Run `./build.sh` to compile the project
   - Output will be in the `bin/` directory

3. **Verification**:
   - Run `./verify-mod.sh` to check mod structure integrity

### Handling Dependencies

This project uses a hybrid approach for managing Bannerlord dependencies:
- Reference DLLs in the `lib/` folder for compilation
- Stub implementations in `stubs/` for missing types
- Compatible with both Replit and local development

### Using With Bannerlord

To use the compiled mod:
1. Copy the output from the `bin/` directory to your Bannerlord Modules folder
2. Ensure the folder structure matches Bannerlord's module requirements

## GitHub Repository

The latest code is available at: https://github.com/mrblmoore/Hannibal-AI
