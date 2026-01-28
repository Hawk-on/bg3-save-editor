# BG3 Save Editor - Viability Analysis

## Overview
This document analyzes the technical feasibility of creating a save game editor for Baldur's Gate 3.

## Save File Structure

### Format
- **Container**: `.lsv` files are proprietary Larian Save (LSV) packages
- **Compression**: LZ4 compression with custom headers
- **Internal Structure**: Contains multiple binary (.lsf) and JSON files

### Key Components
1. **SaveInfo.json**: Human-readable metadata (character name, level, playtime, etc.)
2. **Globals.lsf**: Global game state, variables, and mod data
3. **LevelCache/*.lsf**: World state files containing:
   - Character data (stats, inventory, position)
   - NPC states
   - Item locations and properties
   - Quest progress
   - Environmental states

### File Formats
- **LSF (Larian Save Format)**: Binary serialized data structures
- **LSX**: XML representation of LSF files (used for modding/editing)

## Technical Approach

### Tools Required
- **LSLib/divine.exe**: Open-source tool that handles:
  - Package extraction/creation
  - LSF ↔ LSX conversion
  - Proper compression and format handling

### Workflow
1. **Extract**: Unpack `.lsv` → directory of files
2. **Convert**: Binary `.lsf` → readable `.lsx` (XML)
3. **Modify**: Parse and edit XML data
4. **Convert Back**: Modified `.lsx` → `.lsf`
5. **Repack**: Directory → new `.lsv` file

## Feasibility Assessment

### ✅ Viable Features
- **Gold/Currency Editing**: Located in `WLD_Main_A.lsx` as `OBJ_GoldCoin`/`OBJ_GoldPile` items with `StackAmount` attributes
- **Item Duplication**: Items are discrete XML nodes with UUIDs
- **Basic Stats**: Character stats present in XML structure
- **Position Modification**: X/Y/Z coordinates accessible

### ⚠️ Moderately Complex
- **Experience/Leveling**: Multiple interconnected values need synchronization
- **Ability Scores**: Requires understanding stat formulas and dependencies
- **Inventory Management**: Complex parent-child relationships
- **Spell Lists**: Template system with class restrictions

### ❌ High Risk / Not Recommended
- **Quest State Manipulation**: Complex dependencies could break game logic
- **Companion Relationships**: Intricate state machines
- **Story Flags**: Critical to game progression, easy to corrupt
- **Mod Compatibility**: Unpredictable interactions with modified data

## Performance Considerations

### File Sizes
- Typical save: 50-150 MB compressed
- Extracted: 200-500 MB
- Main world file (`WLD_Main_A.lsx`): 100-200 MB XML

### Processing Time
- Extraction: 5-15 seconds
- LSF→LSX conversion: 10-30 seconds per file
- XML parsing: 2-5 seconds
- Repacking: 15-30 seconds

### Memory Requirements
- Loading full XML in memory: ~500 MB - 1 GB
- Streaming parsers recommended for large files

## Safety Mechanisms

### Critical Safeguards
1. **Automatic Backups**: Create timestamped backup before any modification
2. **Validation**: Verify XML structure before repacking
3. **Version Checking**: Ensure save compatibility with current game version
4. **Dry Run Mode**: Preview changes without applying them

### Error Handling
- Corrupted file detection
- Rollback capability
- Detailed error messages
- Save integrity verification

## Risks & Limitations

### Known Risks
- **Save Corruption**: Improper editing can make saves unloadable
- **Game Crashes**: Invalid values may crash the game on load
- **Achievement Loss**: Modified saves flagged in some games
- **Multiplayer Issues**: Modified saves incompatible with clean clients

### Mitigation Strategies
- Always keep original backups
- Test with non-critical saves first
- Document all modifications made
- Provide restore functionality

## Conclusion

**Verdict**: ✅ **VIABLE** for basic features (gold, items, basic stats)

The BG3 save format is well-understood thanks to the modding community and LSLib. Basic modifications like currency editing, item management, and stat tweaking are highly feasible. More complex features require careful implementation and thorough testing.

### Recommended MVP Scope
1. ✅ Gold/Currency Editor (IMPLEMENTED)
2. Basic Stat Viewer/Editor
3. Item Browser
4. Automatic Backup System (IMPLEMENTED)

### Future Enhancements
- Experience/Level Editor
- Inventory Manager
- Ability Score Editor
- Spell List Manager

## References
- [LSLib GitHub](https://github.com/Norbyte/lslib) - Core tool for save manipulation
- [BG3 Modding Wiki](https://wiki.bg3.community/) - Community documentation
- Larian Studios save format (reverse-engineered by community)

---
*Last Updated: January 28, 2026*
