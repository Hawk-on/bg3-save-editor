# BG3 Save Game Editor - Task List

## Phase 1: Project Setup
- [x] Create Tauri + Vue project structure
- [x] Download LSLib (divine.exe)
- [ ] Verify `divine.exe` integration via Rust
- [ ] Implement Rust commands for extraction/repacking

## Phase 2: Save Extraction & Analysis
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

## Phase 4: Core Implementation
- [x] Backend: LSLib Wrapper (`bg3_io.rs`)
- [x] Backend: Save Parser (`save_model.rs`)
- [x] Frontend: Save File Browser (Path Input Implemented)
- [x] Frontend: Basic Stats Editor (Gold - View Only Implemented)
- [ ] Backend: Save Logic (Edit -> Repack)

## Phase 5: Verification & Polish
- [ ] Verify edited save loads in game
- [ ] Test backup restoration
- [ ] Polish UI
