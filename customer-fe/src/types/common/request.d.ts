
type MetaType = {};

type ResponseType<T = any> = {
	extra: {
		currentPage: number;
		totalPages: number;
		totalCount: number;
		pageSize: number;
	};
	data: T;
	messages?: string[];
};

export type { MetaType, ResponseType };
