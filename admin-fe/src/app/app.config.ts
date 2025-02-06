import { ApplicationConfig, importProvidersFrom } from '@angular/core';


import { provideRouter } from '@angular/router';
import { provideClientHydration } from '@angular/platform-browser';
import { CommonModule, DatePipe } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideFirebaseApp, initializeApp } from '@angular/fire/app';
import { provideStorage, getStorage } from '@angular/fire/storage';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';
import { provideQueryClient, QueryClient } from '@tanstack/angular-query-experimental';
import { DATA_AUTH_IOC } from 'data/data.ioc';
import { AuthInterceptor } from 'domain/services/auth.interceptor';
import { AuthService } from 'domain/services/auth/auth.service';
import { NgProgressModule } from 'ngx-progressbar';
import { ToastrModule } from 'ngx-toastr';
import { RemoveCommaPipeModule } from 'remove-comma.pipe/remove-comma.pipe.module';
import { environment_firebase } from '../environments/environment.development';
import { ModalService } from './_components/modal/services/modal.service';
import { NotificationModalService } from './admin/_components/ui/notification/services/notification.modal.service';
import { BreadcrumbService } from './admin/_services/breadcrumbs/breadcrumbs.service';
import { NgProgressRouterModule } from 'ngx-progressbar/router';
import { AppComponent } from './app.component';
import { routes } from './app.routes';

import { CKEditorModule } from '@ckeditor/ckeditor5-angular';
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes), // ⚠️ Cập nhật `routes` nếu cần
    provideClientHydration(),
    provideQueryClient(new QueryClient()),
provideHttpClient(withInterceptorsFromDi()),
    // ✅ Import các module đúng cách
    importProvidersFrom(
      CommonModule,
      HttpClientModule,
      RouterModule.forRoot(routes), // ⚠️ Cập nhật `routes`
      BrowserAnimationsModule,

      ToastrModule.forRoot({
        timeOut: 2000,
        progressBar: true,
        progressAnimation: 'increasing',
        newestOnTop: true,
      }),
      NgProgressModule,
      NgProgressRouterModule,
      RemoveCommaPipeModule,
      // AngularFireModule.initializeApp(environment_firebase.firebase)

    ),
    // provideFirebaseApp(() => initializeApp(environment_firebase.firebase)),
    // provideStorage(() => getStorage()),
    //  importProvidersFrom(AngularFireModule.initializeApp(environment_firebase.firebase)),
		// AngularFireStorageModule,
    // ✅ Các providers khác
    ...DATA_AUTH_IOC,
    AuthService,
    DatePipe,
    ModalService,
    NotificationModalService,
    BreadcrumbService,

    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true,
    },
  ],
};
