# C# Backend for BG3 Save Editor

This backend replaces the previous Rust/Tauri backend with a native C# solution using [LSLib](https://github.com/Norbyte/lslib).

## Architecture

The solution `BG3SaveEditor.sln` includes:

### 1. `BG3.SaveEditor.Api`
- **Type**: ASP.NET Core Web API (.NET 8)
- **Port**: 5062 (http)
- **Role**: Serves REST endpoints for the frontend.
- **Endpoints**:
  - `POST /api/save/load`: Loads a `.lsv` save package and returns parsed metadata.
  - `POST /api/save/gold`: (Placeholder) for modifying gold.

### 2. `BG3.SaveEditor.Core`
- **Type**: Class Library (.NET 8)
- **Role**: Handles business logic and LSLib integration.
- **Dependencies**: References `LSLib.dll` and its native dependencies (`granny2.dll`, `LSLibNative.dll`) manually from `backend/lib/lslib`.

## Setup & Running

1. **Build**:
   ```powershell
   dotnet build backend/BG3SaveEditor.slnx
   ```
   *Note: Native DLLs are automatically copied to the output directory via `.csproj` rules.*

2. **Run API**:
   ```powershell
   dotnet run --project backend/BG3.SaveEditor.Api/BG3.SaveEditor.Api.csproj
   ```

## Development Status

### ✅ Implemented
- **Package Extraction**: Successfully extracts `SaveInfo.json` and `Globals.lsf` from `.lsv` packages.
- **LSLib Integration**: Native DLL loading is configured and working.
- **Template Parsing**: Can parse "Template" gold items (usually loot in chests).

### 🚧 Limitations
- **Character Inventory**: The main character inventory (including gold) is stored in a **Binary Blob** (ECS data) within the `NewAge` region of the save file.
- **Next Steps**: Implement parsing logic for the `ScratchBuffer` in `NewAge` region to read/write character inventory.
