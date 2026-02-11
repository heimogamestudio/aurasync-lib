# CLAUDE.md - AuraSync Unity SDK

## Project Overview

AuraSync is a **Unity Editor plugin** (UPM package) that tracks developer activity in real-time and sends telemetry heartbeats to the AuraSync backend (Hono/PostgreSQL on Railway). It's designed for developer workflow analytics, team productivity insights, and AI-generated insights.

- **Package name:** `com.heimo.aurasync`
- **Namespace:** `Heimo.AuraSync`
- **Version:** 1.1.0
- **Min Unity:** 2021.3
- **License:** MIT
- **Author:** Heimo Game Studio

## Architecture

```
Editor/AuraSyncEditorDetector.cs  -- [InitializeOnLoad] triggers initialization
        |
        v
Runtime/AuraSyncManager.cs        -- Static facade, wires collector -> sender
        |
        +-> HeartbeatCollector.cs  -- Monitors 15+ Unity Editor callbacks
        |       |
        |       v (OnHeartbeat event)
        +-> HeartbeatSender.cs     -- Async HTTP queue, POST to backend
```

All code is **editor-only** (`#if UNITY_EDITOR` + assembly `includePlatforms: ["Editor"]`). Nothing ships in game builds.

## Key Design Decisions

- **Zero dependencies** - no external packages required
- **Non-blocking** - async HTTP via UnityWebRequest, never blocks editor
- **Debounced events** - 2-second cooldown between same-type events
- **Silent failures** - errors logged only with `AURA_SYNC_DEBUG` define, never crashes editor
- **Immutable config** - `AuraSyncSettings` uses private setters, auto-resolves user/project
- **Queue-based sending** - HeartbeatSender processes a queue with 100ms delay between requests

## Directory Structure

```
Runtime/
  AuraSyncManager.cs              # Main entry point, static facade
  Heimo.AuraSync.asmdef           # Runtime assembly (Editor platform only)
  Heartbeat/
    HeartbeatCollector.cs         # Optimized event capture (ACTIVE)
    HeartbeatCollectorLegacy.cs   # Deprecated, do not use
    HeartbeatSender.cs            # Async HTTP sender with queue
    HeartbeatData.cs              # Serializable payload (~200 bytes)
    Heartbeat.cs                  # Internal data model (18 properties)
    HeartbeatEnums.cs             # HeartbeatCategories, EntityTypes enums
    EventTags.cs                  # 24 EventTag enum values + metadata
    AuraSyncSettings.cs           # Auto-detected config (env vars -> Unity -> Git -> system)
    AuraSyncLogger.cs             # IAuraSyncLogger interface + DefaultLogger
    AuraSyncRuntime.cs            # Placeholder for future runtime tracking
    Extensions.cs                 # Enum description + DateTime extensions
    GitClient.cs                  # IGitClient, gets branch via `git rev-parse`
    IHeartbeatCollector.cs        # Interface with OnHeartbeat event
Editor/
  Heimo.AuraSync.Editor.asmdef    # Editor assembly, references Runtime
  Heartbeat/
    AuraSyncEditorDetector.cs     # [InitializeOnLoad] auto-start/stop
Tests/
  Heimo.AuraSync.Tests.asmdef     # NUnit test assembly (no tests written yet)
```

## Conventions

### Code Style
- C# with .NET Standard 2.1 targeting Unity 2021.3+
- PascalCase for public members, _camelCase for private fields
- XML doc comments (`///`) on public classes and methods
- Comments and doc strings in **Portuguese (pt-BR)** for internal docs
- Enum values use PascalCase; serialized field names use snake_case

### Git & Commits
- Conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`
- Branch names follow `feat/`, `fix/`, `docs/` patterns
- Version tags: `v1.0.0`, `v1.1.0`

### Error Handling
- All Unity Editor callbacks wrapped in `SafeExecute()` (try-catch)
- Never throw in production - log with `IAuraSyncLogger` (only outputs with `AURA_SYNC_DEBUG`)
- HTTP failures are silently swallowed, never block editor

### Preprocessor Directives
- All code must be wrapped in `#if UNITY_EDITOR ... #endif`
- Debug logging guarded by `#if AURA_SYNC_DEBUG`
- Assembly definitions already restrict to Editor platform, but `#if` guards are used as defense-in-depth

## Configuration Resolution

Settings are auto-detected with this priority chain:

**User identity:**
1. `AURASYNC_USER_EMAIL` env var
2. `AURASYNC_USER` env var
3. `EMAIL` env var
4. `UnityEditor.CloudProjectSettings.userName`
5. `git config user.email`
6. `Environment.UserName + "@local"`
7. `"unknown@local"` (final fallback)

**Backend URL:** `AURASYNC_BACKEND_URL` env var or hardcoded default

**API Key:** `AURASYNC_API_KEY` env var or hardcoded default

**Organization ID:** `AURASYNC_ORGANIZATION_ID` env var or hardcoded default

## Event System

24 `EventTag` values organized in 6 groups:
- **Coding:** CodeEdit, CodeSave, CompileStart, CompileEnd
- **Scene:** SceneOpen, SceneSave, SceneCreate, SceneClose, HierarchyChange
- **Assets:** AssetImport, AssetModify, PackageImport, PackageFailed
- **Testing:** PlayStart, PlayStop
- **Editor:** WindowFocus, SelectionChange, InspectorEdit, ProjectBrowse
- **Session:** SessionStart, SessionEnd, SessionPing

Each tag has metadata: label, icon (emoji), color (hex).

## Backend Integration

- **Endpoint:** `POST /api/pulse/heartbeat` (Railway production or local dev)
- **Headers:** `x-api-key`, `x-organization-id`, `Content-Type: application/json; charset=utf-8`
- **Payload:** `DevActivityPayload` wrapping `HeartbeatData` (JSON, ~200 bytes)
- **Session management:** Backend creates/extends sessions with 15-min inactivity timeout
- **Backend repo:** [aurasync](https://github.com/pbisogno/aurasync)
- **Important:** The `x-organization-id` and user email must correspond to existing records in the backend DB (FK constraints on `organization` and `user` tables)

## Performance Characteristics

| Metric | Value |
|--------|-------|
| CPU overhead | <1% idle |
| Event debounce | 2 seconds per tag |
| Session ping interval | 120 seconds |
| Inactivity threshold | 5 minutes (stops pings) |
| Git branch cache | 5 minutes TTL |
| HTTP timeout | 10 seconds |
| Queue delay | 100ms between sends |

## Common Tasks

### Adding a new EventTag
1. Add value to `EventTag` enum in `EventTags.cs`
2. Add metadata case in `EventTagMetadata.GetDisplayInfo()`
3. Add the event capture logic in `HeartbeatCollector.cs`
4. Update `PLATFORM_DOCUMENTATION.md` Appendix C

### Enabling debug logs
Add `AURA_SYNC_DEBUG` to Unity's Scripting Define Symbols (Project Settings > Player).

### Changing the backend URL
Set the `AURASYNC_BACKEND_URL` environment variable, or modify the default in `AuraSyncSettings.ResolveBackendUrl()`.

### Local Development with Backend

To develop this plugin alongside the backend:

1. **Remove remote package** from your Unity project's `Packages/manifest.json` (remove the git URL line)
2. In Unity: **Window > Package Manager > "+" > "Add package from disk..."** → select this repo's `package.json`
3. Add `AURA_SYNC_DEBUG` to **Scripting Define Symbols** (Project Settings > Player)
4. Set `AURASYNC_BACKEND_URL=http://localhost:3000/api/pulse/heartbeat` as env var (or edit `AuraSyncSettings.cs`)
5. To revert: remove local package, re-add git URL to manifest.json

## Known Issues

- `HeartbeatCollectorLegacy.cs` is deprecated but still in the codebase - do not reference it
- `AuraSyncRuntime.cs` is a placeholder stub - `TrackGameEvent()` is not implemented
- `Tests/` directory has an assembly definition but no test files yet
- `Extensions.cs` `GetDescription()` uses reflection without caching

## Related Repositories

- **Backend:** [aurasync](https://github.com/pbisogno/aurasync) (TurboStarter monorepo — Hono API, Drizzle ORM, PostgreSQL)
