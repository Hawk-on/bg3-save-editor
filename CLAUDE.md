# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BG3 Save Editor - a web app for editing Baldur's Gate 3 save files. C# .NET 8 backend with Angular 20 LTS frontend.

## Build & Run Commands

### Backend (.NET 8)
```powershell
dotnet build backend/BG3.SaveEditor.Api/BG3.SaveEditor.Api.csproj
dotnet run --project backend/BG3.SaveEditor.Api/BG3.SaveEditor.Api.csproj   # runs on :5062
dotnet watch --project backend/BG3.SaveEditor.Api                           # hot reload
```

### Frontend (Angular 20)
```powershell
cd frontend-ng
npm install
npm start        # dev server on :4200
npm run build    # production build
npm test         # karma tests
```

### Test Project
```powershell
dotnet run --project backend/BG3.SaveEditor.Test/BG3.SaveEditor.Test.csproj
```

## Architecture

### Backend
- **BG3.SaveEditor.Api** - ASP.NET Core Web API. `SaveController` exposes REST endpoints. CORS configured for localhost origins.
- **BG3.SaveEditor.Core** - Business logic. `SaveService` handles all save file operations via LSLib. `Models/SaveModels.cs` defines `SaveInfo`, `SaveState`, `GoldItem`.
- **BG3.SaveEditor.Test** - Console app for manual testing against real save files.

### Frontend
- Angular 20 standalone components (no NgModules)
- Services use Angular **signals** for reactive state and **RxJS** for async HTTP
- Components access service signals via getters (not field initializers, due to DI timing)
- Uses `@if`/`@for` control flow syntax (Angular 17+ style)

### API Endpoints (all POST)
| Endpoint | Purpose | Request Body |
|---|---|---|
| `/api/save/list` | List saves in directory | `{ Path: string }` |
| `/api/save/load` | Load save + parse gold | `{ Path: string }` |
| `/api/save/gold` | Modify gold amount | `{ Path: string, Amount: int, OutputPath?: string }` |
| `/api/save/backup` | Create timestamped backup | `{ Path: string }` |

**Note**: C# returns PascalCase JSON. Frontend services handle both PascalCase and camelCase property names.

## LSLib Integration

LSLib is referenced via **local DLLs** (not NuGet) in `backend/lib/lslib/Packed/`. Both Core and Test csproj files must include the same `<Reference>` and native DLL copy entries.

Key LSLib classes:
- `PackageReader` / `PackageWriter` - read/write .lsv packages
- `LSFReader` / `LSFWriter` - parse/write binary .lsf format to `Resource` tree
- `Resource` → `Node` → `Attributes` - tree structure for save data

**Divine.exe** (`backend/lib/lslib/Packed/Tools/Divine.exe`) is used for extract-package and create-package operations. `SaveService.ExecuteDivineAsync()` wraps process execution.

## Save File Flow

1. **Load**: `PackageReader.Read(path)` extracts .lsv → finds `Globals.lsf` → `LSFReader` parses to `Resource` tree
2. **Gold detection** (dual strategy):
   - **Strategy 1** (legacy): `SearchNodeForGold()` traverses LSF node tree for `MapKey`/`Stats`/`ItemName` containing "Gold"
   - **Strategy 2** (modern): `TryParseScratchBufferGold()` → `ScratchBufferParser.Parse()` reads NewAge LSMF binary blob → extracts ECS StackEntry components with quantity > 1
3. **Modify**: `CreateBackup()` → Divine.exe extracts to temp dir → `LSFReader` reads Globals.lsf → `ModifyGoldInResource()` sets first gold item to new amount, others to 1 → `LSFWriter` writes back → Divine.exe repacks → cleanup temp

### ScratchBuffer (LSMF) Format

Modern BG3 saves store entity data in a binary LSMF blob inside `Globals.lsf > NewAge > NewAge` attribute:
- 48-byte header: magic ("LSMF"), version, hash, block offset/size, component count
- Name block: UTF-8 component name strings
- Component entry table: 48 bytes each (name offset, hash, struct size, element count, data offset)
- Component data: flat arrays of fixed-size structs

Key components: `game.inventory.v0.StackEntry` (quantities), `game.stats.v0.HealthComponent`, `game.experience.v0.ExperienceComponent`

## Key File Locations

- Default BG3 saves: `%LOCALAPPDATA%\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story`
- Frontend API config: `frontend-ng/src/environments/environment.ts` (`apiUrl`)
- CORS origins: `backend/BG3.SaveEditor.Api/Program.cs`
- Save structure docs: `FIELD_MAPPING.md`, `GOLD_STORAGE_FIX.md`
- Roadmap: `BACKLOG.md` (Phase 7 - C# migration in progress)

## Conventions

- Backend: async/await throughout, `Console.WriteLine("[DEBUG]...")` for logging
- Frontend: services are `providedIn: 'root'` singletons; components use inline templates and styles
- Gold modification always creates a backup first
- When adding LSLib references to new projects, copy the full `<ItemGroup>` block from `BG3.SaveEditor.Core.csproj`
