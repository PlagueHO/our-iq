# Changelog

All notable changes to Our IQ will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and versions are intended to follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Initial repository and documentation scaffold.
- Proposed dependency-ordered GitHub issue backlog for the V1 implementation.
- Durable, atomic Cosmos DB replay for the four implemented private ontology
  mutations, including saved business failures and changed-input conflicts.
- Shared structured tool errors across both MCP hosts, updated pre-GA contract
  schemas, and replay coverage. Public workflows remain unsupported.
- Proposed ADR-0031 for replay comparison, atomic persistence, and
  content-bearing replay-record retention.
