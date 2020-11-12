# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2020-11-11

No longer maintain `SocketIOClient.NetFx`, it is replaced by `SocketIOClient`. For users, you don’t have to worry about choosing `SocketIOClient` or `SocketIOClient.NetFx`

### Added

- Support verify the server certificate

### Changed

- The usage of `Options.EnabledSslProtocols` has changed
- `ClientWebSocket` becomes a sealed class
- Changed the way to configure the proxy

### Removed

- Removed `ClientWebSocket.CreatClient()` virtual method

## [2.0.2.9] - 2020-10-27

### Fixed

- Fixed a serious error that caused connection and sending message to become unusable. The error originated from the last update(v2.0.2.8).

## [2.0.2.8] - 2020-10-25

### Added

- Implement ConnectionTimeout
- Added library docs
- When connecting for the first time, it supports automatic reconnection.

## [2.0.2.7] - 2020-10-09

### Added

- Support WebProxy, [PR](https://github.com/doghappy/socket.io-client-csharp/pull/87)

## [2.0.2.6] - 2020-09-09

### Fixed

- Fixed "SynchronizationLockException" exception, [issues-84](https://github.com/doghappy/socket.io-client-csharp/issues/84)

## [2.0.2.5] - 2020-09-08

### Fixed

- Fixed "Collection was modified" exception