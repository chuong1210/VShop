
type MetaType = object;

type ResponseType<T = any> = {
  exp(exp: unknown): unknown;
  token(token: string): string;
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
