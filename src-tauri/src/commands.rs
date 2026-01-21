use std::path::{Path, PathBuf};
use std::sync::Mutex;
use crate::bg3_io;
use crate::save_model;

/// Shared state to track the current extraction path
pub struct ExtractState {
    pub path: Mutex<Option<PathBuf>>,
}

impl ExtractState {
    pub fn new() -> Self {
        Self {
            path: Mutex::new(None),
        }
    }
}

/// Validates that a path doesn't contain path traversal patterns
fn validate_path_safety(path: &str) -> Result<(), String> {
    // Check for basic path traversal patterns
    if path.contains("..") || path.contains("../") || path.contains("..\\") {
        return Err("Invalid path: path traversal patterns are not allowed".to_string());
    }
    
    // Additional check: validate the path doesn't escape to unexpected locations
    // For now, we rely on basic string checks since canonicalize requires the path to exist
    // In production, consider more comprehensive path validation based on allowed directories
    
    Ok(())
}

/// Gets a unique temporary directory for extraction
fn get_temp_extract_path() -> Result<PathBuf, String> {
    let temp_dir = std::env::temp_dir();
    let unique_id = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map_err(|e| e.to_string())?
        .as_secs();
    let extract_path = temp_dir.join(format!("bg3_save_editor_{}", unique_id));
    Ok(extract_path)
}

#[tauri::command]
pub fn check_lslib_status() -> Result<String, String> {
    // bg3_io helper might need to be public or we check manually
    // Just a quick check
    let tool_path_src = "../tools/lslib/Packed/Tools/Divine.exe";
    let tool_path_root = "tools/lslib/Packed/Tools/Divine.exe";
    
    if Path::new(tool_path_src).exists() || Path::new(tool_path_root).exists() {
        Ok("LSLib tools found.".to_string())
    } else {
        Err("LSLib tools not found (Divine.exe missing).".to_string())
    }
}

#[tauri::command]
pub async fn extract_save(
    save_path: String,
    state: tauri::State<'_, ExtractState>
) -> Result<String, String> {
    let extract_path = get_temp_extract_path()?;
    let extract_path_str = extract_path.to_string_lossy().to_string();
    
    // Clean up old extraction if it exists
    if extract_path.exists() {
        std::fs::remove_dir_all(&extract_path).map_err(|e| e.to_string())?;
    }
    std::fs::create_dir_all(&extract_path).map_err(|e| e.to_string())?;

    // Extract using spawn_blocking to avoid blocking async executor
    let save_path_clone = save_path.clone();
    let extract_path_clone = extract_path_str.clone();
    tokio::task::spawn_blocking(move || {
        bg3_io::extract_save(&save_path_clone, &extract_path_clone)
    })
    .await
    .map_err(|e| format!("Task join error: {}", e))??;

    // Convert Globals.lsf -> lsx
    let globals_lsf = extract_path.join("Globals.lsf");
    let globals_lsx = extract_path.join("Globals.lsx");
    if globals_lsf.exists() {
        bg3_io::convert_lsf_to_lsx(
            &globals_lsf.to_string_lossy(),
            &globals_lsx.to_string_lossy()
        )?;
    }

    // Convert WLD_Main_A.lsf -> lsx
    let level_lsf = extract_path.join("LevelCache/WLD_Main_A.lsf");
    let level_lsx = extract_path.join("LevelCache/WLD_Main_A.lsx");
    if level_lsf.exists() {
        bg3_io::convert_lsf_to_lsx(
            &level_lsf.to_string_lossy(),
            &level_lsx.to_string_lossy()
        )?;
    }

    // Store the extraction path in state
    *state.path.lock()
        .map_err(|e| format!("Failed to acquire state lock: {}", e))? = Some(extract_path.clone());

    Ok(format!("Save extracted and converted to {}", extract_path_str))
}

#[tauri::command]
pub async fn read_save_info(state: tauri::State<'_, ExtractState>) -> Result<serde_json::Value, String> {
    let extract_path = state.path.lock()
        .map_err(|e| format!("Failed to acquire state lock: {}", e))?
        .as_ref()
        .ok_or("No save extracted yet")?
        .clone();
    
    let info_path = extract_path.join("SaveInfo.json");
    if !info_path.exists() {
        return Err("SaveInfo.json not found. Extract a save first.".to_string());
    }

    // Use spawn_blocking for file I/O
    let content = tokio::task::spawn_blocking(move || {
        std::fs::read_to_string(info_path)
    })
    .await
    .map_err(|e| format!("Task join error: {}", e))?
    .map_err(|e| e.to_string())?;
    
    let json: serde_json::Value = serde_json::from_str(&content).map_err(|e| e.to_string())?;
    
    Ok(json)
}

#[tauri::command]
pub async fn get_gold_count(state: tauri::State<'_, ExtractState>) -> Result<save_model::SaveState, String> {
    let extract_path = state.path.lock()
        .map_err(|e| format!("Failed to acquire state lock: {}", e))?
        .as_ref()
        .ok_or("No save extracted yet")?
        .clone();
    
    let lsx_path = extract_path.join("LevelCache/WLD_Main_A.lsx");
    if !lsx_path.exists() {
        return Err("Level data not found (WLD_Main_A.lsx).".to_string());
    }

    // Use spawn_blocking for large file I/O to prevent UI freezes
    let content = tokio::task::spawn_blocking(move || {
        std::fs::read_to_string(lsx_path)
    })
    .await
    .map_err(|e| format!("Task join error: {}", e))?
    .map_err(|e| e.to_string())?;
    
    // Parse
    let gold_info = save_model::get_gold_info(&content);
    Ok(gold_info)
}

#[tauri::command]
pub fn verify_divine_integration() -> Result<String, String> {
    bg3_io::verify_divine()
}

#[tauri::command]
pub async fn update_gold(
    new_gold: i32,
    state: tauri::State<'_, ExtractState>
) -> Result<String, String> {
    let extract_path = state.path.lock()
        .map_err(|e| format!("Failed to acquire state lock: {}", e))?
        .as_ref()
        .ok_or("No save extracted yet")?
        .clone();
    
    let lsx_path = extract_path.join("LevelCache/WLD_Main_A.lsx");
    if !lsx_path.exists() {
        return Err("Level data not found (WLD_Main_A.lsx). Extract a save first.".to_string());
    }

    // Use spawn_blocking for large file I/O
    let lsx_path_clone = lsx_path.clone();
    let content = tokio::task::spawn_blocking(move || {
        std::fs::read_to_string(lsx_path_clone)
    })
    .await
    .map_err(|e| format!("Task join error: {}", e))?
    .map_err(|e| e.to_string())?;
    
    // Update gold
    let updated_content = save_model::update_gold_in_lsx(&content, new_gold)?;
    
    // Write back to file using spawn_blocking
    tokio::task::spawn_blocking(move || {
        std::fs::write(lsx_path, updated_content)
    })
    .await
    .map_err(|e| format!("Task join error: {}", e))?
    .map_err(|e| e.to_string())?;
    
    Ok(format!("Gold updated to {}", new_gold))
}

#[tauri::command]
pub async fn repack_save(
    output_path: String,
    state: tauri::State<'_, ExtractState>
) -> Result<String, String> {
    // Validate output path for security
    validate_path_safety(&output_path)?;
    
    let extract_path = state.path.lock()
        .map_err(|e| format!("Failed to acquire state lock: {}", e))?
        .as_ref()
        .ok_or("No save extracted yet")?
        .clone();
    
    if !extract_path.exists() {
        return Err("No extracted save found. Extract a save first.".to_string());
    }
    
    // Convert LSX back to LSF
    let level_lsx = extract_path.join("LevelCache/WLD_Main_A.lsx");
    let level_lsf = extract_path.join("LevelCache/WLD_Main_A.lsf");
    
    if level_lsx.exists() {
        bg3_io::convert_lsx_to_lsf(
            &level_lsx.to_string_lossy(),
            &level_lsf.to_string_lossy()
        )?;
    }
    
    // Also convert Globals if it was converted
    let globals_lsx = extract_path.join("Globals.lsx");
    let globals_lsf = extract_path.join("Globals.lsf");
    
    if globals_lsx.exists() {
        bg3_io::convert_lsx_to_lsf(
            &globals_lsx.to_string_lossy(),
            &globals_lsf.to_string_lossy()
        )?;
    }
    
    // Repack into new .lsv file
    bg3_io::repack_save(&extract_path.to_string_lossy(), &output_path)?;
    
    Ok(format!("Save repacked successfully to {}", output_path))
}

#[tauri::command]
pub fn create_backup(original_path: String) -> Result<String, String> {
    // Validate that the path ends with .lsv extension
    if !original_path.to_lowercase().ends_with(".lsv") {
        return Err("Invalid file: only .lsv save files can be backed up".to_string());
    }
    
    // Check if original exists
    if !Path::new(&original_path).exists() {
        return Err(format!("Original save file not found: {}", original_path));
    }
    
    let backup_path = format!("{}.backup", original_path);
    
    // Check if backup already exists
    if Path::new(&backup_path).exists() {
        return Err(format!("Backup already exists: {}. Delete it first or use a different name to avoid overwriting.", backup_path));
    }
    
    // Copy to backup
    std::fs::copy(&original_path, &backup_path)
        .map_err(|e| format!("Failed to create backup: {}", e))?;
    
    Ok(format!("Backup created: {}", backup_path))
}
