use std::process::Command;

// Helper to get Divine.exe path. 
// For dev env, it is relative to the workspace.
// For prod, it should be bundled (TODO for Polish phase).
fn get_divine_path() -> String {
    // Hardcoded relative path for Dev environment
    // Assuming running from src-tauri or project root
    // Typically `cargo run` runs from `src-tauri`, so `../tools/...`
    let path = std::path::Path::new("../tools/lslib/Packed/Tools/Divine.exe");
    if path.exists() {
        return path.to_string_lossy().to_string();
    }
    
    // If running from root?
    let path_root = std::path::Path::new("tools/lslib/Packed/Tools/Divine.exe");
    if path_root.exists() {
        return path_root.to_string_lossy().to_string();
    }

    // Fallback/Error
    "Divine.exe".to_string()
}

pub fn verify_divine() -> Result<String, String> {
    let divine_exe = get_divine_path();
    
    // Check if file exists
    let path = std::path::Path::new(&divine_exe);
    if !path.exists() {
        return Err(format!("Divine.exe not found at: {}", divine_exe));
    }
    
    // Try to run it with --version or --help to verify it works
    let output = Command::new(&divine_exe)
        .arg("--version")
        .output();
    
    match output {
        Ok(out) => {
            if out.status.success() {
                let version = String::from_utf8_lossy(&out.stdout);
                Ok(format!("Divine.exe verified at: {}\n{}", divine_exe, version))
            } else {
                // Some versions might not support --version, so just confirm it exists
                Ok(format!("Divine.exe found at: {}", divine_exe))
            }
        }
        Err(e) => Err(format!("Failed to execute Divine.exe: {}", e))
    }
}

pub fn extract_save(pkg_path: &str, output_path: &str) -> Result<(), String> {
    let divine_exe = get_divine_path();
    
    println!("Extracting {} to {} using {}", pkg_path, output_path, divine_exe);

    let output = Command::new(&divine_exe)
        .arg("-g")
        .arg("bg3")
        .arg("-a")
        .arg("extract-package")
        .arg("-s")
        .arg(pkg_path)
        .arg("-d")
        .arg(output_path)
        .output()
        .map_err(|e| format!("Failed to execute Divine: {}", e))?;

    if !output.status.success() {
        return Err(format!("Divine failed: {}", String::from_utf8_lossy(&output.stderr)));
    }

    Ok(())
}

pub fn convert_lsf_to_lsx(lsf_path: &str, lsx_path: &str) -> Result<(), String> {
    let divine_exe = get_divine_path();

    let output = Command::new(&divine_exe)
        .arg("-g")
        .arg("bg3")
        .arg("-a")
        .arg("convert-resource")
        .arg("-s")
        .arg(lsf_path)
        .arg("-d")
        .arg(lsx_path)
        .arg("-i")
        .arg("lsf")
        .arg("-o")
        .arg("lsx")
        .output()
        .map_err(|e| format!("Failed to execute Divine conversion: {}", e))?;

    if !output.status.success() {
        return Err(format!("Divine conversion failed: {}", String::from_utf8_lossy(&output.stderr)));
    }

    Ok(())
}

pub fn convert_lsx_to_lsf(lsx_path: &str, lsf_path: &str) -> Result<(), String> {
    let divine_exe = get_divine_path();

    let output = Command::new(&divine_exe)
        .arg("-g")
        .arg("bg3")
        .arg("-a")
        .arg("convert-resource")
        .arg("-s")
        .arg(lsx_path)
        .arg("-d")
        .arg(lsf_path)
        .arg("-i")
        .arg("lsx")
        .arg("-o")
        .arg("lsf")
        .output()
        .map_err(|e| format!("Failed to execute Divine conversion to LSF: {}", e))?;

    if !output.status.success() {
        return Err(format!("Divine conversion failed: {}", String::from_utf8_lossy(&output.stderr)));
    }

    Ok(())
}

pub fn repack_save(source_path: &str, output_lsv_path: &str) -> Result<(), String> {
    let divine_exe = get_divine_path();

    let output = Command::new(&divine_exe)
        .arg("-g")
        .arg("bg3")
        .arg("-a")
        .arg("create-package")
        .arg("-s")
        .arg(source_path)
        .arg("-d")
        .arg(output_lsv_path)
        // Compression might be needed, Divine defaults often work
        .output()
        .map_err(|e| format!("Failed to execute Divine repack: {}", e))?;

    if !output.status.success() {
        return Err(format!("Divine repack failed: {}", String::from_utf8_lossy(&output.stderr)));
    }

    Ok(())
}
