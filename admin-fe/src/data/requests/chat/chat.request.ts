// chat.request.ts
export interface IMessage {
	id?: string;
	senderId: number;
	receiverId: number;
	content: string;
	sentAt?: Date;
	isRead?: boolean;
	senderName?: string; // ✅ Add this
	senderAvatar?: string; // ✅ Add this
	receiverName?: string; // ✅ Add this for conversation list
}

export interface IConversation {
	userId: number;
	userName: string;
	userAvatar?: string;
	lastMessage: string;
	lastMessageTime: Date;
	unreadCount: number;
}
