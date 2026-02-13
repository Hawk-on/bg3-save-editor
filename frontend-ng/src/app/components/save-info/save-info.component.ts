import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SaveExtractionService } from '../../services/save-extraction.service';

@Component({
  selector: 'app-save-info',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (saveInfo()) {
      <section class="card info-card">
        <h3>Campaign Info</h3>
        <div class="info-row">
          <strong>Save Name:</strong>
          <span>{{ saveInfo()!.saveName }}</span>
        </div>
        <div class="info-row">
          <strong>Player:</strong>
          <span>{{ saveInfo()!.playerName }}</span>
        </div>
        <div class="info-row">
          <strong>Level:</strong>
          <span>{{ saveInfo()!.partyLevel }}</span>
        </div>
        <div class="info-row">
          <strong>Play Time:</strong>
          <span>{{ saveInfo()!.playTime }}</span>
        </div>
      </section>
    }
  `,
  styles: [`
    h3 {
      margin-top: 0;
      margin-bottom: 16px;
    }

    .info-row {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
    }

    .info-row:last-child {
      border-bottom: none;
    }

    .info-row strong {
      color: var(--text-secondary, #6b7280);
    }
  `]
})
export class SaveInfoComponent {
  constructor(public saveExtractionService: SaveExtractionService) {}

  get saveInfo() {
    return this.saveExtractionService.saveInfo;
  }
}
