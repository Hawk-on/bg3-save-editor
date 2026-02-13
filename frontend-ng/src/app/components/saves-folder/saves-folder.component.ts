import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SaveListService } from '../../services/save-list.service';

@Component({
  selector: 'app-saves-folder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="card load-card">
      <h2>📂 Select Saves Folder</h2>
      <div class="input-group">
        <input [(ngModel)]="savesFolder" placeholder="Path to Story folder" />
        <button class="btn-secondary" (click)="browseSavesFolder()" [disabled]="isLoading()">
          📁 Browse
        </button>
        <button class="btn-primary" (click)="loadSavesList()" [disabled]="isLoading()">
          🔄 Load Saves
        </button>
      </div>

      @if (hasAvailableSaves()) {
        <div class="saves-list">
          <h3>Available Saves:</h3>
          <select [(ngModel)]="selectedSave" class="save-select">
            @for (save of availableSaves(); track save.path) {
              <option [value]="save.path">
                {{ save.name }}
              </option>
            }
          </select>
          <button class="btn-primary" (click)="onExtractSave()" [disabled]="isLoading() || !selectedSave">
            {{ isLoading() ? 'Processing...' : 'Load & Extract' }}
          </button>
        </div>
      }

      <p class="status-text">{{ extractionStatus() }}</p>
    </section>
  `,
  styles: [`
    h2 {
      margin-top: 0;
      margin-bottom: 16px;
    }

    .input-group {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }

    .input-group input {
      flex: 1;
    }

    .saves-list {
      margin-top: 20px;
      padding-top: 20px;
      border-top: 1px solid var(--border-color, #e5e7eb);
    }

    .saves-list h3 {
      font-size: 1rem;
      margin: 0 0 12px 0;
      color: var(--text-secondary, #6b7280);
    }

    .save-select {
      width: 100%;
      margin-bottom: 12px;
      padding: 8px;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 6px;
    }

    .status-text {
      margin-top: 12px;
      font-size: 0.9rem;
      color: var(--text-secondary, #6b7280);
    }
  `]
})
export class SavesFolderComponent {
  @Output() extract = new EventEmitter<string>();

  savesFolder = '';
  selectedSave = '';

  constructor(public saveListService: SaveListService) {
    this.savesFolder = this.saveListService.savesFolder();
  }

  // Expose service signals via getters
  get availableSaves() {
    return this.saveListService.availableSaves;
  }

  get extractionStatus() {
    return this.saveListService.extractionStatus;
  }

  get isLoading() {
    return this.saveListService.isLoading;
  }

  get hasAvailableSaves() {
    return this.saveListService.hasAvailableSaves;
  }

  browseSavesFolder() {
    this.saveListService.browseSavesFolder();
    this.savesFolder = this.saveListService.savesFolder();
  }

  loadSavesList() {
    this.saveListService.loadSavesList();
  }

  onExtractSave() {
    if (this.selectedSave) {
      this.saveListService.isLoading.set(true);
      this.extract.emit(this.selectedSave);
    }
  }

  setLoading(loading: boolean) {
    this.saveListService.isLoading.set(loading);
  }

  setStatus(status: string) {
    this.saveListService.extractionStatus.set(status);
  }
}
