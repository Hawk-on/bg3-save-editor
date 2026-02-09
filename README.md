# BG3 Save Editor

A cross-platform save game editor for Baldur's Gate 3, featuring a modern Vue 3 frontend and a high-performance C# backend using LSLib.

## Features
- **Modern UI**: Clean, responsive interface built with Vue 3.
- **Native Parsing**: Uses C# backend with native [LSLib](https://github.com/Norbyte/lslib) integration for reliable save handling.
- **Gold Analysis**: Inspects save files to report gold quantities (Template items supported, Inventory pending).
- **Game Data Inspection**: View raw extracted structures.

## Project Structure

```
src/                  # Vue 3 Frontend
├── api/             # API client (Axios/Fetch wrappers)
├── components/      # UI components
└── App.vue          # Root component

backend/              # C# Backend (.NET 8)
├── BG3.SaveEditor.Api/   # ASP.NET Core Web API
├── BG3.SaveEditor.Core/  # Core logic & LSLib integration
└── lib/lslib/            # Native dependencies
```

## Setup

### Prerequisites
- Node.js & npm
- .NET 8 SDK

### Running

1. **Start Backend**:
   ```powershell
   dotnet run --project backend/BG3.SaveEditor.Api/BG3.SaveEditor.Api.csproj
   ```
   *Runs on http://localhost:5062*

2. **Start Frontend**:
   ```powershell
   npm install
   npm run dev
   ```
   *Runs on http://localhost:5173*

## Documentation
- [Backend Documentation](backend/README.md) - C# architecture & NewAge technical details
- [Project Backlog](BACKLOG.md) - Roadmap & remaining tasks
- [Field Mapping](FIELD_MAPPING.md) - Save file structure reference
