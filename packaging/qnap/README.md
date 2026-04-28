# MediaMatch — QNAP QPKG Package

## Overview

This directory contains the files needed to build a QNAP QPKG package for the MediaMatch CLI.
The QPKG installs MediaMatch as a command-line tool accessible via SSH on QNAP NAS devices.

## Prerequisites

- .NET 10 SDK (for building)
- A Linux/macOS shell with `tar` and `sed` (or WSL on Windows)
- QNAP NAS running QTS 5.0 or later

## Building

### 1. Publish the CLI

```bash
# For x86_64 NAS (most QNAP models)
dotnet publish src/MediaMatch.CLI -c Release -r linux-x64 --self-contained -o publish/linux-x64

# For ARM64 NAS
dotnet publish src/MediaMatch.CLI -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
```

### 2. Build the QPKG

```bash
# Make the script executable (needed once, especially after cloning on Windows)
chmod +x packaging/qnap/build-qpkg.sh

# Build for x86_64
./packaging/qnap/build-qpkg.sh 1.0.0 publish/linux-x64 x86_64

# Build for ARM64
./packaging/qnap/build-qpkg.sh 1.0.0 publish/linux-arm64 aarch64
```

Output QPKG files are written to `artifacts/qnap/`.

## Installing

1. Open **QTS → App Center → Install Manually**
2. Upload the `.qpkg` file
3. Follow the installation prompts
4. SSH into the NAS and run: `mediamatch --help`

## Uninstalling

Uninstall via **App Center** or SSH:

```bash
/etc/init.d/MediaMatch.sh stop
```

## Package Structure

```
MediaMatch_1.0.0_x86_64.qpkg
├── qpkg.cfg                # Package metadata and configuration
├── data.tar.gz             # Application files (self-contained CLI)
└── shared/
    └── MediaMatch.sh       # Service script (creates/removes symlink)
```

## Notes

- The `mediamatch` symlink is created in `/usr/local/bin` when the package is started.
- Git on Windows does not preserve Unix file permissions. The build script
  applies `chmod +x` to scripts during QPKG assembly.
