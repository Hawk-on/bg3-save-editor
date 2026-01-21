# Logic Implemented: Save Extraction & Gold Reading

I have implemented the core foundation of the BG3 Save Editor.

## 1. Architecture
- **Backend (Rust)**:
  - `bg3_io.rs`: Wraps `Divine.exe` to extract `.lsv` packages and convert `.lsf` binaries to `.lsx` XML.
  - `save_model.rs`: Parses the huge XML files to identify Gold items (`OBJ_GoldCoin`, `OBJ_GoldPile`) and sum their `StackAmount` or `Amount`.
  - `commands.rs`: Exposes `extract_save`, `read_save_info`, and `get_gold_count` to the frontend.
- **Frontend (Vue/TS)**:
  - complete UI overhaul with a dark, premium aesthetic.
  - Integration with backend to loading, extracting, and displaying save data.

## 2. Capabilities
- **Load Save**: Users can input the path to a `.lsv` save file.
- **Extraction**: The app automatically extracts the save and converts internal files to readable XML.
- **Save Info**: Displays Campaign Name, Level, and Difficulty.
- **Wealth Management**: Scans the save file and displays total Gold count across all inventory stacks found.

## 3. Next Steps (Phase 4.b)
- **Implement Writing**: Add `modify_gold` logic in `save_model.rs` and `repack_save` in `bg3_io.rs`.
- **Frontend Editing**: Turn the Gold display into an input field to allow modification.
- **Repack**: Add "Save Changes" button to pack the XML back into `.lsv`.

## 4. How to Test
1. Run the app (`npm run tauri dev`).
2. Enter the path to your save file (e.g. `C:\Users\Hawkon\AppData\Local...`).
3. Click "Load & Extract".
4. Verify the Total Gold matches your in-game amount.
