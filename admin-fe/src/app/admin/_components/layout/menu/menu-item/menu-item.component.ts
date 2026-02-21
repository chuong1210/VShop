import { Component, EventEmitter, Input, Output } from '@angular/core';
import { OptionTpe } from 'core/types/types';

@Component({
	selector: 'app-menu-item',
	templateUrl: './menu-item.component.html',
	styleUrls: ['./menu-item.component.scss']
})
export class MenuItemComponent {
	@Input() data!: OptionTpe;
	@Input() activeItem: string = '';
	@Output() onClick = new EventEmitter<string>();

	isExpanded = false;

	get isActive(): boolean {
		if (this.data.to) {
			return this.activeItem === this.data.to;
		}

		if (this.data.options) {
			return this.data.options.some(opt => opt.to === this.activeItem);
		}

		return false;
	}

	toggleMenu(): void {
		if (this.data.options && this.data.options.length > 0) {
			this.isExpanded = !this.isExpanded;
		}
	}

	onSubItemClick(path: string): void {
		this.onClick.emit(path);
	}
}
