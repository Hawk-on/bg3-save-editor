use std::process::Command;
use std::path::{Path, PathBuf};
use tauri::State;

// Define a struct to hold our app state or config if needed
// pub struct AppState { ... }

#[tauri::command]
pub fn check_lslib_status() -> Result<String, String> {
    let tool_path = "tools/lslib/Packed/Tools/Divine.exe";
    if Path::new(tool_path).exists() {
        Ok("LSLib tools found.".to_string())
    } else {
        Err("LSLib tools not found at 'tools/lslib/Packed/Tools/Divine.exe'".to_string())
    }
}

#[tauri::command]
pub async fn extract_save(save_path: String) -> Result<String, String> {
    let tool_path = "tools/lslib/Packed/Tools/Divine.exe";
    let extract_path = "extracted/temp_save"; // Temporary extraction path
    
    // Ensure extraction dir exists or is clean
    if Path::new(extract_path).exists() {
        std::fs::remove_dir_all(extract_path).map_err(|e| e.to_string())?;
    }
    std::fs::create_dir_all(extract_path).map_err(|e| e.to_string())?;

    let output = Command::new(tool_path)
        .args(&["-g", "bg3", "-a", "extract-package", "-s", &save_path, "-d", extract_path])
        .output()
        .map_err(|e| format!("Failed to execute process: {}", e))?;

    if output.status.success() {
        Ok(format!("Save extracted to {}", extract_path))
    } else {
        let stderr = String::from_utf8_lossy(&output.stderr);
        Err(format!("Extraction failed: {}", stderr))
    }
}

#[tauri::command]
pub async fn read_save_info() -> Result<serde_json::Value, String> {
    let info_path = "extracted/temp_save/SaveInfo.json";
    if !Path::new(info_path).exists() {
        return Err("SaveInfo.json not found. Extract a save first.".to_string());
    }

    let content = std::fs::read_to_string(info_path).map_err(|e| e.to_string())?;
    let json: serde_json::Value = serde_json::from_str(&content).map_err(|e| e.to_string())?;
    
    Ok(json)
}
