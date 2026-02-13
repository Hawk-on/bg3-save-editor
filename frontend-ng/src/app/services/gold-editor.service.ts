import { Injectable, signal, computed } from '@angular/core';
import { ApiService } from './api.service';
import { SaveExtractionService } from './save-extraction.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface GoldItemDisplay {
  name: string;
  amount: number;
}

export interface GoldSaveState {
  total_gold: number;
  items: GoldItemDisplay[];
}

@Injectable({
  providedIn: 'root'
})
export class GoldEditorService {
  // Signals (reactive state)
  goldState = signal<GoldSaveState | null>(null);
  isEditing = signal<boolean>(false);
  editedGold = signal<number>(0);
  saveStatus = signal<string>("");
  isLoading = signal<boolean>(false);

  // Computed signals
  isGoldLoaded = computed(() => this.goldState() !== null);
  hasChangedGold = computed(() =>
    this.isGoldLoaded() && this.editedGold() !== this.goldState()!.total_gold
  );

  constructor(
    private apiService: ApiService,
    private saveExtractionService: SaveExtractionService
  ) {}

  /**
   * Load gold information from the extracted save
   */
  loadGoldInfo(): void {
    const extractedState = this.saveExtractionService.saveState();

    if (extractedState) {
      this.goldState.set({
        total_gold: extractedState.totalGold,
        items: extractedState.goldItems.map(item => ({
          name: item.templateName,
          amount: item.amount
        }))
      });
      this.editedGold.set(extractedState.totalGold);
    }
  }

  /**
   * Enable editing mode for gold amount
   */
  enableEditing(): void {
    this.isEditing.set(true);
    this.editedGold.set(this.goldState()?.total_gold || 0);
  }

  /**
   * Cancel editing and discard changes
   */
  cancelEditing(): void {
    this.isEditing.set(false);
    this.saveStatus.set("");
  }

  /**
   * Save modified gold amount back to save file
   */
  saveGoldChanges(onSaveSuccess?: (newSavePath: string) => void): Observable<void> {
    if (!this.goldState()) {
      throw new Error("No gold state loaded");
    }

    const currentPath = this.saveExtractionService.currentSavePath();
    if (!currentPath) {
      this.saveStatus.set("❌ No save file loaded");
      throw new Error("No save file loaded");
    }

    this.isLoading.set(true);
    this.saveStatus.set("Saving changes...");

    return this.apiService.post<{ success: boolean; outputPath: string }>("/api/save/gold", {
      Path: currentPath,
      Amount: this.editedGold()
    }).pipe(
      map(response => {
        if (response.success && response.outputPath) {
          const modifiedSavePath = response.outputPath;
          this.saveStatus.set("✅ Changes saved! Reloading modified save...");

          if (onSaveSuccess) {
            onSaveSuccess(modifiedSavePath);
          }

          this.saveStatus.set(`✅ Gold modified successfully!\nNew save: ${modifiedSavePath}`);
        } else {
          this.saveStatus.set("✅ Changes saved!");
        }

        this.isEditing.set(false);
        this.isLoading.set(false);
      })
    );
  }

  /**
   * Reset gold editor state
   */
  reset(): void {
    this.goldState.set(null);
    this.isEditing.set(false);
    this.editedGold.set(0);
    this.saveStatus.set("");
  }
}
