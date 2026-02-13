# BG3 Save Editor

A modern, cross-platform save game editor for Baldur's Gate 3 featuring a Vue 3 frontend and a high-performance C# .NET backend using LSLib.

## Features
- **Modern UI**: Clean, responsive interface built with Vue 3 and TypeScript
- **Native Parsing**: Uses C# backend with native [LSLib](https://github.com/Norbyte/lslib) integration for reliable save handling
- **Gold Editing**: View and modify gold amounts in your saves
- **Automatic Backups**: Creates timestamped backups before any modifications
- **RESTful API**: Backend exposes HTTP API endpoints for easy integration

## Architecture

```
Frontend (Vue 3 + TypeScript)
├── HTTP API Client (fetch)
└── Composables for state management

Backend (ASP.NET Core Web API .NET 8)
├── SaveService - Core save manipulation logic
├── LSLib Integration - Native BG3 file format support
└── Divine.exe - Save extraction and repacking
```

## Project Structure

```
src/                  # Vue 3 Frontend
├── components/       # UI components
├── composables/      # Business logic & API integration
└── App.vue           # Root component

backend/              # C# Backend (.NET 8)
├── BG3.SaveEditor.Api/   # ASP.NET Core Web API
├── BG3.SaveEditor.Core/  # Core logic & LSLib integration
└── lib/lslib/            # Native LSLib dependencies & Divine.exe
```

## Prerequisites

- **Backend**: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Frontend**: [Node.js](https://nodejs.org/) (v18 or higher)

## Setup & Running

### 1. Start the Backend

Open a terminal in the project root:

```powershell
cd backend/BG3.SaveEditor.Api
dotnet run
```

The API will start on **http://localhost:5062**

### 2. Start the Frontend

Open another terminal in the project root:

```powershell
npm install
npm run dev
```

The frontend will start on **http://localhost:5173**

### 3. Open in Browser

Navigate to http://localhost:5173 and start editing your saves!

## API Endpoints

The backend exposes the following REST endpoints:

- `POST /api/save/load` - Load and parse a save file
  - Request: `{ "Path": "C:/path/to/save.lsv" }`
  - Returns: Save info and gold data

- `POST /api/save/gold` - Modify gold amount
  - Request: `{ "Path": "...", "Amount": 9999, "OutputPath": "..." }`
  - Returns: `{ "success": true, "outputPath": "..." }`

- `POST /api/save/list` - List saves in a directory
  - Request: `{ "Path": "C:/path/to/saves" }`
  - Returns: Array of save file info

- `POST /api/save/backup` - Create a backup
  - Request: `{ "Path": "C:/path/to/save.lsv" }`
  - Returns: `{ "success": true, "backupPath": "..." }`

## Configuration

### Frontend Configuration

Create a `.env` file in the project root to customize the API URL:

```env
VITE_API_URL=http://localhost:5062
```

### Default Save Location

The editor defaults to the standard BG3 saves location:
```
C:\Users\<YourName>\AppData\Local\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story
```

## Development

### Backend Development

```powershell
# Build
dotnet build backend/BG3.SaveEditor.Api/BG3.SaveEditor.Api.csproj

# Run with hot reload
dotnet watch --project backend/BG3.SaveEditor.Api
```

### Frontend Development

```powershell
# Install dependencies
npm install

# Dev server with hot reload
npm run dev

# Type checking
npm run type-check

# Build for production
npm run build
```

## How It Works

1. **Load Save**: Backend uses LSLib's PackageReader to load .lsv files
2. **Parse Data**: Extracts Globals.lsf and parses the LSF binary format
3. **Modify**: Updates gold item amounts in the resource tree
4. **Repack**: Uses Divine.exe to repack the modified save
5. **Backup**: Automatic timestamped backups before any changes

## Migration Notes

This project was migrated from a Rust/Tauri desktop application to a web-based architecture with C# backend:

- **Previous**: Tauri (Rust backend + Vue frontend in Electron-style app)
- **Current**: Standalone web app with .NET API backend
- **Benefit**: Better compatibility with .NET ecosystem, easier to maintain, more flexible deployment

## Documentation

- [Project Backlog](BACKLOG.md) - Roadmap & remaining tasks
- [Field Mapping](FIELD_MAPPING.md) - Save file structure reference
- [Gold Storage Fix](GOLD_STORAGE_FIX.md) - Technical details on gold handling

## Troubleshooting

### Backend won't start
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Check that Divine.exe exists in `backend/lib/lslib/Packed/Tools/`

### Frontend can't connect to backend
- Verify backend is running on port 5062
- Check the console for CORS errors
- Ensure `.env` file has correct `VITE_API_URL`

### Gold modification fails
- Ensure you have write permissions to the save directory
- Check that the save file isn't open in the game
- Look for error details in the backend console

## License

MIT License - See [LICENSE](LICENSE) file for details

## Credits

- Built with [LSLib](https://github.com/Norbyte/lslib) by Norbyte
- Uses Divine.exe for BG3 save file manipulation
- Frontend powered by Vue 3 and Vite
- Backend built with ASP.NET Core
