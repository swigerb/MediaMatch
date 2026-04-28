#!/bin/bash
# Build a QNAP QPKG package from a self-contained dotnet publish output.
#
# Usage:
#   ./build-qpkg.sh <version> <publish-dir> [arch]
#
# Example:
#   dotnet publish src/MediaMatch.CLI -c Release -r linux-x64 --self-contained -o publish/linux-x64
#   ./packaging/qnap/build-qpkg.sh 1.0.0 publish/linux-x64 x86_64
#
#   dotnet publish src/MediaMatch.CLI -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
#   ./packaging/qnap/build-qpkg.sh 1.0.0 publish/linux-arm64 aarch64

set -euo pipefail

VERSION="${1:?Usage: build-qpkg.sh <version> <publish-dir> [arch]}"
PUBLISH_DIR="${2:?Usage: build-qpkg.sh <version> <publish-dir> [arch]}"
ARCH="${3:-x86_64}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORK_DIR="$(mktemp -d)"
OUTPUT_DIR="${SCRIPT_DIR}/../../artifacts/qnap"

cleanup() {
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

echo "Building QNAP QPKG v${VERSION} for ${ARCH}..."

# Validate publish directory
if [ ! -d "${PUBLISH_DIR}" ]; then
    echo "Error: Publish directory '${PUBLISH_DIR}' does not exist." >&2
    exit 1
fi

if [ ! -f "${PUBLISH_DIR}/MediaMatch.CLI" ]; then
    echo "Error: MediaMatch.CLI binary not found in '${PUBLISH_DIR}'." >&2
    exit 1
fi

# Create data archive from published files
echo "Creating data.tar.gz..."
tar czf "${WORK_DIR}/data.tar.gz" -C "${PUBLISH_DIR}" .

# Copy service script
echo "Copying service script..."
mkdir -p "${WORK_DIR}/shared"
cp "${SCRIPT_DIR}/shared/MediaMatch.sh" "${WORK_DIR}/shared/"
chmod +x "${WORK_DIR}/shared/MediaMatch.sh"

# Generate qpkg.cfg with version substituted
echo "Generating qpkg.cfg..."
sed -e "s/\${VERSION}/${VERSION}/g" \
    "${SCRIPT_DIR}/qpkg.cfg" > "${WORK_DIR}/qpkg.cfg"

# Build the QPKG (tar.gz containing qpkg.cfg, data.tar.gz, and shared/)
echo "Assembling QPKG..."
mkdir -p "${OUTPUT_DIR}"
QPKG_NAME="MediaMatch_${VERSION}_${ARCH}.qpkg"
tar czf "${OUTPUT_DIR}/${QPKG_NAME}" -C "${WORK_DIR}" qpkg.cfg data.tar.gz shared

echo "Built: ${OUTPUT_DIR}/${QPKG_NAME}"
echo "Done."
