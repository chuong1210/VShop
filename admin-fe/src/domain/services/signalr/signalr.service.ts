import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from 'environments/environment.development';
import { IMessage } from 'data/requests/chat/chat.request';

@Injectable({
	providedIn: 'root',
})
export class SignalRService {
	private hubConnection: signalR.HubConnection | undefined;
	private messageReceivedSubject = new BehaviorSubject<IMessage | null>(null);
	private userTypingSubject = new BehaviorSubject<number | null>(null);
	private userStoppedTypingSubject = new BehaviorSubject<number | null>(null);

	public messageReceived$: Observable<IMessage | null> = this.messageReceivedSubject.asObservable();
	public userTyping$: Observable<number | null> = this.userTypingSubject.asObservable();
	public userStoppedTyping$: Observable<number | null> = this.userStoppedTypingSubject.asObservable();

	constructor() {}

	public startConnection(token: string): Promise<void> {
		this.hubConnection = new signalR.HubConnectionBuilder()
			.withUrl(`${environment.api}/smw-api/chatHub`, {
				accessTokenFactory: () => token,
			})
			.withAutomaticReconnect()
			.build();

		return this.hubConnection
			.start()
			.then(() => {
				console.log('SignalR Connected');
				this.registerSignalREvents();
			})
			.catch(err => console.error('Error while starting SignalR connection: ' + err));
	}

	private registerSignalREvents(): void {
		if (!this.hubConnection) return;

		this.hubConnection.on('ReceiveMessage', (message: IMessage) => {
			console.log('Message received:', message);
			this.messageReceivedSubject.next(message);
		});

		this.hubConnection.on('UserTyping', (senderId: number) => {
			console.log('User typing:', senderId);
			this.userTypingSubject.next(senderId);
		});

		this.hubConnection.on('UserStoppedTyping', (senderId: number) => {
			console.log('User stopped typing:', senderId);
			this.userStoppedTypingSubject.next(senderId);
		});
	}

	public notifyTyping(receiverId: number): Promise<void> {
		if (!this.hubConnection) return Promise.reject('No connection');
		return this.hubConnection.invoke('UserTyping', receiverId);
	}

	public notifyStoppedTyping(receiverId: number): Promise<void> {
		if (!this.hubConnection) return Promise.reject('No connection');
		return this.hubConnection.invoke('UserStoppedTyping', receiverId);
	}

	public stopConnection(): void {
		if (this.hubConnection) {
			this.hubConnection.stop();
		}
	}
}
