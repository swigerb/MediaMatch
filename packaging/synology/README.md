# MediaMatch — Synology SPK Package

## Overview

This directory contains the files needed to build a Synology SPK package for the MediaMatch CLI.
The SPK installs MediaMatch as a command-line tool accessible via SSH on DSM 7.0+.

## Prerequisites

- .NET 10 SDK (for building)
- A Linux/macOS shell with `tar` and `sed` (or WSL on Windows)
- Synology NAS running DSM 7.0 or later

## Building

### 1. Publish the CLI

```bash
# For x86_64 NAS (most Synology models)
dotnet publish src/MediaMatch.CLI -c Release -r linux-x64 --self-contained -o publish/linux-x64

# For ARM64 NAS (e.g., DS223j, DS124)
dotnet publish src/MediaMatch.CLI -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
```

### 2. Build the SPK

```bash
# Make the script executable (needed once, especially after cloning on Windows)
chmod +x packaging/synology/build-spk.sh

# Build for x86_64
./packaging/synology/build-spk.sh 1.0.0 publish/linux-x64 x86_64

# Build for ARM64
./packaging/synology/build-spk.sh 1.0.0 publish/linux-arm64 aarch64
```

Output SPK files are written to `artifacts/synology/`.

## Installing

1. Open **DSM → Package Center → Manual Install**
2. Upload the `.spk` file
3. Follow the installation wizard
4. SSH into the NAS and run: `mediamatch --help`

## Uninstalling

Uninstall via **Package Center** or run:

```bash
synopkg uninstall MediaMatch
```

## Package Structure

```
MediaMatch-1.0.0-x86_64.spk
├── INFO                    # Package metadata
├── package.tgz             # Application files (self-contained CLI)
└── scripts/
    ├── start-stop-status   # Required by SPK format
    ├── postinst            # Creates /usr/local/bin/mediamatch symlink
    └── preuninst           # Removes symlink on uninstall
```

## Notes

- The package sets `startable="no"` since MediaMatch is a CLI tool, not a service.
- The `mediamatch` symlink is created in `/usr/local/bin` for easy SSH access.
- Git on Windows does not preserve Unix file permissions. The build script
  applies `chmod +x` to all scripts during SPK assembly.
