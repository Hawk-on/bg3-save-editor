// Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
#[tauri::command]
fn greet(name: &str) -> String {
    format!("Hello, {}! You've been greeted from Rust!", name)
}

mod commands;
mod bg3_io;
mod save_model;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .invoke_handler(tauri::generate_handler![
            greet,
            commands::check_lslib_status,
            commands::list_saves,
            commands::extract_save,
            commands::read_save_info,
            commands::get_gold_count,
            commands::modify_and_save_gold,
            commands::get_backup_path
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
