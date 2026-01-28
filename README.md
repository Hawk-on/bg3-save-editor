# BG3 Save Editor

A cross-platform save game editor for Baldur's Gate 3, built with Tauri, Vue.js, and LSLib.

## Features
- **Modern UI**: Clean, responsive dark interface built with Vue 3 and composable architecture.
- **Save Parsing**: Extracts and parses Larian Save Packages (.lsv) using `divine.exe`.
- **Gold Editing**: View and modify character wealth with automatic backups.
- **Game Data Inspection**: View raw extracted data (JSON/XML).
- **Extensible Architecture**: Composables-based frontend and refactored backend for easy feature additions.

## Project Structure

```
src/
├── composables/          # Vue 3 Composition API logic layer
│   ├── useApi.ts        # Shared Tauri command invocation
│   ├── useLsLib.ts      # LSLib tools management
│   ├── useSaveList.ts   # Save discovery & listing
│   ├── useSaveExtraction.ts  # Save extraction & conversion
│   └── useGoldEditor.ts # Gold modification logic
├── components/           # UI components
│   ├── LslibStatus.vue  # System status display
│   ├── SavesFolder.vue  # Folder selection & save list
│   ├── SaveInfo.vue     # Campaign metadata
│   └── GoldEditor.vue   # Gold editing UI
├── styles/              # Centralized CSS
│   ├── globals.css      # Theme variables & resets
│   └── components.css   # Reusable component styles
└── App.vue              # Root component

src-tauri/src/
├── main.rs              # Tauri app entry
├── lib.rs               # Library exports
├── commands.rs          # Tauri command handlers (helper functions)
├── bg3_io.rs            # Divine.exe wrapper (execute_divine_command consolidation)
└── save_model.rs        # Save file parsing & modification (8 helper functions)
```

## Setup
1. Clone the repository.
2. Run `npm install` to install dependencies.
3. Ensure LSLib is in `tools/lslib/Packed/Tools/Divine.exe`.
4. Run `npm run tauri dev` to start the development app.

## Architecture Highlights

### Frontend (Vue 3 with Composition API)
- **Composables**: Business logic extracted into reusable composables for testability
- **Components**: UI-focused components with minimal state management
- **Styles**: Centralized CSS with CSS variables for theme consistency
- **Type Safety**: Full TypeScript support across all files

### Backend (Rust + Tauri)
- **bg3_io.rs**: Consolidated Divine.exe commands with helper functions
- **save_model.rs**: 8 focused helper functions for XML parsing and gold modification
- **commands.rs**: Extracted path validation and directory scanning helpers
- **Error Handling**: Comprehensive error messages for user feedback

## Documentation
- [Implementation Walkthrough](WALKTHROUGH.md) - Feature details & usage
- [Project Backlog](BACKLOG.md) - Task tracking & roadmap
- [Field Mapping (Save Structure)](FIELD_MAPPING.md) - BG3 save format reference
