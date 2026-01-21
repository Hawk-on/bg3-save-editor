# BG3 Save Editor

A cross-platform save game editor for Baldur's Gate 3, built with Tauri, Vue.js, and LSLib.

## Features
- **Modern UI**: Clean, responsive interface built with Vue 3.
- **Save Parsing**: Extracts and parses Larian Save Packages (.lsv) using `divine.exe`.
- **Game Data Inspection**: View raw extracted data (JSON/XML).
- **Extensible**: Designed to support more features via `Backlog`.

## Setup
1. Clone the repository.
2. Run `npm install` to install dependencies.
3. Run `npm run tauri dev` to start the development app.

## Documentation
- [Project Backlog](BACKLOG.md)
- [Field Mapping (Save Structure)](FIELD_MAPPING.md)
- [Viability Analysis](VIABILITY_ANALYSIS.md)

## License
MIT
