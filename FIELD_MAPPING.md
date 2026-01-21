# BG3 Save File Field Mapping

This document details the structure of Baldur's Gate 3 save files and the location of specific editable fields.

## File Structure
A `.lsv` save package contains:
- **`SaveInfo.json`**: Readable JSON summary (Party, Level, Version).
- **`Globals.lsf`** (-> `.lsx`): Global variables, game state, mod data.
- **`LevelCache/WLD_Main_A.lsf`** (-> `.lsx`): The main world state, containing characters, NPCs, and items in the active level.

## Field Locations

### 1. Currency (Gold)
- **File**: `Globals.lsx` (usually) OR `WLD_Main_A.lsx` (if in specific inventory)
- **Search Key**: `OBJ_GoldCoin`
- **Structure**:
  ```xml
  <node id="Item">
      <!-- ... -->
      <attribute id="Stats" type="FixedString" value="OBJ_GoldCoin" />
      <attribute id="StackAmount" type="int32" value="1234" /> <!-- This is the value to edit -->
      <!-- ... -->
  </node>
  ```

### 2. Character Experience & Level
- **File**: `SaveInfo.json` (Read-only/Summary?) & `WLD_Main_A.lsx` (Actual data)
- **Key**: `Experience`
- **Notes**: Modifying `SaveInfo.json` might not affect the game state. The source of truth is likely in the XML.

### 3. Ability Scores
- **File**: `WLD_Main_A.lsx`
- **Location**: Inside `Character` nodes.
- **Structure**: TBD (Needs verification)

### 4. Basic Attributes
- **HP**: `WLD_Main_A.lsx` -> `Character` -> `Stats` -> `CurrentHP` / `MaxHP` (TBD)
- **Name**: `WLD_Main_A.lsx` -> `Character` -> `Name` (often "Tav" or Origin name)

## UUID Reference
- **Tav (Player)**: Variable. Search for "Tav" or check `SaveInfo.json`.
- **Shadowheart**: `3ed74f06-3c60-42dc-83f6-f034cb47c679`
- **Astarion**: `c7c13742-bacd-460a-8f65-f864fe41f255`
- **Gale**: `ad9af97d-75da-406a-ae13-7071c563f604`
- **Lae'zel**: `58a69333-40bf-8358-1d17-fff240d7b12d`
- **Wyll**: `c774d764-4a17-48dc-b470-32ace9ce447d`
- **Karlach**: `2c76687d-93a2-477b-8b18-8a14b549304c`
