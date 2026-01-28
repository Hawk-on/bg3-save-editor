// ============================================================================
// Data Structures
// ============================================================================

#[derive(Debug, serde::Serialize)]
pub struct SaveState {
    pub total_gold: i32,
    pub items: Vec<GoldItemDisplay>,
}

#[derive(Debug, serde::Serialize)]
pub struct GoldItemDisplay {
    pub name: String,
    pub amount: i32,
}

// ============================================================================
// Helper Functions for XML/LSX Parsing
// ============================================================================

/// Extract an attribute value from XML/LSX text by attribute ID
/// Searches for `id="attr_id"` and then finds the corresponding `value="..."`
fn extract_attribute_value(text: &str, attr_id: &str) -> Option<String> {
    let search_str = format!("id=\"{}\"", attr_id);
    let attr_idx = text.find(&search_str)?;
    
    // Look for value=" after the attribute ID
    let value_search = "value=\"";
    let value_idx = text[attr_idx..].find(value_search)?;
    let absolute_value_idx = attr_idx + value_idx + value_search.len();
    
    // Find the closing quote
    let value_end = text[absolute_value_idx..].find('"')?;
    
    Some(text[absolute_value_idx..absolute_value_idx + value_end].to_string())
}

/// Check if an item contains gold indicators
fn is_gold_item(item_text: &str) -> bool {
    item_text.contains("LOOT_Gold") || item_text.contains("OBJ_Gold")
}

/// Parse an amount value, defaulting to 1 if parsing fails
fn parse_amount(value_str: &str) -> i32 {
    value_str.parse::<i32>().unwrap_or(1)
}

/// Find and sum all gold in character inventory
/// Returns a SaveState with total gold and itemized breakdown
pub fn get_gold_info(content: &str) -> SaveState {
    let mut items = Vec::new();
    let mut total_gold = 0;
    let mut count = 0;

    // Log to debug file
    let mut debug_file = std::fs::File::create("C:\\Git\\BG3 savegame editor\\gold_debug.txt").ok();
    if let Some(ref mut f) = debug_file {
        use std::io::Write;
        let _ = writeln!(f, "\nDEBUG: Searching for gold in ItemList nodes\n");
    }
    
    // Find all ItemList sections
    let inventory_parts: Vec<&str> = content.split("<node id=\"ItemList\">").collect();
    
    if let Some(ref mut f) = debug_file {
        use std::io::Write;
        let _ = writeln!(f, "Found {} ItemList sections\n", inventory_parts.len());
    }
    
    // Process each inventory section (skip the part before the first InventoryList)
    for inv_part in inventory_parts.iter().skip(1) {
        process_inventory_section(inv_part, &mut items, &mut total_gold, &mut count, &mut debug_file);
    }
    
    if let Some(ref mut f) = debug_file {
        use std::io::Write;
        let _ = writeln!(f, "\nTOTAL GOLD FOUND: {} (across {} items)\n", total_gold, count);
    }
    
    SaveState { total_gold, items }
}

/// Process a single inventory section to extract gold items
fn process_inventory_section(
    inv_part: &str,
    items: &mut Vec<GoldItemDisplay>,
    total_gold: &mut i32,
    count: &mut i32,
    debug_file: &mut Option<std::fs::File>,
) {
    use std::io::Write;
    
    // Limit scope to just this inventory section
    let end_inventory = inv_part.find("</node>").unwrap_or(inv_part.len());
    let inv_section = &inv_part[..end_inventory];
    
    // Find all Item nodes within this inventory
    let item_parts: Vec<&str> = inv_section.split("<node id=\"Item\">").collect();
    
    // Process each item (skip the part before the first Item)
    for item_part in item_parts.iter().skip(1) {
        if is_gold_item(item_part) {
            *count += 1;
            
            if let Some(ref mut f) = debug_file {
                let _ = writeln!(f, "\n=== GOLD ITEM #{} ===\n", count);
                let preview_len = std::cmp::min(1000, item_part.len());
                let _ = writeln!(f, "{}\n", &item_part[..preview_len]);
            }
            
            // Extract gold amount
            let amount = extract_attribute_value(item_part, "Amount")
                .map(|v| parse_amount(&v))
                .unwrap_or(1);
            
            // Extract item name for display
            let name = extract_attribute_value(item_part, "ItemName")
                .unwrap_or_else(|| "Gold".to_string());
            
            if let Some(ref mut f) = debug_file {
                let _ = writeln!(f, "Amount: {}, Name: {}\n", amount, name);
            }
            
            *total_gold += amount;
            items.push(GoldItemDisplay { name, amount });
        }
    }
}

#[allow(dead_code)]
pub fn parse_and_sum_gold(content: &str) -> i32 {
    get_gold_info(content).total_gold
}

/// Modify gold amount in character inventory
/// Consolidates all gold into the first gold item and sets others to 1
pub fn modify_gold(content: &str, new_amount: i32) -> Result<String, String> {
    validate_gold_amount(new_amount)?;
    
    let lines: Vec<&str> = content.lines().collect();
    let mut result_lines = Vec::new();
    let mut found_gold_items = 0;
    let mut modified_first = false;
    let mut in_item_node = false;
    let mut current_item_is_gold = false;
    
    for (i, line) in lines.iter().enumerate() {
        let trimmed = line.trim_start();
        
        // Check if entering an Item node
        if trimmed.starts_with("<node id=\"Item\">") {
            in_item_node = true;
            current_item_is_gold = check_item_is_gold(&lines, i);
        }
        
        // Try to modify Amount attribute in gold items
        if current_item_is_gold && in_item_node && is_amount_attribute(trimmed) {
            found_gold_items += 1;
            let amount_to_set = if !modified_first {
                modified_first = true;
                new_amount
            } else {
                1 // Keep other stacks minimal
            };
            
            if let Some(modified_line) = replace_attribute_value(line, "value=\"", amount_to_set) {
                result_lines.push(modified_line);
                continue;
            }
        }
        
        // Check if exiting Item node
        if in_item_node && trimmed == "</node>" {
            in_item_node = false;
            current_item_is_gold = false;
        }
        
        result_lines.push(line.to_string());
    }
    
    if found_gold_items == 0 {
        return Err("No gold inventory items found in save file".to_string());
    }
    
    println!("Modified {} gold inventory items. Total set to {}", found_gold_items, new_amount);
    Ok(result_lines.join("\n"))
}

/// Validate gold amount is non-negative
fn validate_gold_amount(amount: i32) -> Result<(), String> {
    if amount < 0 {
        Err("Gold amount cannot be negative".to_string())
    } else {
        Ok(())
    }
}

/// Check if an item (at given line index) is a gold item by looking ahead
fn check_item_is_gold(lines: &[&str], start_idx: usize) -> bool {
    // Look up to 50 lines ahead for gold indicators or closing tag
    for j in (start_idx + 1)..std::cmp::min(start_idx + 50, lines.len()) {
        let line = lines[j].trim();
        if line.starts_with("</node>") {
            return false; // Item ended without finding gold
        }
        if lines[j].contains("LOOT_Gold") || lines[j].contains("OBJ_Gold") {
            return true;
        }
    }
    false
}

/// Check if a line is an Amount attribute
fn is_amount_attribute(line: &str) -> bool {
    line.contains("id=\"Amount\"") && line.contains("type=\"int32\"") && line.contains("value=\"")
}

/// Replace an attribute value in a line
/// Handles format like: `<attribute id="Amount" type="int32" value="123" />`
fn replace_attribute_value(line: &str, value_marker: &str, new_value: i32) -> Option<String> {
    let value_start = line.find(value_marker)?;
    let absolute_value_idx = value_start + value_marker.len();
    let value_end = line[absolute_value_idx..].find('"')?;
    
    let before_value = &line[..absolute_value_idx];
    let after_value = &line[absolute_value_idx + value_end..];
    
    Some(format!("{}{}{}", before_value, new_value, after_value))
}
