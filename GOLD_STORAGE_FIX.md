# BG3 Gold Storage Fix

## Problem Discovery
Initial attempts to modify gold failed because the code was searching for gold in the wrong location. The code was looking for `OBJ_GoldCoin` and `OBJ_GoldPile` as placement objects in the main Items list (Globals.lsx and level cache files).

### What Was Wrong
- Gold items (OBJ_GoldCoin, OBJ_GoldPile) are **placement objects** only
- They exist in the main level Items list as visual/interactable objects
- But they have **NO Amount attribute** - they're just props/decorations
- The placement objects had no way to store or retrieve the actual gold quantity

### Actual Gold Storage Location
Gold in BG3 is stored as **Inventory Items** within character inventory sections:
- Location: `<node id="InventoryList">` → `<node id="Item">` blocks within character data
- Item Template: Items with `ItemName="LOOT_Gold_A"` (or similar LOOT_Gold variants)
- Amount Field: `<attribute id="Amount" type="int32" value="X"/>` 
- The Amount attribute stores the actual gold quantity

## Solution Implemented
Modified `save_model.rs` functions:

### `get_gold_info(content: &str) -> SaveState`
**New Logic:**
1. Search for `InventoryList` nodes (character inventory sections)
2. Within each inventory, find `Item` nodes
3. Check if the item has `LOOT_Gold` or `OBJ_Gold` in the `ItemName` attribute
4. Read the `Amount` attribute as the gold quantity
5. Sum all gold items to get total

**Key Changes:**
- No longer looks for gold placement objects
- Correctly identifies gold inventory items
- Reads actual Amount values that represent gold totals

### `modify_gold(content: &str, new_amount: i32) -> Result<String, String>`
**New Logic:**
1. Search for Item nodes within InventoryList sections
2. Look-ahead to determine if item contains gold (LOOT_Gold or OBJ_Gold in ItemName)
3. For gold items, modify the `Amount` attribute:
   - First gold item: set to `new_amount` (user's requested total)
   - Subsequent gold items: set to 1 (preserve minimal stacks)
4. Return modified content

**Key Changes:**
- Consolidates multiple gold stacks into a single amount on the first stack
- Properly handles line-by-line modification with correct XML preservation
- Much more reliable since we're modifying the correct attribute location

## Technical Details

### Gold Item Structure in InventoryList
```xml
<node id="InventoryList">
    <children>
        <node id="Item">
            <attribute id="ScoreModifier" type="float" value="1" />
            <attribute id="ItemName" type="LSString" value="LOOT_Gold_A" />
            <attribute id="UUID" type="FixedString" value="..." />
            <attribute id="Amount" type="int32" value="41649" />
            <!-- Other attributes -->
        </node>
    </children>
</node>
```

### Why Placement Objects Don't Store Gold
The `OBJ_GoldCoin` and `OBJ_GoldPile` items in the main level Items list are:
- Visual representation of loot containers
- Used for game interaction (right-click to loot)
- Marked with position/rotation data for placement
- **Not** the actual inventory items (those are in character InventoryList)

## Testing
The fix has been validated by:
1. Finding InventoryList sections in actual BG3 save files
2. Confirming LOOT_Gold items exist within inventory
3. Confirming Amount attributes contain numeric values (e.g., 41649)
4. Rust code compiles without errors with new logic

## Future Improvements
- Consider parsing multiple character inventories (4-player party)
- Add validation to ensure we're modifying the correct character's gold
- Performance optimization for very large inventory lists
