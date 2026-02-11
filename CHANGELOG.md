# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-02-11

### Added
- ClickUp task tracking via Git branch names (`task_id` extracted from branches)
- Debug logging for heartbeat processing and initialization errors (`AURA_SYNC_DEBUG`)
- Time metrics API support and Lovable time dashboard integration
- Inactivity detection - stops sending session pings after 5 minutes of no activity
- Enhanced user resolution logic (environment variables, Unity account, Git email, system fallback)

### Changed
- Optimized HeartbeatCollector with debouncing, git branch caching, and inactivity detection
- Backend URL updated to Railway production endpoint
- README updated with backend repository link

### Fixed
- Cleaned up malformed preprocessor directives in `HeartbeatSender.SendHeartbeatToBackendAsync`
- Corrected preprocessor directive placement in `HeartbeatSender`
- Fixed session pings being sent when developer is inactive

## [1.0.1] - 2025-05-26

### Added
- N/A

### Changed
- N/A

### Fixed
- Fixed an issue that could cause build compilation errors due to references to CloudProjectSettings
- Ensured all code runs only in Unity Editor and is not compiled in the final build

## [1.0.0] - 2025-05-22

### Added
- Initial release of AuraSync package

### Changed
- N/A

### Fixed
- N/A
