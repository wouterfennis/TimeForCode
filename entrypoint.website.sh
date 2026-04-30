#!/bin/bash
set -e

# Fix ownership of the storage directory at runtime so the non-root user
# can read and write Data Protection keys, even when a Docker named volume
# is mounted over /storage by a root-owned volume.
chown -R appuser:appgroup /storage

# Drop privileges and execute the application
exec gosu appuser dotnet TimeForCode.Website.dll "$@"
