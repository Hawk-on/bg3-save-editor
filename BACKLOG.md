# BG3 Save Game Editor - Task List

## Phase 1: Project Setup ✅
- [x] Create Tauri + Vue project structure
- [x] Download LSLib (divine.exe)
- [x] Verify `divine.exe` integration via Rust
- [x] Implement Rust commands for extraction/repacking

## Phase 2: Save Extraction & Analysis ✅
- [x] Extract sample save file
- [x] Convert LSF files to readable LSX format
- [x] Analyze `Globals.lsx` for Character stats
- [x] Analyze `WLD_Main_A.lsx` for Entity data
- [x] Document key fields in `FIELD_MAPPING.md`

## Phase 3: Field Research
- [x] Locate Gold value (Confirmed `OBJ_GoldPile` / `StackAmount`)
- [ ] Locate Experience/Level values (Deferred - Read-only from `SaveInfo` for now)
- [ ] Locate Ability Scores (Deferred)
- [ ] Locate Inventory items (Deferred)

## Phase 4: Core Implementation ✅
- [x] Backend: LSLib Wrapper (`bg3_io.rs`)
- [x] Backend: Save Parser (`save_model.rs`)
- [x] Frontend: Save File Browser (Path Input Implemented)
- [x] Frontend: Stats Editor (Gold Editing Fully Implemented)
- [x] Backend: Save Logic (Edit -> Repack)

## Phase 5: Verification & Polish ✅
- [x] Test backup restoration
- [x] Polish UI
- [ ] Verify edited save loads in game (Requires actual game testing)

## Completed Features
- ✅ Divine.exe integration verification with error handling
- ✅ Gold value viewing and editing
- ✅ Save file extraction and analysis
- ✅ LSX/LSF conversion for editing
- ✅ Save repacking to .lsv format
- ✅ Automatic backup creation
- ✅ User-friendly UI with status messages
- ✅ Auto-suggested output paths
- ✅ Input validation and error handling
- ✅ Comprehensive user guide in UI
