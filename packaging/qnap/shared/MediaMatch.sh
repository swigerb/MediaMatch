#!/bin/bash
# QNAP QPKG service script for MediaMatch
# MediaMatch is a CLI tool — this script handles install/remove symlink management.

QPKG_NAME="MediaMatch"
QPKG_DIR="/share/CACHEDEV1_DATA/.qpkg/${QPKG_NAME}"
BINARY="${QPKG_DIR}/MediaMatch.CLI"
SYMLINK_PATH="/usr/local/bin/mediamatch"

case "$1" in
    start)
        # Create symlink for CLI access
        if [ -x "${BINARY}" ]; then
            ln -sf "${BINARY}" "${SYMLINK_PATH}"
            echo "${QPKG_NAME}: symlink created at ${SYMLINK_PATH}"
        else
            echo "${QPKG_NAME}: binary not found at ${BINARY}" >&2
            exit 1
        fi
        ;;
    stop)
        # Remove symlink
        if [ -L "${SYMLINK_PATH}" ]; then
            rm -f "${SYMLINK_PATH}"
            echo "${QPKG_NAME}: symlink removed"
        fi
        ;;
    restart)
        "$0" stop
        "$0" start
        ;;
    status)
        if [ -x "${BINARY}" ]; then
            echo "${QPKG_NAME}: installed"
            exit 0
        else
            echo "${QPKG_NAME}: not installed"
            exit 1
        fi
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status}" >&2
        exit 1
        ;;
esac

exit 0
