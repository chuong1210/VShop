import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CommunicationComponent } from './communication.component';
import { ChatComponent } from './chat/chat.component';

const routes: Routes = [
	{
		path: '',
		component: CommunicationComponent,
		children: [
			{
				path: 'chat',
				component: ChatComponent,
			},
			{
				path: '',
				redirectTo: 'chat',
				pathMatch: 'full',
			},
		],
	},
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
})
export class CommunicationRoutingModule {}
