import { DatePipe } from '@angular/common';
import { Component, signal, WritableSignal, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { AppFormService } from 'app/_components/form/app-form.service';
import { queryKey } from 'config/query-key';
import { statusProduct } from 'config/status';
import { ICategory } from 'data/requests/category/category.request';
import { IProduct } from 'data/requests/product/product.request';
import { ProductService } from 'domain/services/product/product.service';
import { ToastrService } from 'ngx-toastr';
import { fromEvent, lastValueFrom, takeUntil } from 'rxjs';
import { array, number, object, string } from 'yup';

@Component({
	selector: 'app-product-form',
	templateUrl: './product-form.component.html',
	styles: `
		:host ::ng-deep {
			.p-card {
				border-radius: 12px;
				overflow: hidden;
			}

			.p-card .p-card-body {
				padding: 0;
			}

			.p-card .p-card-content {
				padding: 0;
			}

			.form-group {
				animation: fadeInUp 0.3s ease-in-out;
			}

			@keyframes fadeInUp {
				from {
					opacity: 0;
					transform: translateY(10px);
				}
				to {
					opacity: 1;
					transform: translateY(0);
				}
			}

			.p-button {
				transition: all 0.3s ease;
			}

			.p-button:hover {
				transform: translateY(-2px);
				box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
			}

			.p-chip {
				font-weight: 600;
				padding: 0.5rem 1rem;
			}
		}
	`,
})
export class ProductFormComponent {
	id: WritableSignal<number> = signal(0);
	status = statusProduct;

	formSchema = object({
		name: string().required('Tên sản phẩm là bắt buộc'),
		internalCode: string().required('Mã sản phẩm là bắt buộc'),
		price: number().required('Giá sản phẩm là bắt buộc').min(0, 'Giá phải lớn hơn 0'),
		categoryId: number().required('Loại sản phẩm là bắt buộc'),
		describes: string().required('Mô tả là bắt buộc'),
		feature: string().required('Tính năng là bắt buộc'),
		specifications: string().required('Thông số kỹ thuật là bắt buộc'),
		images: array().of(string()).default([]).min(1, 'Cần ít nhất 1 ảnh sản phẩm'),
	});

	detailQuery = injectQuery(() => ({
		refetchOnWindowFocus: false,
		queryKey: [queryKey.product.detail(this.id())],
		enabled: this.id() > 0,
		queryFn: (context) => {
			const abort$ = fromEvent(context.signal, 'abort');
			return lastValueFrom(this.productService.detailProduct({ Id: this.id() }).pipe(takeUntil(abort$)));
		},
	}));

	categoryColumns: {
		title: string;
		getter: (item: ICategory) => void;
		name?: string;
	}[] = [
		{ title: 'Mã loại sản phẩm', getter: (item: ICategory) => item['internalCode'] },
		{ title: 'Tên loại sản phẩm', getter: (item: ICategory) => item['name'] },
		{
			title: 'Loại sản phẩm cha',
			getter: (item: ICategory) => {
				return item?.['parent']?.['name'] || 'N/A';
			},
		},
	];

	addMutate = injectMutation(() => ({
		mutationFn: (data: IProduct) => {
			return lastValueFrom(this.productService.addProduct(data));
		},
		onSuccess: (data) => {
			this.toast.success('Thêm sản phẩm thành công', 'Thành công');
			this.router.navigate(['admin/master-data/product']);
		},
		onError: (error: any) => {
			this.toast.error(error.error.messages?.[0] || error.message || 'Có lỗi xảy ra', 'Lỗi');
		},
		retry: (failureCount, error: any) => {
			if (error.status === 403) {
				this.toast.error('Bạn không có quyền thực hiện thao tác này', 'Lỗi phân quyền');
				return false;
			}
			return failureCount < 3;
		},
	}));

	updateMutate = injectMutation(() => ({
		mutationFn: (data: IProduct) => {
			return lastValueFrom(this.productService.updateProduct(data));
		},
		onSuccess: (data) => {
			this.toast.success('Cập nhật sản phẩm thành công', 'Thành công');
			this.router.navigate(['admin/master-data/product']);
		},
		onError: (error: any) => {
			this.toast.error(error.error.messages?.[0] || error.message || 'Có lỗi xảy ra', 'Lỗi');
		},
		retry: (failureCount, error: any) => {
			if (error.status === 403) {
				this.toast.error('Bạn không có quyền thực hiện thao tác này', 'Lỗi phân quyền');
				return false;
			}
			return failureCount < 3;
		},
	}));

	updateStatusMutate = injectMutation(() => ({
		mutationFn: (data: IProduct) => {
			return lastValueFrom(
				this.productService.updateStatusProduct({
					productId: data.id,
					status: data.status,
				}),
			);
		},
		onSuccess: (data) => {
			this.toast.success('Cập nhật trạng thái sản phẩm thành công', 'Thành công');
			this.detailQuery.refetch();
		},
		onError: (error: any) => {
			this.toast.error(error.error.messages?.[0] || error.message || 'Có lỗi xảy ra', 'Lỗi');
		},
		retry: (failureCount, error: any) => {
			if (error.status === 403) {
				this.toast.error('Bạn không có quyền thực hiện thao tác này', 'Lỗi phân quyền');
				return false;
			}
			return failureCount < 3;
		},
	}));

	form = {} as IProduct;
	errors: WritableSignal<{ [key: string]: string }> = signal({});

	constructor(
		private productService: ProductService,
		private router: Router,
		private activatedRoute: ActivatedRoute,
		private formService: AppFormService,
		private toast: ToastrService,
	) {
		this.activatedRoute.queryParams.subscribe({
			next: (value) => {
				const idValue = Number(value['id']) || 0;
				this.id.set(idValue);
				this.form.id = idValue;

				if (idValue === 0) {
					this.form = { id: 0 } as IProduct;
					this.errors.set({});
				}
			},
		});

		// Effect để tự động đổ data vào form khi load xong
		effect(() => {
			const data = this.detailQuery.data()?.data;

			if (data && this.id() > 0) {
				console.log('Loading data into form:', data);

				// Đổ tất cả data vào form
				this.form = {
					id: data.id,
					internalCode: data.internalCode,
					name: data.name,
					categoryId: data.categoryId,
					price: data.price,
					describes: data.describes || '',
					feature: data.feature || '',
					specifications: data.specifications || '',
					images: data.images || [],
					status: data.status,
				};

				console.log('Form after loading:', this.form);
			}
		});
	}

	get isFieldDisabled(): boolean {
		if (this.id() === 0) {
			return false;
		}

		const productData = this.detailQuery.data()?.data;
		if (!productData) {
			return true;
		}

		return productData.status !== 0;
	}

	getStatusLabel(status: number): string {
		if (this.status && this.status[status] && this.status[status].label) {
			return this.status[status].label;
		}

		const defaultLabels: { [key: number]: string } = {
			0: 'Nháp',
			1: 'Đã duyệt',
			2: 'Tạm ngưng',
			3: 'Hết hàng',
			4: 'Dừng hoạt động',
		};

		return defaultLabels[status] || 'Không xác định';
	}

	getStatusClass(status: number): string {
		const statusClasses: { [key: number]: string } = {
			0: 'bg-gray-100 text-gray-800',
			1: 'bg-green-100 text-green-800',
			2: 'bg-orange-100 text-orange-800',
			3: 'bg-slate-100 text-slate-800',
			4: 'bg-red-100 text-red-800',
		};
		return statusClasses[status] || 'bg-gray-100 text-gray-800';
	}

	formatDate(date: string): string {
		const newDate = new Date(date);
		const datePipe = new DatePipe('en-US');
		return datePipe.transform(newDate, 'dd/MM/yyyy') || '';
	}

	goBack(): void {
		this.router.navigate(['admin/master-data/product']);
	}

	async onSubmit(status: number = -1) {
		console.log('Form data before validation:', this.form);

		const result = await this.formService.validate<IProduct>(this.formSchema, this.form);

		console.log('Validation result:', result);

		if (result.message) {
			this.errors.set(result.message);
			this.toast.warning('Vui lòng kiểm tra lại thông tin đã nhập', 'Cảnh báo');

			setTimeout(() => {
				const firstErrorElement = document.querySelector('.error');
				if (firstErrorElement) {
					firstErrorElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
				}
			}, 100);
			return;
		}

		if (result.valid && result.data) {
			if (this.id() > 0) {
				if (status > -1) {
					this.updateStatusMutate.mutate({
						...result.data,
						status,
					});
					return;
				}

				this.updateMutate.mutate({
					...result.data,
				});
				return;
			}

			this.addMutate.mutate({
				...result.data,
			});
		}
	}
}
