import { Injectable, signal, computed } from '@angular/core';
import { ApiService } from './api.service';

export interface SaveEntry {
  name: string;
  path: string;
  modified: string | Date;
  size?: number;
}

@Injectable({
  providedIn: 'root'
})
export class SaveListService {
  // Signals (reactive state)
  savesFolder = signal<string>("C:\\Users\\Hawkon\\AppData\\Local\\Larian Studios\\Baldur's Gate 3\\PlayerProfiles\\Public\\Savegames\\Story");
  availableSaves = signal<SaveEntry[]>([]);
  selectedSave = signal<string>("");
  extractionStatus = signal<string>("");
  isLoading = signal<boolean>(false);

  // Computed signals
  hasAvailableSaves = computed(() => this.availableSaves().length > 0);

  constructor(private apiService: ApiService) {}

  /**
   * Browse for saves folder (manual input in web version)
   */
  browseSavesFolder(): void {
    const newPath = prompt("Enter the path to your saves folder:", this.savesFolder());
    if (newPath) {
      this.savesFolder.set(newPath);
      this.loadSavesList();
    }
  }

  /**
   * Load list of available saves from the selected folder
   */
  loadSavesList(): void {
    this.apiService.post<SaveEntry[]>("/api/save/list", {
      Path: this.savesFolder()
    }).subscribe({
      next: (response) => {
        const saves = response.map((save: any) => ({
          name: save.name || save.Name,
          path: save.path || save.Path,
          modified: save.modified || save.Modified,
          size: save.size || save.Size
        }));

        this.availableSaves.set(saves);

        if (this.hasAvailableSaves()) {
          this.selectedSave.set(saves[0].path);
          this.extractionStatus.set(`Found ${saves.length} save(s)`);
        } else {
          this.extractionStatus.set("No saves found in folder");
        }
      },
      error: (error) => {
        this.extractionStatus.set(error.message);
      }
    });
  }
}
