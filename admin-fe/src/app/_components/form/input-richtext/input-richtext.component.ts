import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
	selector: 'app-input-richtext-component',
	template: `
		<div class="flex flex-col gap-2">
			<label class="font-medium text-gray-700" *ngIf="label">
				{{ label }}
				<span *ngIf="required" class="text-red-500 ml-1">*</span>
			</label>

			<textarea
				[placeholder]="placeholder"
				[disabled]="disabled"
				[value]="value"
				(input)="onInput($event)"
				[class]="getTextareaClass()"
				rows="10"
			></textarea>

			<small *ngIf="errorMessage" class="text-red-500 text-sm mt-1 flex items-center gap-1">
				<i class="pi pi-exclamation-circle"></i>
				{{ errorMessage }}
			</small>
		</div>
	`,
	styles: [`
		textarea {
			width: 100%;
			padding: 0.75rem;
			border: 1px solid #d1d5db;
			border-radius: 0.375rem;
			font-size: 0.875rem;
			line-height: 1.5;
			font-family: inherit;
			transition: all 0.2s;
			resize: vertical;
			min-height: 200px;
		}

		textarea:focus {
			outline: none;
			border-color: #3b82f6;
			box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
		}

		textarea.error {
			border-color: #ef4444;
		}

		textarea.error:focus {
			box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
		}

		textarea:disabled {
			background-color: #f3f4f6;
			cursor: not-allowed;
			opacity: 0.6;
		}

		textarea::placeholder {
			color: #9ca3af;
		}
	`]
})
export class InputRichTextComponent {
	@Input() label: string = '';
	@Input() placeholder: string = 'Nhập nội dung...';
	@Input() required: boolean = false;
	@Input() disabled: boolean = false;
	@Input() value: string = '';
	@Input() errorMessage: string = '';

	@Output() onChange = new EventEmitter<string>();

	onInput(event: Event): void {
		const target = event.target as HTMLTextAreaElement;
		this.onChange.emit(target.value);
	}

	getTextareaClass(): string {
		return this.errorMessage ? 'error' : '';
	}
}
