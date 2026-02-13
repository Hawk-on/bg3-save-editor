import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GoldEditorService } from '../../services/gold-editor.service';

@Component({
  selector: 'app-gold-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="card gold-card">
      <h3>💰 Wealth Management</h3>
      @if (isGoldLoaded()) {
        <div class="gold-display">
          <div class="total-gold">
            <span class="label">Total Gold</span>
            @if (!isEditing()) {
              <span class="value">{{ goldState()!.total_gold.toLocaleString() }}</span>
            } @else {
              <input
                [(ngModel)]="editedGoldValue"
                type="number"
                class="gold-input"
                min="0"
                max="999999"
              />
            }
          </div>

          <div class="items-list">
            <h4>Sources Found:</h4>
            <ul>
              @for (item of goldState()!.items; track $index) {
                <li>
                  {{ item.name }}: <span class="gold-amount">{{ item.amount }}</span>
                </li>
              }
            </ul>
          </div>

          <div class="button-group">
            @if (!isEditing()) {
              <button
                (click)="enableEditing()"
                class="btn-primary"
                [disabled]="isLoading()"
              >
                ✏️ Edit Gold
              </button>
            } @else {
              <button (click)="onSaveChanges()" class="btn-success" [disabled]="isLoading()">
                {{ isLoading() ? 'Saving...' : '💾 Save Changes' }}
              </button>
              <button (click)="cancelEditing()" class="btn-secondary" [disabled]="isLoading()">
                ❌ Cancel
              </button>
            }
          </div>

          @if (saveStatus()) {
            <p class="save-status">{{ saveStatus() }}</p>
          }
        </div>
      } @else if (isLoading()) {
        <div class="loading-spinner">Analyzing wealth...</div>
      } @else {
        <div class="placeholder">Load a save to view gold.</div>
      }
    </section>
  `,
  styles: [`
    h3 {
      margin-top: 0;
      margin-bottom: 20px;
    }

    .total-gold {
      text-align: center;
      padding: 20px;
      background: linear-gradient(135deg, rgba(251, 191, 36, 0.1), rgba(245, 158, 11, 0.1));
      border-radius: 8px;
      border: 1px solid rgba(251, 191, 36, 0.3);
      margin-bottom: 16px;
    }

    .total-gold .label {
      display: block;
      font-size: 0.9rem;
      text-transform: uppercase;
      letter-spacing: 1px;
      color: #b45309;
    }

    .total-gold .value {
      display: block;
      font-size: 2.5rem;
      font-weight: 800;
      color: #d97706;
      margin-top: 8px;
    }

    .gold-input {
      font-size: 2.5rem;
      font-weight: 800;
      color: #d97706;
      background: rgba(251, 191, 36, 0.15);
      border: 2px solid #d97706;
      border-radius: 6px;
      width: 100%;
      max-width: 300px;
      padding: 8px;
      text-align: center;
      margin-top: 8px;
    }

    .items-list {
      margin: 20px 0;
    }

    .items-list h4 {
      font-size: 0.9rem;
      margin: 0 0 8px 0;
      color: var(--text-secondary, #6b7280);
    }

    .items-list ul {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .items-list li {
      padding: 6px 0;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
    }

    .items-list li:last-child {
      border-bottom: none;
    }

    .gold-amount {
      color: #d97706;
      font-weight: 600;
    }

    .button-group {
      display: flex;
      gap: 12px;
      margin-top: 20px;
    }

    .button-group button {
      flex: 1;
    }

    .save-status {
      margin-top: 16px;
      padding: 12px;
      background: var(--bg-secondary, #f9fafb);
      border-radius: 6px;
      font-size: 0.9rem;
      white-space: pre-line;
    }

    .loading-spinner, .placeholder {
      text-align: center;
      padding: 40px;
      color: var(--text-secondary, #6b7280);
    }
  `]
})
export class GoldEditorComponent {
  @Output() goldSaved = new EventEmitter<string>();

  editedGoldValue = 0;

  constructor(public goldEditorService: GoldEditorService) {}

  // Expose service signals via getters
  get goldState() {
    return this.goldEditorService.goldState;
  }

  get isEditing() {
    return this.goldEditorService.isEditing;
  }

  get saveStatus() {
    return this.goldEditorService.saveStatus;
  }

  get isLoading() {
    return this.goldEditorService.isLoading;
  }

  get isGoldLoaded() {
    return this.goldEditorService.isGoldLoaded;
  }

  enableEditing() {
    this.goldEditorService.enableEditing();
    this.editedGoldValue = this.goldEditorService.editedGold();
  }

  cancelEditing() {
    this.goldEditorService.cancelEditing();
  }

  onSaveChanges() {
    this.goldEditorService.editedGold.set(this.editedGoldValue);
    this.goldEditorService.saveGoldChanges((newSavePath) => {
      this.goldSaved.emit(newSavePath);
    }).subscribe();
  }
}
