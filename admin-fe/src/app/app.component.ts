import { Component, inject, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { provideQueryClient, QueryClient } from '@tanstack/angular-query-experimental';
import { NgProgressModule } from 'ngx-progressbar';
import { NgProgressRouterModule } from 'ngx-progressbar/router';
import { PrimeNGConfig } from 'primeng/api';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html',
	styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [  NgProgressModule,  // ✅ Đúng cách để import trong Angular 19
        NgProgressRouterModule,
        RouterModule,],
    providers: [PrimeNGConfig,
    //provideQueryClient(new QueryClient()),
    ],

})
export class AppComponent implements OnInit {
  title(title: any) {
    throw new Error('Method not implemented.');
  }
  private queryClient = inject(QueryClient); // ✅ Inject QueryClient vào component

	options = {
		min: 0,
		max: 100,
		ease: 'linear',
		speed: 200,
		trickleSpeed: 300,
		meteor: true,
		spinner: false,
		color: 'green',
		thick: true,
	};

	constructor(private primengConfig: PrimeNGConfig) {}

	ngOnInit() {
		this.primengConfig.setTranslation({
			dayNames: ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'],
			dayNamesShort: ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'],
			monthNames: ['01/', '02/', '03/', '04/', '05/', '06/', '07/', '08/', '09/', '10/', '11/', '12/'],
			monthNamesShort: [
				'Thg 1',
				'Thg 2',
				'Thg 3',
				'Thg 4',
				'Thg 5',
				'Thg 6',
				'Thg 7',
				'Thg 8',
				'Thg 9',
				'Thg 10',
				'Thg 11',
				'Thg 12',
			],
		});
	}
}
