import { Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface SaveInfo {
  filePath: string;
  saveName: string;
  playerName: string;
  partyLevel: number;
  playTime: string;
  saveDate: string;
  gold: number;
}

export interface SaveState {
  info: SaveInfo;
  goldItems: Array<{
    handle: string;
    templateName: string;
    amount: number;
    location: string;
  }>;
  totalGold: number;
}

@Injectable({
  providedIn: 'root'
})
export class SaveExtractionService {
  // Signals (reactive state)
  saveInfo = signal<SaveInfo | null>(null);
  saveState = signal<SaveState | null>(null);
  currentSavePath = signal<string>("");

  constructor(private apiService: ApiService) {}

  /**
   * Extract and load save file (combined operation in C# backend)
   */
  extractSave(selectedSavePath: string): Observable<string> {
    this.currentSavePath.set(selectedSavePath);

    return this.apiService.post<any>("/api/save/load", {
      Path: selectedSavePath
    }).pipe(
      map(response => {
        // Handle both camelCase (JS) and PascalCase (C#) property names
        const info = response.info || response.Info || {};
        const state: SaveState = {
          info: {
            filePath: info.filePath || info.FilePath || "",
            saveName: info.saveName || info.SaveName || "",
            playerName: info.playerName || info.PlayerName || "",
            partyLevel: info.partyLevel || info.PartyLevel || 0,
            playTime: info.playTime || info.PlayTime || "0",
            saveDate: info.saveDate || info.SaveDate || "",
            gold: response.totalGold || response.TotalGold || 0
          },
          goldItems: response.goldItems || response.GoldItems || [],
          totalGold: response.totalGold || response.TotalGold || 0
        };

        this.saveState.set(state);
        this.saveInfo.set(state.info);

        return `Save loaded successfully`;
      })
    );
  }

  /**
   * Read save metadata (already loaded with extractSave)
   */
  readSaveInfo(): void {
    // Data is already loaded in extractSave
    if (!this.saveInfo() && this.currentSavePath()) {
      this.extractSave(this.currentSavePath()).subscribe();
    }
  }

  /**
   * Reset extraction state
   */
  reset(): void {
    this.saveInfo.set(null);
    this.saveState.set(null);
  }
}
