use std::path::{Path, PathBuf};
use crate::bg3_io;
use crate::save_model;

#[derive(serde::Serialize)]
pub struct SaveEntry {
    pub name: String,
    pub path: String,
    pub modified: String,
}

// ============================================================================
// Helpers for common operations
// ============================================================================

/// Get the path to the temporary save extraction folder
fn get_temp_save_path() -> Result<PathBuf, String> {
    std::env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))
        .map(|dir| dir.join("extracted/temp_save"))
}

/// Expand environment variables in folder paths
fn expand_path_variables(path: &str) -> Result<String, String> {
    if path.contains("%LOCALAPPDATA%") || path.contains("$env:LOCALAPPDATA") {
        let local_appdata = std::env::var("LOCALAPPDATA")
            .or_else(|_| std::env::var("UserProfile")
                .map(|p| format!("{}\\AppData\\Local", p)))
            .map_err(|_| "Could not determine LocalAppData folder".to_string())?;
        Ok(path.replace("%LOCALAPPDATA%", &local_appdata)
            .replace("$env:LOCALAPPDATA", &local_appdata))
    } else {
        Ok(path.to_string())
    }
}

/// Recursively clean directory and create fresh copy
fn clean_and_create_directory(path: &Path) -> Result<(), String> {
    if path.exists() {
        std::fs::remove_dir_all(path).map_err(|e| e.to_string())?;
    }
    std::fs::create_dir_all(path).map_err(|e| e.to_string())
}

/// Find first .lsv file in directory
fn find_save_file_in_directory(dir: &Path) -> Result<Option<PathBuf>, String> {
    let entries = std::fs::read_dir(dir)
        .map_err(|e| format!("Failed to read directory: {}", e))?;
    
    for entry in entries {
        if let Ok(entry) = entry {
            let path = entry.path();
            if path.extension().and_then(|s| s.to_str()) == Some("lsv") {
                return Ok(Some(path));
            }
        }
    }
    Ok(None)
}

/// Get metadata for a save file (name, path, modified time)
fn create_save_entry(dir_path: &Path, lsv_path: &Path) -> Result<SaveEntry, String> {
    let modified = std::fs::metadata(lsv_path)
        .and_then(|m| m.modified())
        .map(|t| format!("{:?}", t))
        .unwrap_or_else(|_| "Unknown".to_string());
    
    Ok(SaveEntry {
        name: dir_path.file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("Unknown")
            .to_string(),
        path: lsv_path.to_string_lossy().to_string(),
        modified,
    })
}

/// Validate that a file exists and is readable
fn validate_file_exists(path: &str) -> Result<(), String> {
    if !Path::new(path).exists() {
        return Err(format!("File not found: {}", path));
    }
    Ok(())
}


#[tauri::command]
pub fn list_saves(folder_path: String) -> Result<Vec<SaveEntry>, String> {
    let expanded_path = expand_path_variables(&folder_path)?;
    let dir = Path::new(&expanded_path);
    
    if !dir.exists() || !dir.is_dir() {
        return Err(format!("Invalid directory: {}", expanded_path));
    }
    
    let mut saves = Vec::new();
    
    let entries = std::fs::read_dir(dir)
        .map_err(|e| format!("Failed to read directory: {}", e))?;
    
    for entry in entries {
        if let Ok(entry) = entry {
            let path = entry.path();
            
            if path.is_dir() {
                if let Ok(lsv_file) = find_save_file_in_directory(&path) {
                    if let Some(lsv_path) = lsv_file {
                        if let Ok(save_entry) = create_save_entry(&path, &lsv_path) {
                            saves.push(save_entry);
                        }
                    }
                }
            }
        }
    }
    
    // Sort by modified time (most recent first)
    saves.sort_by(|a, b| b.modified.cmp(&a.modified));
    
    Ok(saves)
}

#[tauri::command]
pub fn check_lslib_status() -> Result<String, String> {
    match bg3_io::get_divine_path() {
        Ok(path) => Ok(format!("LSLib tools found at: {}", path)),
        Err(e) => Err(e)
    }
}

#[tauri::command]
pub async fn extract_save(save_path: String) -> Result<String, String> {
    validate_file_exists(&save_path)?;
    
    let extract_path = get_temp_save_path()?;
    let extract_path_str = extract_path.to_string_lossy().to_string();
    
    // Clean and recreate extraction directory
    clean_and_create_directory(&extract_path)?;

    // Extract
    bg3_io::extract_save(&save_path, &extract_path_str)?;

    // Convert Globals.lsf -> lsx
    let globals_lsf = format!("{}/Globals.lsf", extract_path_str);
    let globals_lsx = format!("{}/Globals.lsx", extract_path_str);
    if Path::new(&globals_lsf).exists() {
        bg3_io::convert_lsf_to_lsx(&globals_lsf, &globals_lsx)?;
    }

    // Convert WLD_Main_A.lsf -> lsx (Recursively convert all in LevelCache? Or just Main)
    // Finding the level name dynamically would be better, but often it's LevelCache/WLD_Main_A.lsf
    // SaveInfo.json has "Current Level" key.
    let level_lsf = format!("{}/LevelCache/WLD_Main_A.lsf", extract_path_str);
    let level_lsx = format!("{}/LevelCache/WLD_Main_A.lsx", extract_path_str);
    if Path::new(&level_lsf).exists() {
        bg3_io::convert_lsf_to_lsx(&level_lsf, &level_lsx)?;
    }
    
    // Store the original save path for later use
    let marker_path = format!("{}/.source_path", extract_path_str);
    std::fs::write(marker_path, &save_path).map_err(|e| e.to_string())?;

    Ok(format!("Save extracted and converted to {}", extract_path_str))
}

#[tauri::command]
pub async fn read_save_info() -> Result<serde_json::Value, String> {
    let extract_path = get_temp_save_path()?;
    let info_path = extract_path.join("SaveInfo.json");
    
    if !info_path.exists() {
        return Err("SaveInfo.json not found. Extract a save first.".to_string());
    }

    let content = std::fs::read_to_string(info_path).map_err(|e| e.to_string())?;
    serde_json::from_str(&content).map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn get_gold_count() -> Result<save_model::SaveState, String> {
    let extract_path = get_temp_save_path()?;
    let lsx_path = extract_path.join("LevelCache/WLD_Main_A.lsx");
    
    if !lsx_path.exists() {
        return Err("Level data not found (WLD_Main_A.lsx).".to_string());
    }

    // Read content (File can be large 100MB+)
    let content = std::fs::read_to_string(lsx_path).map_err(|e| e.to_string())?;
    Ok(save_model::get_gold_info(&content))
}

#[tauri::command]
pub async fn modify_and_save_gold(new_gold: i32) -> Result<String, String> {
    // Get paths
    let extract_path = get_temp_save_path()?;
    let save_path_marker = extract_path.join(".source_path");
    
    // Read the original save path (stored during extraction)
    let source_save_path = std::fs::read_to_string(&save_path_marker)
        .map_err(|_| "Original save path not found. Please extract a save first.".to_string())?;
    
    // Create backup
    let backup_path = bg3_io::backup_save(&source_save_path)?;
    
    // Read and modify the LSX file
    let lsx_path = extract_path.join("LevelCache/WLD_Main_A.lsx");
    if !lsx_path.exists() {
        return Err("Level data not found (WLD_Main_A.lsx). Extract a save first.".to_string());
    }
    
    let content = std::fs::read_to_string(&lsx_path).map_err(|e| e.to_string())?;
    let modified_content = save_model::modify_gold(&content, new_gold)?;
    
    // Write modified LSX back
    std::fs::write(&lsx_path, modified_content).map_err(|e| e.to_string())?;
    
    // Convert LSX back to LSF
    let lsf_path = extract_path.join("LevelCache/WLD_Main_A.lsf");
    bg3_io::convert_lsx_to_lsf(
        &lsx_path.to_string_lossy().to_string(),
        &lsf_path.to_string_lossy().to_string()
    )?;
    
    // Repack the save
    let output_save = format!("{}_modified.lsv", source_save_path.trim_end_matches(".lsv"));
    bg3_io::repack_save(&extract_path.to_string_lossy().to_string(), &output_save)?;
    
    Ok(format!("Save modified successfully!\nBackup: {}\nNew save: {}", backup_path, output_save))
}

#[tauri::command]
pub async fn get_backup_path(save_path: String) -> Result<String, String> {
    bg3_io::backup_save(&save_path)
}
