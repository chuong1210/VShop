import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AdminSharedModule } from '../_modules/admin-shared.module';
import { CommunicationRoutingModule } from './communication-routing.module';
import { CommunicationComponent } from './communication.component';
import { ChatComponent } from './chat/chat.component';

@NgModule({
	declarations: [
		CommunicationComponent,
		ChatComponent,
	],
	imports: [
		CommonModule,
		FormsModule,
		ReactiveFormsModule,
		CommunicationRoutingModule,
		AdminSharedModule,
	],
})
export class CommunicationModule {}
