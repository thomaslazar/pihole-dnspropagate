# Fix Docker Build Version Source
- Type: Task
- Phase: 8
- Prerequisites: PIDP-015
- Status: Completed

## Context
The Docker build stage fails during `dotnet restore` because `Directory.Build.props` reads the root `VERSION` file, but the Dockerfile does not copy that file into the build context. Releases are currently blocked until the build includes the version metadata.

## Work Items
- [x] Update the Dockerfile to copy `VERSION` (and any other version metadata) into the build stage before running `dotnet restore`.
- [x] Ensure the manual image build workflow behaves similarly if additional files are required.
- [x] Validate the release workflow and manual build workflow both succeed after the fix.

## Acceptance Criteria
- [x] Docker builds (release workflow + manual image workflow) complete without `VERSION` missing errors.
- [x] VERSION values in assemblies/container metadata match the root file.

## Notes
- Coordinate with PIDP-015 to keep the version source authoritative.
