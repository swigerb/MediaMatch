#!/bin/bash
# Build a Synology SPK package from a self-contained dotnet publish output.
#
# Usage:
#   ./build-spk.sh <version> <publish-dir> [arch]
#
# Example:
#   dotnet publish src/MediaMatch.CLI -c Release -r linux-x64 --self-contained -o publish/linux-x64
#   ./packaging/synology/build-spk.sh 1.0.0 publish/linux-x64 x86_64
#
#   dotnet publish src/MediaMatch.CLI -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
#   ./packaging/synology/build-spk.sh 1.0.0 publish/linux-arm64 aarch64

set -euo pipefail

VERSION="${1:?Usage: build-spk.sh <version> <publish-dir> [arch]}"
PUBLISH_DIR="${2:?Usage: build-spk.sh <version> <publish-dir> [arch]}"
ARCH="${3:-x86_64}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORK_DIR="$(mktemp -d)"
OUTPUT_DIR="${SCRIPT_DIR}/../../artifacts/synology"

cleanup() {
    rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

echo "Building Synology SPK v${VERSION} for ${ARCH}..."

# Validate publish directory
if [ ! -d "${PUBLISH_DIR}" ]; then
    echo "Error: Publish directory '${PUBLISH_DIR}' does not exist." >&2
    exit 1
fi

if [ ! -f "${PUBLISH_DIR}/MediaMatch.CLI" ]; then
    echo "Error: MediaMatch.CLI binary not found in '${PUBLISH_DIR}'." >&2
    exit 1
fi

# Create package.tgz from published files
echo "Creating package.tgz..."
tar czf "${WORK_DIR}/package.tgz" -C "${PUBLISH_DIR}" .

# Generate INFO file with version and arch substituted
echo "Generating INFO..."
sed -e "s/\${VERSION}/${VERSION}/g" \
    -e "s/\${ARCH}/${ARCH}/g" \
    "${SCRIPT_DIR}/INFO" > "${WORK_DIR}/INFO"

# Copy scripts
echo "Copying scripts..."
mkdir -p "${WORK_DIR}/scripts"
cp "${SCRIPT_DIR}/scripts/start-stop-status" "${WORK_DIR}/scripts/"
cp "${SCRIPT_DIR}/scripts/postinst" "${WORK_DIR}/scripts/"
cp "${SCRIPT_DIR}/scripts/preuninst" "${WORK_DIR}/scripts/"
chmod +x "${WORK_DIR}/scripts/"*

# Build the SPK (a tar archive containing INFO, package.tgz, and scripts/)
echo "Assembling SPK..."
mkdir -p "${OUTPUT_DIR}"
SPK_NAME="MediaMatch-${VERSION}-${ARCH}.spk"
tar cf "${OUTPUT_DIR}/${SPK_NAME}" -C "${WORK_DIR}" INFO package.tgz scripts

echo "Built: ${OUTPUT_DIR}/${SPK_NAME}"
echo "Done."
