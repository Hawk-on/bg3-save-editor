import { Component, ViewChild } from '@angular/core';
import { LslibStatusComponent } from './components/lslib-status/lslib-status.component';
import { SavesFolderComponent } from './components/saves-folder/saves-folder.component';
import { SaveInfoComponent } from './components/save-info/save-info.component';
import { GoldEditorComponent } from './components/gold-editor/gold-editor.component';
import { SaveExtractionService } from './services/save-extraction.service';
import { GoldEditorService } from './services/gold-editor.service';

@Component({
  selector: 'app-root',
  imports: [
    LslibStatusComponent,
    SavesFolderComponent,
    SaveInfoComponent,
    GoldEditorComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  @ViewChild(SavesFolderComponent) savesFolderComponent!: SavesFolderComponent;

  constructor(
    private saveExtractionService: SaveExtractionService,
    private goldEditorService: GoldEditorService,
  ) {}

  onExtractSave(savePath: string) {
    this.goldEditorService.reset();
    this.saveExtractionService.extractSave(savePath).subscribe({
      next: (msg) => {
        this.savesFolderComponent.setLoading(false);
        this.savesFolderComponent.setStatus(msg);
        this.goldEditorService.loadGoldInfo();
      },
      error: (err) => {
        this.savesFolderComponent.setLoading(false);
        this.savesFolderComponent.setStatus(err.message);
      }
    });
  }

  onGoldSaved(newSavePath: string) {
    // Reload the modified save
    this.saveExtractionService.extractSave(newSavePath).subscribe({
      next: () => this.goldEditorService.loadGoldInfo()
    });
  }
}
