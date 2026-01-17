# 📚 AuraSync Platform - Complete Documentation

> **Version:** 2.0 | **Date:** January 17, 2026  
> **Purpose:** Platform analysis for feature expansion study

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Platform Architecture](#platform-architecture)
3. [Unity SDK Component](#unity-sdk-component)
4. [Backend Infrastructure](#backend-infrastructure)
5. [API Reference](#api-reference)
6. [Data Models](#data-models)
7. [AI Insights System](#ai-insights-system)
8. [Current Features](#current-features)
9. [Technical Constraints](#technical-constraints)
10. [Potential Expansion Areas](#potential-expansion-areas)

---

## 1. Executive Summary

### What is AuraSync?

AuraSync is a **developer activity tracking and analytics platform** specifically designed for **Unity game development teams**. It captures real-time events from the Unity Editor, processes them through a cloud backend, and provides insights via AI analysis and a web dashboard.

### Core Value Proposition

- **For Developers:** Automatic time tracking without manual input
- **For Team Leads:** Real-time visibility into team activity and progress
- **For Managers:** AI-powered insights on productivity, burnout risk, and bottlenecks

### Technology Stack

| Layer | Technology |
|-------|------------|
| Unity SDK | C# (.NET Standard 2.1) |
| Backend | Supabase (PostgreSQL + Edge Functions) |
| Edge Functions | Deno/TypeScript |
| AI | OpenAI GPT-4o-mini |
| Frontend | React (via Lovable) |
| Hosting | Supabase Cloud (São Paulo region) |

---

## 2. Platform Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           AURASYNC ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────┐                                                   │
│  │     UNITY EDITOR     │                                                   │
│  │  ┌────────────────┐  │                                                   │
│  │  │ HeartbeatCollector│◄─── Editor Events (callbacks)                     │
│  │  └───────┬────────┘  │     - PlayModeStateChanged                        │
│  │          │           │     - SceneSaved/Opened                           │
│  │          ▼           │     - HierarchyChanged                            │
│  │  ┌────────────────┐  │     - CompilationStarted/Finished                 │
│  │  │ HeartbeatSender │  │     - Selection/Window changes                   │
│  │  └───────┬────────┘  │                                                   │
│  └──────────┼───────────┘                                                   │
│             │ HTTPS POST                                                    │
│             │ (JSON payload ~200 bytes)                                     │
│             ▼                                                               │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      SUPABASE BACKEND                                 │  │
│  │                                                                       │  │
│  │   ┌─────────────┐    ┌─────────────┐    ┌─────────────────────────┐  │  │
│  │   │  /log       │    │  /metrics   │    │    /ai-insights         │  │  │
│  │   │  (POST)     │    │  (GET)      │    │    (POST)               │  │  │
│  │   │             │    │             │    │                         │  │  │
│  │   │ Receives    │    │ Returns     │    │ Analyzes activity       │  │  │
│  │   │ heartbeats  │    │ dashboard   │    │ via GPT-4o-mini        │  │  │
│  │   │ from Unity  │    │ data        │    │                         │  │  │
│  │   └──────┬──────┘    └──────┬──────┘    └────────────┬────────────┘  │  │
│  │          │                  │                        │                │  │
│  │          ▼                  ▼                        ▼                │  │
│  │   ┌────────────────────────────────────────────────────────────────┐ │  │
│  │   │                    POSTGRESQL DATABASE                          │ │  │
│  │   │                                                                 │ │  │
│  │   │  workspaces ─┬─► projects                                      │ │  │
│  │   │              │                                                  │ │  │
│  │   │              ├─► sessions ──► heartbeats                       │ │  │
│  │   │              │                                                  │ │  │
│  │   │              └─► insights                                      │ │  │
│  │   └────────────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│             │                                                               │
│             │ REST API                                                      │
│             ▼                                                               │
│  ┌──────────────────────┐                                                   │
│  │   WEB DASHBOARD      │                                                   │
│  │   (React/Lovable)    │                                                   │
│  │                      │                                                   │
│  │  - Real-time feed    │                                                   │
│  │  - Team overview     │                                                   │
│  │  - AI insights       │                                                   │
│  │  - Charts/Analytics  │                                                   │
│  └──────────────────────┘                                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Unity SDK Component

### Installation

Distributed as a Unity Package Manager (UPM) package via Git URL:
```json
{
  "dependencies": {
    "com.heimo.aurasync": "https://github.com/heimogamestudio/aurasync-lib.git"
  }
}
```

### Core Classes

#### HeartbeatCollector.cs
Captures Unity Editor events and generates heartbeat data.

**Key Features:**
- Debounce system (2-second cooldown between similar events)
- 24 distinct event tags for categorization
- Automatic git branch detection
- Low CPU impact (<1% idle)

**Monitored Events:**
| Event | Unity Callback | Event Tag |
|-------|---------------|-----------|
| Play Mode Enter | `playModeStateChanged` | `play_start` |
| Play Mode Exit | `playModeStateChanged` | `play_stop` |
| Scene Open | `sceneOpened` | `scene_open` |
| Scene Save | `sceneSaved` | `scene_save` |
| Scene Create | `newSceneCreated` | `scene_create` |
| Hierarchy Change | `hierarchyChanged` | `hierarchy_change` |
| Selection Change | `selectionChanged` | `selection_change` |
| Compilation Start | `compilationStarted` | `compile_start` |
| Compilation End | `compilationFinished` | `compile_end` |
| Window Focus | `EditorApplication.update` | `window_focus` |
| Package Import | `importPackageCompleted` | `package_import` |
| Asset Modify | `projectChanged` | `asset_modify` |
| Session Start | Constructor | `session_start` |
| Session End | Dispose | `session_end` |
| Periodic Ping | Every 120s | `session_ping` |

#### HeartbeatSender.cs
Handles async HTTP transmission to backend.

**Features:**
- Queue-based processing
- 100ms delay between requests (prevent flooding)
- 10-second timeout with auto-retry
- Graceful failure handling (never blocks editor)

#### HeartbeatData.cs
Optimized payload structure (~200 bytes).

```csharp
public class HeartbeatData
{
    public string entity;           // "Assets/Scripts/Player.cs"
    public string timestamp;        // ISO 8601 UTC
    public bool is_write;           // true if modifying
    public string branch_name;      // "feat/player-movement"
    public string event_tag;        // "code_save"
    public string category;         // "coding"
    public string entity_type;      // "file"
    public string file_ext;         // "cs"
    public string scene;            // "MainLevel"
    public string window;           // "SceneView"
    public string details;          // "Player script saved"
    
    // Only on session_start:
    public string unity_ver;        // "6000.3.2f1"
    public string os;               // "Mac OS X"
}
```

#### EventTags.cs
24 categorized event tags with metadata:

```csharp
public enum EventTag
{
    // Coding Group
    CodeEdit, CodeSave, CompileStart, CompileEnd,
    
    // Scene Group
    SceneOpen, SceneSave, SceneCreate, SceneClose, HierarchyChange,
    
    // Assets Group
    AssetImport, AssetModify, PackageImport, PackageFailed,
    
    // Testing Group
    PlayStart, PlayStop,
    
    // Editor Group
    WindowFocus, SelectionChange, InspectorEdit, ProjectBrowse,
    
    // Session Group
    SessionStart, SessionEnd, SessionPing,
    
    // Other
    Other
}
```

Each tag has:
- `label`: Human-readable name
- `icon`: Emoji for UI
- `color`: Hex color code
- `group`: Category for filtering

---

## 4. Backend Infrastructure

### Supabase Project

- **Project ID:** `ulgebuochosphlsmfmrz`
- **Region:** South America (São Paulo)
- **Database:** PostgreSQL 15
- **Edge Functions:** Deno runtime

### Edge Functions

#### 1. `/log` (POST)
Receives heartbeats from Unity clients.

**Authentication:** API Key header (`api_key`)

**Request:**
```json
{
  "user": "dev@company.com",
  "Project": "GameName",
  "TaskId": "optional-task-id",
  "heartbeat_data": { ... }
}
```

**Response:**
```json
{
  "success": true,
  "session_id": "uuid",
  "workspace": "Workspace Name",
  "event_tag": "code_save",
  "tag_metadata": {
    "label": "Code Save",
    "icon": "💾",
    "color": "#10B981",
    "group": "coding"
  }
}
```

**Logic:**
1. Validate API key via hash lookup
2. Get/create project record
3. Find or create active session (15-min window)
4. Insert heartbeat with enriched metadata
5. Return success with tag info

#### 2. `/metrics` (GET)
Returns dashboard data for frontend.

**Query Parameters:**
- `workspace_id` (required)
- `tag` (optional) - Filter by specific tag
- `group` (optional) - Filter by tag group
- `limit` (optional, default: 50)

**Response:**
```json
{
  "summary": {
    "active_devs_now": 3,
    "total_events_last_hour": 150,
    "events_by_group": {
      "coding": 45,
      "scene": 30,
      "assets": 25
    }
  },
  "active_developers": [
    {
      "user_email": "dev@company.com",
      "status": "online",
      "last_seen": "2026-01-17T14:30:00Z"
    }
  ],
  "recent_events": [
    {
      "id": "uuid",
      "user_email": "dev@company.com",
      "timestamp": "2026-01-17T14:30:00Z",
      "entity": "Assets/Scripts/Player.cs",
      "event_tag": "code_save",
      "tag_label": "Code Save",
      "tag_icon": "💾",
      "tag_color": "#10B981",
      "tag_group": "coding"
    }
  ],
  "hourly_activity": {
    "2026-01-17T10:00:00Z": 15,
    "2026-01-17T11:00:00Z": 23
  },
  "recent_insights": [...],
  "available_tags": [...],
  "available_groups": [...]
}
```

#### 3. `/ai-insights` (POST)
Generates AI-powered analysis.

**Authentication:** JWT Bearer token

**Request:**
```json
{
  "workspace_id": "uuid",
  "type": "session|daily|weekly|burnout|productivity|focus",
  "user_email": "optional@filter.com",
  "session_id": "optional-uuid"
}
```

**Analysis Types:**

| Type | Description | AI Output |
|------|-------------|-----------|
| `session` | Current session analysis | summary, focus_score, mood, recommendations |
| `daily` | Daily summary | productive_hours, peak_hours, achievements, blockers |
| `weekly` | Weekly trends | total_hours, trends, focus_evolution |
| `burnout` | Burnout detection | risk_level, warning_signs, positive_signs |
| `productivity` | Productivity metrics | productivity_score, context_switches, deep_work_sessions |
| `focus` | Focus analysis | current_focus, focus_score, flow_state |

---

## 5. API Reference

### Authentication Methods

| Endpoint | Auth Method | How to Obtain |
|----------|-------------|---------------|
| `/log` | API Key header | Generated per workspace |
| `/metrics` | None (public read) | N/A |
| `/ai-insights` | JWT Bearer | Supabase Auth |

### Rate Limits

| Endpoint | Limit |
|----------|-------|
| `/log` | 100 req/min per API key |
| `/metrics` | 60 req/min per IP |
| `/ai-insights` | 10 req/min per workspace |

### Error Codes

| Code | Meaning |
|------|---------|
| 400 | Missing required fields |
| 401 | Invalid/missing API key |
| 404 | Resource not found |
| 429 | Rate limit exceeded |
| 500 | Internal server error |

---

## 6. Data Models

### Database Schema (PostgreSQL)

```sql
-- Workspaces (Teams/Organizations)
CREATE TABLE workspaces (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    api_key_hash TEXT UNIQUE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

-- Projects (Games)
CREATE TABLE projects (
    id UUID PRIMARY KEY,
    workspace_id UUID REFERENCES workspaces(id),
    name TEXT NOT NULL,
    task_id TEXT,  -- External task tracker integration
    created_at TIMESTAMPTZ NOT NULL
);

-- Sessions (Developer work sessions)
CREATE TABLE sessions (
    id UUID PRIMARY KEY,
    workspace_id UUID REFERENCES workspaces(id),
    project_id UUID REFERENCES projects(id),
    user_email TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    ended_at TIMESTAMPTZ,  -- NULL = active
    last_heartbeat_at TIMESTAMPTZ NOT NULL,
    total_events INTEGER DEFAULT 0,
    activity_score FLOAT  -- Calculated metric
);

-- Heartbeats (Individual events)
CREATE TABLE heartbeats (
    id UUID PRIMARY KEY,
    session_id UUID REFERENCES sessions(id),
    workspace_id UUID REFERENCES workspaces(id),
    user_email TEXT NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    payload JSONB NOT NULL,  -- Full event data
    created_at TIMESTAMPTZ NOT NULL
);

-- AI Insights
CREATE TABLE insights (
    id UUID PRIMARY KEY,
    workspace_id UUID REFERENCES workspaces(id),
    subject_type TEXT NOT NULL,  -- session|user_daily|user_weekly|burnout
    subject_id UUID NOT NULL,
    summary TEXT NOT NULL,
    risk_score FLOAT,  -- 0-1
    recommendations JSONB,  -- Array of strings
    created_at TIMESTAMPTZ NOT NULL
);
```

### Heartbeat Payload Structure (JSONB)

```json
{
  "entity": "Assets/Scripts/Player.cs",
  "timestamp": "2026-01-17T14:30:00.000Z",
  "is_write": true,
  "branch_name": "feat/player-movement",
  "event_tag": "code_save",
  "category": "coding",
  "entity_type": "file",
  "file_ext": "cs",
  "scene": "MainLevel",
  "window": "SceneView",
  "details": "Player script saved",
  
  // Enriched by backend:
  "tag_label": "Code Save",
  "tag_icon": "💾",
  "tag_color": "#10B981",
  "tag_group": "coding"
}
```

---

## 7. AI Insights System

### Model: GPT-4o-mini

**Why this model:**
- Cost-effective (~$0.0004 per analysis)
- Fast response time (~1-2s)
- JSON mode support
- Sufficient for pattern analysis

### System Prompts

Each analysis type has a specialized prompt:

**Session Analysis:**
```
You are an AI analyzing a Unity developer's current session.
Analyze their activity patterns and provide actionable insights.
Respond with JSON: {
  "summary": "...",
  "current_task": "...",
  "focus_score": 0-1,
  "risk_score": 0-1,
  "recommendations": [],
  "mood": "productive|stuck|scattered|deep_work"
}
```

**Burnout Detection:**
```
You are an AI specialized in detecting developer burnout signals.
Analyze patterns that might indicate stress, overwork, or frustration.
Respond with JSON: {
  "risk_level": "low|medium|high|critical",
  "risk_score": 0-1,
  "warning_signs": [],
  "positive_signs": [],
  "recommendations": [],
  "suggested_break": "..."
}
```

### Data Sent to AI

```json
{
  "user": "dev@company.com",
  "session_duration": "2h 15m",
  "total_events": 45,
  "event_summary": {
    "code_save": 12,
    "compile_end": 5,
    "scene_save": 3,
    "play_start": 8
  },
  "recent_events": [
    {
      "time": "14:30:00",
      "tag": "code_save",
      "entity": "Assets/Scripts/Player.cs",
      "is_write": true
    }
  ]
}
```

### Cost Estimation

| Scenario | Monthly Cost |
|----------|-------------|
| 10 devs, basic (session only) | ~$3 |
| 10 devs, full (all analysis types) | ~$15 |
| 50 devs, basic | ~$15 |
| 50 devs, full | ~$75 |

---

## 8. Current Features

### ✅ Implemented

| Feature | Status | Description |
|---------|--------|-------------|
| Real-time heartbeat capture | ✅ | 24 event types from Unity Editor |
| Automatic session management | ✅ | 15-min inactivity timeout |
| Git branch tracking | ✅ | Automatic detection per heartbeat |
| Event categorization (tags) | ✅ | 6 groups, 24 tags with colors |
| Debounce system | ✅ | 2-second cooldown prevents spam |
| Optimized payload | ✅ | ~200 bytes per event |
| Dashboard metrics API | ✅ | Real-time stats and filtering |
| Session AI analysis | ✅ | GPT-4o-mini integration |
| Daily/Weekly reports | ✅ | Aggregated analysis |
| Burnout detection | ✅ | Risk scoring and alerts |
| Focus/Productivity analysis | ✅ | Deep work detection |

### 🚧 Partially Implemented

| Feature | Status | Notes |
|---------|--------|-------|
| Web Dashboard | 🚧 | Design ready, needs Lovable build |
| User authentication | 🚧 | Supabase Auth ready, not integrated |
| Team management | 🚧 | Data model exists, no UI |

### ❌ Not Implemented

| Feature | Priority | Notes |
|---------|----------|-------|
| External task tracker integration | High | ClickUp/Jira/Notion APIs |
| Code diff tracking | Medium | Track actual code changes |
| Build time analytics | Medium | Track Unity build times |
| Custom alerts/notifications | Medium | Slack/Discord/Email |
| Project comparison | Low | Compare across projects |
| Historical trends | Low | Long-term analytics |
| Mobile app | Low | View stats on mobile |

---

## 9. Technical Constraints

### Unity SDK Limitations

| Constraint | Impact | Workaround |
|------------|--------|------------|
| EditorApplication.update runs every frame | CPU usage | Debounce + early return |
| No FileSystemWatcher in Unity | Can't detect external edits | Periodic check (120s) |
| WebRequest is synchronous | Main thread blocking | Async queue with timeout |
| No persistent storage in Editor | Can't cache offline | Retry on next session |

### Backend Limitations

| Constraint | Impact | Workaround |
|------------|--------|------------|
| Supabase Edge Function 50MB limit | Large payloads fail | Optimize payload size |
| 10s function timeout | Long AI analysis | Limit heartbeats to 50 |
| PostgreSQL JSONB query speed | Slow on large datasets | Indexed created_at |
| No WebSocket support | No real-time push | Client polling (30s) |

### AI Limitations

| Constraint | Impact | Workaround |
|------------|--------|------------|
| Token limit (4K output) | Truncated responses | Limit input data |
| Context window (128K) | Limited history | Summarize events |
| Rate limits | 429 errors | Queue + exponential backoff |
| Cost per request | Budget concerns | Batch analysis |

---

## 10. Potential Expansion Areas

### Data We Currently Have But Don't Fully Utilize

| Data Point | Current Use | Potential Use |
|------------|-------------|---------------|
| `entity` (file path) | Display only | File complexity analysis, hot file detection |
| `timestamp` sequence | Session duration | Work pattern analysis, optimal hours detection |
| `is_write` flag | Basic stats | Read/write ratio, "exploration" vs "production" modes |
| `branch_name` | Display only | Feature lifecycle tracking, branch longevity |
| `file_ext` | Display only | Language/asset type distribution |
| `window` (active window) | Display only | Tool usage patterns, workflow optimization |
| `scene` | Display only | Scene complexity, context switching cost |
| Compilation events | Display only | Build time trends, compilation frequency |
| Play mode events | Display only | Testing patterns, iteration speed |

### Potential New Data Points to Capture

| Data Point | How to Capture | Value |
|------------|----------------|-------|
| Code line count changes | Git diff on save | Actual code output measurement |
| Undo/Redo frequency | EditorApplication callbacks | Frustration indicator |
| Error console messages | Application.logMessageReceived | Blocker detection |
| Asset file sizes | File.GetLength | Project health monitoring |
| Frame rate in Play Mode | Time.deltaTime | Performance awareness |
| External tool usage | Process monitoring | Workflow integration |
| Keyboard shortcuts used | EditorWindow.SendEvent | Expertise level detection |
| Search queries | SearchService hooks | Knowledge gaps |

### Integration Opportunities

| Platform | Integration Type | Value |
|----------|-----------------|-------|
| **ClickUp** | Task ID correlation | Time per task, auto-logging |
| **Jira** | Issue linking | Sprint velocity, blocker correlation |
| **GitHub** | Commit correlation | Code output vs activity |
| **Slack** | Notifications | Real-time alerts, status updates |
| **Discord** | Bot integration | Team presence, quick stats |
| **Notion** | Database sync | Documentation updates |
| **Linear** | Issue tracking | Feature progress |
| **Figma** | Design sync | Designer collaboration |

### AI Enhancement Opportunities

| Feature | Description | Complexity |
|---------|-------------|------------|
| **Anomaly Detection** | Detect unusual patterns automatically | Medium |
| **Predictive Burnout** | Predict burnout before it happens | High |
| **Smart Recommendations** | Context-aware suggestions | Medium |
| **Team Dynamics** | Analyze team collaboration patterns | High |
| **Skill Assessment** | Infer skill levels from activity | High |
| **Onboarding Tracker** | New dev progress monitoring | Medium |
| **Code Review Suggestions** | AI-suggested review points | High |
| **Meeting Impact** | Analyze productivity around meetings | Medium |

### Monetization Potential

| Tier | Features | Price Point |
|------|----------|-------------|
| **Free** | Basic tracking, 1 project, 7-day history | $0 |
| **Pro** | Unlimited projects, 90-day history, basic AI | $9/user/month |
| **Team** | Team analytics, daily reports, integrations | $19/user/month |
| **Enterprise** | Custom AI, SSO, dedicated support | Custom |

---

## Appendix A: File Structure

```
aurasync-lib/
├── Runtime/
│   ├── AuraSyncManager.cs          # Main entry point
│   └── Heartbeat/
│       ├── HeartbeatCollector.cs   # Event capture (optimized)
│       ├── HeartbeatCollectorLegacy.cs  # Old version (deprecated)
│       ├── HeartbeatSender.cs      # HTTP transmission
│       ├── HeartbeatData.cs        # Payload structure
│       ├── Heartbeat.cs            # Internal model
│       ├── HeartbeatEnums.cs       # Categories & entity types
│       ├── EventTags.cs            # 24 event tags with metadata
│       ├── AuraSyncSettings.cs     # Configuration
│       ├── AuraSyncLogger.cs       # Logging abstraction
│       ├── Extensions.cs           # DateTime extensions
│       ├── GitClient.cs            # Git integration
│       └── IHeartbeatCollector.cs  # Interface
├── Editor/
│   └── Heartbeat/
│       └── AuraSyncEditorDetector.cs  # Auto-initialization
├── backend/
│   └── supabase/
│       ├── functions/
│       │   ├── log/index.ts        # Heartbeat receiver
│       │   ├── metrics/index.ts    # Dashboard API
│       │   └── ai-insights/index.ts # AI analysis
│       └── migrations/
│           └── 20260110000000_init_schema.sql
└── Documentation/
    └── (various guides)
```

---

## Appendix B: Environment Variables

### Supabase Edge Functions

| Variable | Description |
|----------|-------------|
| `SUPABASE_URL` | Auto-injected by Supabase |
| `SUPABASE_SERVICE_ROLE_KEY` | Auto-injected by Supabase |
| `OPENAI_API_KEY` | Required for AI insights |

### Unity SDK

| Setting | Default | Description |
|---------|---------|-------------|
| `BackendUrl` | Production URL | Edge function endpoint |
| `ApiKey` | None | Workspace API key |
| `UserEmail` | System username | Developer identifier |

---

## Appendix C: Event Tag Reference

| Tag | Group | Icon | Color | Description |
|-----|-------|------|-------|-------------|
| `code_edit` | coding | 💻 | #3B82F6 | Script file selected/opened |
| `code_save` | coding | 💾 | #10B981 | Script file saved |
| `compile_start` | coding | ⚙️ | #F59E0B | Compilation started |
| `compile_end` | coding | ✅ | #10B981 | Compilation finished |
| `scene_open` | scene | 📂 | #8B5CF6 | Scene opened |
| `scene_save` | scene | 💾 | #10B981 | Scene saved |
| `scene_create` | scene | ✨ | #EC4899 | New scene created |
| `scene_close` | scene | 📁 | #6B7280 | Scene closed |
| `hierarchy_change` | scene | 🔀 | #F97316 | Hierarchy modified |
| `asset_import` | assets | 📦 | #06B6D4 | Asset imported |
| `asset_modify` | assets | ✏️ | #F59E0B | Asset modified |
| `package_import` | assets | 📦 | #8B5CF6 | Package imported |
| `package_failed` | assets | ❌ | #EF4444 | Package import failed |
| `play_start` | testing | ▶️ | #10B981 | Entered play mode |
| `play_stop` | testing | ⏹️ | #6B7280 | Exited play mode |
| `window_focus` | editor | 🪟 | #6B7280 | Window focus changed |
| `selection_change` | editor | 👆 | #8B5CF6 | Selection changed |
| `inspector_edit` | editor | 🔧 | #F59E0B | Inspector property edited |
| `project_browse` | editor | 📁 | #6B7280 | Project view navigation |
| `session_start` | session | 🚀 | #10B981 | Editor session started |
| `session_end` | session | 👋 | #6B7280 | Editor session ended |
| `session_ping` | session | 💓 | #3B82F6 | Periodic heartbeat |
| `other` | other | 📝 | #6B7280 | Uncategorized event |

---

## Appendix D: Sample API Calls

### Send Heartbeat
```bash
curl -X POST "https://ulgebuochosphlsmfmrz.supabase.co/functions/v1/log" \
  -H "Content-Type: application/json" \
  -H "api_key: YOUR_API_KEY" \
  -d '{
    "user": "dev@company.com",
    "Project": "GameName",
    "heartbeat_data": {
      "entity": "Assets/Scripts/Player.cs",
      "timestamp": "2026-01-17T14:30:00.000Z",
      "is_write": true,
      "event_tag": "code_save"
    }
  }'
```

### Get Metrics
```bash
curl "https://ulgebuochosphlsmfmrz.supabase.co/functions/v1/metrics?workspace_id=YOUR_WORKSPACE_ID&group=coding&limit=20"
```

### Generate AI Insight
```bash
curl -X POST "https://ulgebuochosphlsmfmrz.supabase.co/functions/v1/ai-insights" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT" \
  -d '{
    "workspace_id": "YOUR_WORKSPACE_ID",
    "type": "session"
  }'
```

---

**End of Documentation**

*Generated for AuraSync Platform v2.0*  
*January 17, 2026*
