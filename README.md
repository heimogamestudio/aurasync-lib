# AuraSync

Developer activity tracking and analytics for Unity Editor teams.

AuraSync is a Unity Editor plugin that captures real-time development events (scene edits, compilations, play mode, asset changes, etc.) and sends them to a cloud backend for team analytics, productivity insights, and AI-powered analysis.

**Related Repositories:**
- [aurasync-backend](https://github.com/heimogamestudio/aurasync-backend) - Supabase backend with edge functions, metrics, and AI insights

---

## Features

- **Automatic activity tracking** - Monitors 15+ Unity Editor events with 24 categorized event tags
- **Zero configuration** - Auto-detects user identity, project name, and Git branch
- **Zero runtime impact** - Editor-only code, nothing compiled into game builds
- **Non-blocking** - Async HTTP transmission, never freezes the editor
- **Performance optimized** - Event debouncing (2s cooldown), <1% CPU overhead
- **Git integration** - Automatic branch detection and tracking
- **Session management** - Automatic session creation with 15-minute inactivity timeout
- **ClickUp integration** - Task ID extraction from Git branch names
- **AI insights** - Backend generates session, daily, weekly, burnout, and focus analysis via GPT-4o-mini

## Tracked Events

| Group | Events |
|-------|--------|
| **Coding** | Script edit, script save, compilation start/end |
| **Scene** | Scene open/save/create/close, hierarchy changes |
| **Assets** | Asset import/modify, package import/failed |
| **Testing** | Play mode enter/exit |
| **Editor** | Window focus, selection change, inspector edit, project browse |
| **Session** | Session start/end, periodic ping (every 120s) |

## Requirements

- Unity 2021.3 or later
- No external dependencies

## Installation

### Via Git Submodule (Recommended for teams)

```bash
# From your Unity project root
git submodule add https://github.com/heimogamestudio/aurasync-lib.git Packages/com.heimo.aurasync
git commit -m "Add AuraSync as submodule"
```

Team members clone with:
```bash
git clone --recursive <project-url>
# Or if already cloned:
git submodule update --init --recursive
```

### Via Unity Package Manager (Git URL)

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Enter: `https://github.com/heimogamestudio/aurasync-lib.git#v1.1.0`

### Via manifest.json

Add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.heimo.aurasync": "https://github.com/heimogamestudio/aurasync-lib.git#v1.1.0"
  }
}
```

### Via Local Package

```json
{
  "dependencies": {
    "com.heimo.aurasync": "file:../path/to/com.heimo.aurasync"
  }
}
```

## Usage

AuraSync starts automatically when the Unity Editor loads - no manual setup required. It detects your identity, project, and Git branch automatically.

### Configuration via Environment Variables

Override defaults by setting environment variables before launching Unity:

| Variable | Purpose | Example |
|----------|---------|---------|
| `AURASYNC_USER_EMAIL` | Developer identity | `dev@company.com` |
| `AURASYNC_BACKEND_URL` | Backend endpoint | `https://your-backend.com/api/pulse/heartbeat` |
| `AURASYNC_API_KEY` | API authentication key | `your_api_key_here` |

### User Identity Resolution

AuraSync resolves the developer identity in this order:

1. `AURASYNC_USER_EMAIL` / `AURASYNC_USER` / `EMAIL` environment variables
2. Unity Cloud account (`CloudProjectSettings.userName`)
3. Git configured email (`git config user.email`)
4. System username with `@local` suffix
5. `unknown@local` (final fallback)

### Debug Logging

To enable verbose logging, add `AURA_SYNC_DEBUG` to Unity's Scripting Define Symbols:

**Project Settings > Player > Other Settings > Scripting Define Symbols**

### Programmatic Access

```csharp
#if UNITY_EDITOR
using Heimo.AuraSync;

// Force re-initialization
AuraSyncManager.Initialize();

// Shutdown
AuraSyncManager.Shutdown();
#endif
```

## Architecture

```
AuraSyncEditorDetector          [InitializeOnLoad] - auto triggers on editor start
    |
    v
AuraSyncManager                 Static facade - wires components together
    |
    +---> HeartbeatCollector     Listens to Unity Editor callbacks, emits events
    |         |
    |         v (OnHeartbeat)
    +---> HeartbeatSender        Queues and sends heartbeats via async HTTP POST
```

### Key Classes

| Class | Responsibility |
|-------|---------------|
| `AuraSyncEditorDetector` | Auto-initialization via `[InitializeOnLoad]` |
| `AuraSyncManager` | Lifecycle management, wires collector to sender |
| `HeartbeatCollector` | Captures Unity Editor events with debouncing |
| `HeartbeatSender` | Async HTTP queue with retry and timeout |
| `HeartbeatData` | Optimized serializable payload (~200 bytes) |
| `AuraSyncSettings` | Auto-detected immutable configuration |
| `EventTags` | 24 event tag definitions with visual metadata |
| `GitClient` | Git branch detection via CLI |

## Project Structure

```
Runtime/
  AuraSyncManager.cs
  Heimo.AuraSync.asmdef
  Heartbeat/
    HeartbeatCollector.cs
    HeartbeatSender.cs
    HeartbeatData.cs
    Heartbeat.cs
    HeartbeatEnums.cs
    EventTags.cs
    AuraSyncSettings.cs
    AuraSyncLogger.cs
    Extensions.cs
    GitClient.cs
    IHeartbeatCollector.cs
    AuraSyncRuntime.cs
    HeartbeatCollectorLegacy.cs  (deprecated)
Editor/
  Heimo.AuraSync.Editor.asmdef
  Heartbeat/
    AuraSyncEditorDetector.cs
Tests/
  Heimo.AuraSync.Tests.asmdef
```

## Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.

## Support

For questions or support, contact us at contato@heimogames.com.br or visit [heimogames.com.br](https://heimogames.com.br).
