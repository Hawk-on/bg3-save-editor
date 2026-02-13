import { Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LslibService {
  // Signal (reactive state)
  lslibStatus = signal<string>("");

  /**
   * Check if the backend API is available
   */
  async checkLslib(): Promise<void> {
    try {
      await fetch(`${environment.apiUrl}/api/save/list`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Path: '' })
      });

      // Any response (even error) means the server is running
      this.lslibStatus.set("✅ Backend API is running");
    } catch (e) {
      this.lslibStatus.set("❌ Backend API not available. Please start the backend server.");
      console.error("Failed to connect to backend:", e);
    }
  }
}
