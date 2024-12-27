# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]
### Added
- Feature or functionality added.

### Changed
- Existing feature or code change.

### Deprecated
- Feature or functionality marked for removal.

### Removed
- Feature or functionality removed.

### Fixed
- Bug fixes or resolved issues.

### Security
- Notes on security updates.

## [1.0.1] - 2024-12-17

### Added
- Lock and take a snapshot of the dictionary to prevent collection modified during foreach.

### Fixed
- Fix logic when timer reset, the timer instance changed </br>
=> When timer reset, the timer instance keeps the same. Timer instance only change when remove the instance.

### Deprecated
- GetOrCreateTimeSpan => GetOrCreateTimer

## [1.0.0] - 2024-12-01
### Added
- Initial release of the module.
- Core functionality: CRUD time mark.
