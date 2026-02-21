import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { IMessage, IConversation } from 'data/requests/chat/chat.request';
import { MessageResponse, ConversationListResponse } from 'data/responses/chat/chat.response';
import { ChatRepository } from 'data/datasources/remote/chat/chat.repository';

@Injectable({
	providedIn: 'root',
})
export class ChatService {
	constructor(private chatRepository: ChatRepository) {}

	sendMessage(message: IMessage): Observable<MessageResponse<IMessage>> {
		return this.chatRepository.sendMessage(message);
	}

	getConversation(userId: number, correspondentId: number): Observable<ConversationListResponse<IMessage>> {
		return this.chatRepository.getConversation(userId, correspondentId);
	}

	markAsRead(messageId: string): Observable<MessageResponse<string>> {
		return this.chatRepository.markAsRead(messageId);
	}

	getConversationList(): Observable<ConversationListResponse<IConversation>> {
		return this.chatRepository.getConversationList();
	}
}
