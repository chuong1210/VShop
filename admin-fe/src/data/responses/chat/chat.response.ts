export interface MessageResponse<T> {
	data: T;
	succeeded: boolean;
	code: number;
	messages?: string[];
}

export interface ConversationListResponse<T> {
	data: T[];
	succeeded: boolean;
	code: number;
	messages?: string[];
}
