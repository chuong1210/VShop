import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminComponent } from './admin.component';

const routes: Routes = [
	{
		path: '',
		component: AdminComponent,
		children: [
			{
				path: 'master-data',
				loadChildren: () => import('./master-data/master-data.module').then((m) => m.MasterDataModule),
			},
			{
				path: 'business',
				loadChildren: () => import('./business/business.module').then((m) => m.BusinessModule),
			},
			{
				path: 'system',
				loadChildren: () => import('./system/system.module').then((m) => m.SystemModule),
			},
			{
				path: 'dashboard',
				loadChildren: () => import('./dashboard/dashboard.module').then((m) => m.DashboardModule),
			},
			// 👇 Thêm route mới cho Communication
			{
				path: 'communication',
				loadChildren: () => import('./communication/communication.module').then((m) => m.CommunicationModule),
			},
			{
				path: '',
				redirectTo: 'dashboard',
				pathMatch: 'full',
			},
		],
	},
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
})
export class AdminRoutingModule {}
