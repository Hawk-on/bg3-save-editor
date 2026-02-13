import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LslibService } from '../../services/lslib.service';

@Component({
  selector: 'app-lslib-status',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card status-card">
      <div class="status-header">
        <span>System Status:</span>
        <span [class.status-ok]="status().includes('✅')"
              [class.status-err]="status().includes('❌')">
          {{ status() || 'Unchecked' }}
        </span>
      </div>
      @if (!status()) {
        <button class="btn-secondary" (click)="checkLslib()">
          Check Tools
        </button>
      }
    </section>
  `,
  styles: [`
    .status-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .status-header span:last-child {
      font-weight: 600;
    }

    h2 {
      margin-bottom: 16px;
      font-size: 1.1rem;
    }

    .status-ok {
      color: var(--success-color, #22c55e);
    }

    .status-err {
      color: var(--error-color, #ef4444);
    }
  `]
})
export class LslibStatusComponent implements OnInit {
  constructor(public lslibService: LslibService) {}

  get status() {
    return this.lslibService.lslibStatus;
  }

  ngOnInit() {
    this.checkLslib();
  }

  checkLslib() {
    this.lslibService.checkLslib();
  }
}
