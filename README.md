# BG3 Save Editor

A modern, cross-platform save game editor for Baldur's Gate 3 featuring an Angular 20 frontend and a high-performance C# .NET backend using LSLib.

## Features
- **Modern UI**: Clean, responsive interface built with Angular 20 LTS and TypeScript
- **Native Parsing**: Uses C# backend with native [LSLib](https://github.com/Norbyte/lslib) integration for reliable save handling
- **Gold Editing**: View and modify gold amounts in your saves
- **Automatic Backups**: Creates timestamped backups before any modifications
- **RESTful API**: Backend exposes HTTP API endpoints for easy integration

## Architecture

```
Frontend (Angular 20 LTS + TypeScript)
├── HttpClient - Angular HTTP client
├── Services - Business logic with signals
└── Standalone Components - Modern Angular architecture

Backend (ASP.NET Core Web API .NET 8)
├── SaveService - Core save manipulation logic
├── LSLib Integration - Native BG3 file format support
└── Divine.exe - Save extraction and repacking
```

## Project Structure

```
frontend-ng/          # Angular 20 Frontend
├── src/app/
│   ├── components/  # Standalone UI components
│   ├── services/    # Business logic & API integration
│   └── app.ts       # Root component
│
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
cd frontend-ng
npm install
npm start
```

The frontend will start on **http://localhost:4200**

### 3. Open in Browser

Navigate to http://localhost:4200 and start editing your saves!

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

Edit `frontend-ng/src/environments/environment.ts` to customize the API URL:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5062'
};
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
# Navigate to Angular app
cd frontend-ng

# Install dependencies
npm install

# Dev server with hot reload
npm start

# Build for production
npm run build

# Run tests
npm test
```

## How It Works

1. **Load Save**: Backend uses LSLib's PackageReader to load .lsv files
2. **Parse Data**: Extracts Globals.lsf and parses the LSF binary format
3. **Modify**: Updates gold item amounts in the resource tree
4. **Repack**: Uses Divine.exe to repack the modified save
5. **Backup**: Automatic timestamped backups before any changes

## Migration History

This project has evolved through multiple architectures:

1. **v1**: Tauri (Rust backend + Vue 3 frontend in desktop app)
2. **v2**: Web app (C# .NET backend + Vue 3 frontend)
3. **v3 (Current)**: Web app (C# .NET backend + Angular 20 LTS frontend)

**Benefits of current architecture**:
- Angular 20 LTS for long-term support and familiar Angular ecosystem
- C# .NET backend for better Windows compatibility
- Easier to maintain with strong typing throughout
- More flexible deployment options

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
- Check the browser console for CORS errors
- Ensure `environment.ts` has correct `apiUrl`

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
