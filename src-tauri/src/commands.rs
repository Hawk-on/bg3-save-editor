use std::path::{Path, PathBuf};
use crate::bg3_io;
use crate::save_model;

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
pub async fn extract_save(save_path: String) -> Result<String, String> {
    let extract_path = "extracted/temp_save"; 
    
    // Clean up
    if Path::new(extract_path).exists() {
        std::fs::remove_dir_all(extract_path).map_err(|e| e.to_string())?;
    }
    std::fs::create_dir_all(extract_path).map_err(|e| e.to_string())?;

    // Extract
    bg3_io::extract_save(&save_path, extract_path)?;

    // Convert Globals.lsf -> lsx
    let globals_lsf = format!("{}/Globals.lsf", extract_path);
    let globals_lsx = format!("{}/Globals.lsx", extract_path);
    if Path::new(&globals_lsf).exists() {
        bg3_io::convert_lsf_to_lsx(&globals_lsf, &globals_lsx)?;
    }

    // Convert WLD_Main_A.lsf -> lsx (Recursively convert all in LevelCache? Or just Main)
    // Finding the level name dynamically would be better, but often it's LevelCache/WLD_Main_A.lsf
    // SaveInfo.json has "Current Level" key.
    
    /* 
       For MVP, let's look for known paths. 
    */
    let level_lsf = format!("{}/LevelCache/WLD_Main_A.lsf", extract_path);
    let level_lsx = format!("{}/LevelCache/WLD_Main_A.lsx", extract_path);
    if Path::new(&level_lsf).exists() {
        bg3_io::convert_lsf_to_lsx(&level_lsf, &level_lsx)?;
    }

    Ok(format!("Save extracted and converted to {}", extract_path))
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

#[tauri::command]
pub async fn get_gold_count() -> Result<save_model::SaveState, String> {
    let lsx_path = "extracted/temp_save/LevelCache/WLD_Main_A.lsx";
    if !Path::new(lsx_path).exists() {
        return Err("Level data not found (WLD_Main_A.lsx).".to_string());
    }

    // Read content
    // Warn: File can be large (100MB+). This might block async thread if we don't spawn_blocking.
    let content = std::fs::read_to_string(lsx_path).map_err(|e| e.to_string())?;
    
    // Parse
    let gold_info = save_model::get_gold_info(&content);
    Ok(gold_info)
}

#[tauri::command]
pub fn verify_divine_integration() -> Result<String, String> {
    bg3_io::verify_divine()
}

#[tauri::command]
pub async fn update_gold(new_gold: i32) -> Result<String, String> {
    let lsx_path = "extracted/temp_save/LevelCache/WLD_Main_A.lsx";
    if !Path::new(lsx_path).exists() {
        return Err("Level data not found (WLD_Main_A.lsx). Extract a save first.".to_string());
    }

    // Read current content
    let content = std::fs::read_to_string(lsx_path).map_err(|e| e.to_string())?;
    
    // Update gold
    let updated_content = save_model::update_gold_in_lsx(&content, new_gold)?;
    
    // Write back to file
    std::fs::write(lsx_path, updated_content).map_err(|e| e.to_string())?;
    
    Ok(format!("Gold updated to {}", new_gold))
}

#[tauri::command]
pub async fn repack_save(output_path: String) -> Result<String, String> {
    let extract_path = "extracted/temp_save";
    
    if !Path::new(extract_path).exists() {
        return Err("No extracted save found. Extract a save first.".to_string());
    }
    
    // Convert LSX back to LSF
    let level_lsx = format!("{}/LevelCache/WLD_Main_A.lsx", extract_path);
    let level_lsf = format!("{}/LevelCache/WLD_Main_A.lsf", extract_path);
    
    if Path::new(&level_lsx).exists() {
        bg3_io::convert_lsx_to_lsf(&level_lsx, &level_lsf)?;
    }
    
    // Also convert Globals if it was converted
    let globals_lsx = format!("{}/Globals.lsx", extract_path);
    let globals_lsf = format!("{}/Globals.lsf", extract_path);
    
    if Path::new(&globals_lsx).exists() {
        bg3_io::convert_lsx_to_lsf(&globals_lsx, &globals_lsf)?;
    }
    
    // Repack into new .lsv file
    bg3_io::repack_save(extract_path, &output_path)?;
    
    Ok(format!("Save repacked successfully to {}", output_path))
}
