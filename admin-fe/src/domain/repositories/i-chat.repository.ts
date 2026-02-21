import { Observable } from 'rxjs';
import { IMessage, IConversation } from 'data/requests/chat/chat.request';
import { MessageResponse, ConversationListResponse } from 'data/responses/chat/chat.response';

export abstract class IChatRepository {
	abstract sendMessage(message: IMessage): Observable<MessageResponse<IMessage>>;
	abstract getConversation(userId: number, correspondentId: number): Observable<ConversationListResponse<IMessage>>;
	abstract markAsRead(messageId: string): Observable<MessageResponse<string>>;
	abstract getConversationList(): Observable<ConversationListResponse<IConversation>>;
}
