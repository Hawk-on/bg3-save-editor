use std::process::Command;
use std::env;
use std::path::{Path, PathBuf};

// ============================================================================
// Path Resolution
// ============================================================================

/// Get the path to Divine.exe from tools/lslib
pub fn get_divine_path() -> Result<String, String> {
    let current_dir = env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))?;
    
    println!("Current directory: {:?}", current_dir);
    
    let divine_path = find_divine_path(&current_dir)
        .ok_or_else(|| "Divine.exe not found. Please ensure LSLib is installed in tools/lslib/Packed/Tools/".to_string())?;
    
    println!("Found Divine.exe at: {:?}", divine_path);
    Ok(divine_path.to_string_lossy().to_string())
}

/// Search for Divine.exe in common relative locations
fn find_divine_path(base_dir: &Path) -> Option<PathBuf> {
    let possible_paths = vec![
        base_dir.join("../tools/lslib/Packed/Tools/Divine.exe"),
        base_dir.join("tools/lslib/Packed/Tools/Divine.exe"),
        base_dir.join("../../tools/lslib/Packed/Tools/Divine.exe"),
    ];
    
    for path in possible_paths {
        if path.exists() {
            return path.canonicalize().ok();
        }
    }
    None
}

// ============================================================================
// Divine.exe Command Execution
// ============================================================================

/// Execute a Divine.exe command with given arguments
fn execute_divine_command(args: Vec<&str>) -> Result<(), String> {
    let divine_exe = get_divine_path()?;
    
    let output = Command::new(&divine_exe)
        .args(args)
        .output()
        .map_err(|e| format!("Failed to execute Divine.exe: {}. Ensure LSLib is installed correctly.", e))?;
    
    log_divine_output(&output);
    
    if !output.status.success() {
        let error_msg = extract_error_message(&output);
        return Err(format!("Divine operation failed: {}", error_msg));
    }
    
    Ok(())
}

/// Log Divine.exe standard output and error
fn log_divine_output(output: &std::process::Output) {
    let stdout = String::from_utf8_lossy(&output.stdout);
    let stderr = String::from_utf8_lossy(&output.stderr);
    
    if !stdout.is_empty() {
        println!("Divine stdout: {}", stdout);
    }
    if !stderr.is_empty() {
        println!("Divine stderr: {}", stderr);
    }
}

/// Extract meaningful error message from Divine output
fn extract_error_message(output: &std::process::Output) -> String {
    let stderr = String::from_utf8_lossy(&output.stderr);
    let stdout = String::from_utf8_lossy(&output.stdout);
    
    if !stderr.is_empty() {
        stderr.to_string()
    } else if !stdout.is_empty() {
        stdout.to_string()
    } else {
        format!("Exit code: {:?}", output.status.code())
    }
}

// ============================================================================
// Public API Functions
// ============================================================================

pub fn extract_save(pkg_path: &str, output_path: &str) -> Result<(), String> {
    validate_input_file(pkg_path)?;
    
    println!("Extracting {} to {}", pkg_path, output_path);
    
    execute_divine_command(vec![
        "-g", "bg3",
        "-a", "extract-package",
        "-s", pkg_path,
        "-d", output_path,
    ])
}

pub fn convert_lsf_to_lsx(lsf_path: &str, lsx_path: &str) -> Result<(), String> {
    println!("Converting {} to {}", lsf_path, lsx_path);

    execute_divine_command(vec![
        "-g", "bg3",
        "-a", "convert-resource",
        "-s", lsf_path,
        "-d", lsx_path,
        "-i", "lsf",
        "-o", "lsx",
    ])
}

#[allow(dead_code)]
pub fn convert_lsx_to_lsf(lsx_path: &str, lsf_path: &str) -> Result<(), String> {
    println!("Converting {} to {}", lsx_path, lsf_path);

    execute_divine_command(vec![
        "-g", "bg3",
        "-a", "convert-resource",
        "-s", lsx_path,
        "-d", lsf_path,
        "-i", "lsx",
        "-o", "lsf",
    ])
}

#[allow(dead_code)]
pub fn repack_save(source_path: &str, output_lsv_path: &str) -> Result<(), String> {
    println!("Repacking {} to {}", source_path, output_lsv_path);

    execute_divine_command(vec![
        "-g", "bg3",
        "-a", "create-package",
        "-s", source_path,
        "-d", output_lsv_path,
    ])
}

// ============================================================================
// File Operations
// ============================================================================

/// Validate that input file exists
fn validate_input_file(file_path: &str) -> Result<(), String> {
    if !Path::new(file_path).exists() {
        return Err(format!("File not found: {}", file_path));
    }
    Ok(())
}

/// Creates a backup of a save file before modification
pub fn backup_save(save_path: &str) -> Result<String, String> {
    validate_input_file(save_path)?;
    
    let path = Path::new(save_path);
    let timestamp = chrono::Local::now().format("%Y%m%d_%H%M%S");
    
    let backup_name = format!(
        "{}_backup_{}.lsv",
        path.file_stem().unwrap().to_string_lossy(),
        timestamp
    );
    
    let backup_path = path.parent()
        .unwrap_or(path)
        .join(backup_name);
    
    std::fs::copy(save_path, &backup_path)
        .map_err(|e| format!("Failed to create backup: {}", e))?;
    
    println!("Backup created at: {:?}", backup_path);
    Ok(backup_path.to_string_lossy().to_string())
}
