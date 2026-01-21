# BG3 Save File Field Mapping

This document details the structure of Baldur's Gate 3 save files and the location of specific editable fields.

## File Structure
A `.lsv` save package contains:
- **`SaveInfo.json`**: Readable JSON summary (Party, Level, Version).
- **`Globals.lsf`** (-> `.lsx`): Global variables, game state, mod data.
- **`LevelCache/WLD_Main_A.lsf`** (-> `.lsx`): The main world state, containing characters, NPCs, and items in the active level.

## Field Locations

### 1. Currency (Gold)
- **File**: `WLD_Main_A.lsx` (typically in `Items` region)
- **Search Key**: `OBJ_GoldCoin` or `OBJ_GoldPile`
- **Structure**:
  ```xml
  <node id="Item">
      <!-- ... -->
      <attribute id="Stats" type="FixedString" value="OBJ_GoldCoin" />
      <attribute id="Amount" type="int32" value="1" /> <!-- Single coin -->
      <!-- OR -->
      <attribute id="StackAmount" type="int32" value="1234" /> <!-- Stack of coins -->
      <!-- ... -->
  </node>
  ```
  _Note: `StackAmount` seems to be the field for stacks > 1._

### 2. Character Experience & Level
- **File**: `WLD_Main_A.lsx`
- **Search Key**: `Experience` (often in `Variable` nodes or `Trigger` data).
- **Observed Structure**:
  XP values like `2893` (current level XP) and `9393` (total) appear in `SaveInfo.json`.
  In `WLD_Main_A.lsx`, these values map to `Variable` entries or specific attributes on the Character node.
  _Further verification needed to isolate the **writable** field vs read-only trackers._

### 3. Ability Scores
- **File**: `WLD_Main_A.lsx`
- **Location**: Inside `Character` nodes.
- **Structure**:
  Likely defined in `Stats` -> `Ability` attributes or implicit in `GameObjects`.

## UUID Reference
- **Tav (Player)**: Variable. Origin: "Generic".
  - **SaveInfo.json Position**: `[ -175.23..., 24.91..., 542.43... ]`
  - *Confirmed*: Found character at these coordinates in `WLD_Main_A.lsx`.
- **Shadowheart**: `3ed74f06-3c60-42dc-83f6-f034cb47c679`
- **Astarion**: `c7c13742-bacd-460a-8f65-f864fe41f255`
- **Gale**: `ad9af97d-75da-406a-ae13-7071c563f604` # User mentioned having a Gale save too.
- **Lae'zel**: `58a69333-40bf-8358-1d17-fff240d7b12d`
- **Wyll**: `c774d764-4a17-48dc-b470-32ace9ce447d`
- **Karlach**: `2c76687d-93a2-477b-8b18-8a14b549304c`
