// use regex::Regex;

#[derive(Debug, Clone, serde::Serialize, serde::Deserialize)]
pub struct GoldItem {
    pub item_id: String, // Just a unique identifier or offset?
    pub amount: i32,
    pub original_text_range: (usize, usize), // Start/End byte indices in the file?
}

// Simple struct to pass data to frontend
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

pub fn get_gold_info(content: &str) -> SaveState {
    // Regex to find Item nodes that look like Gold
    // Pattern: <node id="Item">(.*?)</node> 
    // But since it's nested, regex is dangerous.
    // Larian LSX structure is usually flat indentation.
    // Let's use a simpler heuristic:
    // Find "OBJ_GoldCoin" or "OBJ_GoldPile"
    // Then search backwards for "<node id="Item">" and forwards for "</node>"?
    // Or just search for the specific attribute lines if we assume indentation.
    
    // Better strategy for MVP:
    // Iterate over the file content using a sliding window or just Regex on the whole string?
    // 100MB string is fine for Regex on modern PC.
    
    // Pattern to capture the Item block is hard because of nesting <children>.
    // However, the attributes are usually direct children of <node id="Item">.
    
    let mut items = Vec::new();
    let mut total_gold = 0;

    // Use fast string searching. Quick-xml would be better, but we are doing MVP.
    // Larian LSX usually has one node per block.
    // We look for segments starting with <node id="Item">
    
    let parts: Vec<&str> = content.split("<node id=\"Item\">").collect();
    
    // Skip the first part as it's before the first item
    for part in parts.iter().skip(1) {
        // Find the closure of this node roughly.
        // Actually, we just need to ensure "Stats" is present before the next node starts.
        // Splitting by node id="Item" handles the separation well enough for top-level search.
        // Note: Child items (in containers) will be inside the chunk.
        
        // Check if this chunk contains OBJ_Gold...
        if let Some(stats_match) = part.find("value=\"OBJ_Gold") {
            // Further verification it is an attribute Stats
            // But value="OBJ_Gold..." is likely unique enough for MVP.
            
            // Extract the stats ID for display
            // E.g. value="OBJ_GoldPile"
            let end_quote = part[stats_match + 7..].find('"').unwrap_or(0);
            let name = &part[stats_match + 7 .. stats_match + 7 + end_quote];
            
            // Look for amount in this part
            // Try StackAmount first
            let mut amount = 1;
            
            // Regex for amount: id="StackAmount" type="int32" value="(\d+)"
            // Need to create regex inside (inefficient) or use static.
            // Using simple string parsing for speed.
            
            if let Some(stack_idx) = part.find("id=\"StackAmount\"") {
                // Find value="..." after that
                if let Some(val_idx) = part[stack_idx..].find("value=\"") {
                    let absolute_val_idx = stack_idx + val_idx + 7;
                    let val_end = part[absolute_val_idx..].find('"').unwrap_or(0);
                    let val_str = &part[absolute_val_idx..absolute_val_idx + val_end];
                    if let Ok(v) = val_str.parse::<i32>() {
                        amount = v;
                    }
                }
            } else if let Some(amt_idx) = part.find("id=\"Amount\"") {
                 // Fallback to Amount?
                 if let Some(val_idx) = part[amt_idx..].find("value=\"") {
                    let absolute_val_idx = amt_idx + val_idx + 7;
                    let val_end = part[absolute_val_idx..].find('"').unwrap_or(0);
                    let val_str = &part[absolute_val_idx..absolute_val_idx + val_end];
                    if let Ok(v) = val_str.parse::<i32>() {
                        amount = v;
                    }
                }
            }
            
            // Only count it if we found a Gold stat
            if name.contains("Gold") {
                total_gold += amount;
                items.push(GoldItemDisplay {
                    name: name.to_string(),
                    amount,
                });
            }
        }
    }

    SaveState {
        total_gold,
        items,
    }
}

pub fn parse_and_sum_gold(content: &str) -> i32 {
    get_gold_info(content).total_gold
}
