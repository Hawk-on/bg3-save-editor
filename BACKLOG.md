# Project Backlog & Roadmap

This document serves as a central repository for planned features, known issues, and future ideas for the BG3 Save Editor. It is intended for developers and LLMs to understand the future direction of the project.

## 🚀 Roadmap

### Phase 1: MVP (Current)
- [x] Project Setup (Tauri + Vue + LSLib)
- [x] Save Extraction & Repacking
- [ ] Basic Stat Editing (Gold, XP)
- [ ] Backup System

### Phase 2: Enhanced Editing
- [ ] **Ability Score Editor**: Edit Str, Dex, Con, Int, Wis, Cha.
- [ ] **Inventory Manager**: Add/Remove items, change quantities.
- [ ] **Party Management**: Edit approval ratings, revive dead characters.

### Phase 3: Advanced Features
- [ ] **Quest Editor**: View and toggle quest flags (Requires database of flags).
- [ ] **Class & Race Respec**: Change character class/subclass and race (High complexity).
- [ ] **Appearance Editor**: Modify hairstyle, colors, etc.
- [ ] **Mod Support**: Better handling of modded save data.

## 💡 Ideas & Future Improvements
- **Performance**: Optimizing `Globals.lsx` parsing (currently ~29MB XML). Consider using stream-based parsing or LSLib's direct manipulation if possible via C# backend.
- **UI/UX**: visual representation of inventory (icons).
- **Auto-Backups**: Implement a retention policy (e.g., keep last 5 backups).
- **Diff Tool**: Compare two save files to see what changed.

## 🐛 Known Issues & Limitations
- **Large File Size**: `WLD_Main_A.lsx` can be 90MB+. Text-based parsing is slow.
- **LSLib Dependency**: Relies on `divine.exe`. Updates to the game may break LSLib compatibility.

## 🤖 LLM Context
For LLMs assisting with this project:
- **Architecture**: Tauri (Rust) + Vue (TypeScript).
- **Core Tool**: LSLib (`divine.exe`) is used for all file operations (extract/repack/convert).
- **Key Files**:
    - `Globals.lsx`: Game variables, some inventory/items (if global).
    - `WLD_Main_A.lsx`: Main level character data, entities, local inventory.
    - `SaveInfo.json`: High-level metadata (Party level, location).
