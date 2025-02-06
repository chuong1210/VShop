import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import Alpine from "alpinejs";
declare global {
  interface Window {
    Alpine: any;
  }
}
bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));

  window.Alpine  = Alpine;
  Alpine.start();
