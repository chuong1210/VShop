import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment.development';
import { IChatRepository } from 'domain/repositories/i-chat.repository';
import { IMessage, IConversation } from 'data/requests/chat/chat.request';
import { MessageResponse, ConversationListResponse } from 'data/responses/chat/chat.response';

@Injectable({
	providedIn: 'root',
})
export class ChatRepository extends IChatRepository {
	private apiUrl = environment.api;

	constructor(private http: HttpClient) {
		super();
	}

	override sendMessage(message: IMessage): Observable<MessageResponse<IMessage>> {
		return this.http.post<MessageResponse<IMessage>>(`${this.apiUrl}/smw-api/chat/send`, message);
	}

	override getConversation(userId: number, correspondentId: number): Observable<ConversationListResponse<IMessage>> {
		const params = new HttpParams()
			.set('userId', userId.toString())
			.set('correspondentId', correspondentId.toString());

		return this.http.get<ConversationListResponse<IMessage>>(`${this.apiUrl}/smw-api/chat/conversation`, { params });
	}

override markAsRead(messageId: string): Observable<MessageResponse<string>> {
  return this.http.post<MessageResponse<string>>(
    `${this.apiUrl}/smw-api/chat/mark-read`,
    { messageId }
  );
}

	override getConversationList(): Observable<ConversationListResponse<IConversation>> {
		return this.http.get<ConversationListResponse<IConversation>>(`${this.apiUrl}/smw-api/chat/conversations`);
	}
}
