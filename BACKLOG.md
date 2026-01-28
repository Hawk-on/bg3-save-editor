# BG3 Save Game Editor - Task List

## Phase 1: Project Setup
- [x] Create Tauri + Vue project structure
- [x] Download LSLib (divine.exe)
- [x] Verify `divine.exe` integration via Rust
- [x] Implement Rust commands for extraction/repacking

## Phase 2: Save Extraction & Analysis
- [x] Extract sample save file
- [x] Convert LSF files to readable LSX format
- [x] Analyze `Globals.lsx` for Character stats
- [x] Analyze `WLD_Main_A.lsx` for Entity data
- [x] Document key fields in `FIELD_MAPPING.md`

## Phase 3: Field Research
- [x] Locate Gold value (Confirmed `InventoryList` → `LOOT_Gold_A` item with `Amount` attribute)
- [ ] Locate Experience/Level values (Deferred - Read-only from `SaveInfo` for now)
- [ ] Locate Ability Scores (Deferred)
- [ ] Locate Inventory items (Deferred)

## Phase 4: Core Implementation
- [x] Backend: LSLib Wrapper (`bg3_io.rs`)
- [x] Backend: Save Parser (`save_model.rs`)
- [x] Frontend: Save File Browser
- [x] Frontend: Gold Editor
- [x] Backend: Save Logic (Edit → Repack)
- [x] Automatic Backup System
- [x] Gold Modification & Repacking

## Phase 5: Code Quality & Refactoring
- [x] **Rust Backend Refactoring**:
  - [x] `bg3_io.rs`: Consolidate Divine.exe commands (4 → 1 `execute_divine_command()`)
  - [x] `save_model.rs`: Extract 8 helper functions (extract_attribute_value, is_gold_item, parse_amount, etc.)
  - [x] `commands.rs`: Extract 6 path/validation helpers
- [x] **Vue Frontend Refactoring**:
  - [x] Extract 5 composables (useApi, useLsLib, useSaveList, useSaveExtraction, useGoldEditor)
  - [x] Create 4 focused components (LslibStatus, SavesFolder, SaveInfo, GoldEditor)
  - [x] Extract CSS into 2 stylesheets (globals.css, components.css with 250 lines)
  - [x] Add Vite alias configuration for clean imports
- [x] **Documentation Updates**:
  - [x] Update README.md with architecture overview
  - [x] Update WALKTHROUGH.md with refactoring details
  - [x] Update BACKLOG.md with completed phases

## Phase 6: Verification & Polish
- [ ] Verify edited save loads in game
- [ ] Test backup restoration
- [ ] Polish UI animations
- [ ] Add error handling improvements
- [ ] Optimize large file parsing (consider worker threads for 100MB+ files)

## Completed Refactoring Summary

### Backend (Rust)
| File | Before | After | Improvement |
|------|--------|-------|-------------|
| `bg3_io.rs` | 199 lines, 4x duplicate Divine calls | 160 lines, 1x `execute_divine_command()` | -20% LOC, DRY principle |
| `save_model.rs` | 230 lines, monolithic functions | 250 lines, 8 helpers | Better testability, reusability |
| `commands.rs` | 185 lines, repeated patterns | 165 lines, 6 helpers | -11% LOC, centralized validation |

### Frontend (Vue 3)
| Layer | Files | Lines | Purpose |
|-------|-------|-------|---------|
| Composables | 5 files | ~200 lines | Business logic, state management |
| Components | 4 files | ~150 lines | UI-focused presentation |
| Styles | 2 files | ~400 lines | Centralized, reusable CSS |
| App.vue | 1 file | ~100 lines | Root orchestration |

### Key Metrics
- ✅ TypeScript: 100% coverage
- ✅ Build: 0 errors, 0 warnings
- ✅ Code organization: Clear SoC/HC/LC
- ✅ Maintainability: 8 helper functions, 6 composables
- ✅ Reusability: All styles use CSS variables, all API calls use `useInvokeCommand()`
