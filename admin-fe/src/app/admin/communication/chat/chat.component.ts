import { Component, OnInit, OnDestroy, ViewChild, ElementRef, signal, computed, effect } from '@angular/core';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { ChatService } from 'domain/services/chat/chat.service';
import { SignalRService } from 'domain/services/signalr/signalr.service';
import { ToastrService } from 'ngx-toastr';
import { IConversation, IMessage } from 'data/requests/chat/chat.request';

@Component({
	selector: 'app-chat',
	templateUrl: './chat.component.html',
	styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy {
	@ViewChild('messageContainer') messageContainer!: ElementRef;

	// Signals
	conversations = signal<IConversation[]>([]);
	messages = signal<IMessage[]>([]);
	selectedUserId = signal<number | null>(null);
	selectedUserName = signal<string>('');
	currentUserId = signal<number>(1);
	loadingMessages = signal<boolean>(false);
	sendingMessage = signal<boolean>(false);
	isTyping = signal<boolean>(false);
	searchTerm = signal<string>('');

	// Form data
	newMessage = '';

	// Typing timeout
	private typingTimeout: any;
	private typingSubject = new Subject<void>();
	private destroy$ = new Subject<void>();

	// Computed
	filteredConversations = computed(() => {
		const term = this.searchTerm().toLowerCase();
		if (!term) return this.conversations();

		return this.conversations().filter(conv =>
			conv.userName.toLowerCase().includes(term) ||
			conv.lastMessage.toLowerCase().includes(term)
		);
	});

	constructor(
		private chatService: ChatService,
		private signalRService: SignalRService,
		private toast: ToastrService
	) {
		// Effect để scroll xuống bottom khi có tin nhắn mới
		effect(() => {
			const msgs = this.messages();
			if (msgs.length > 0) {
				setTimeout(() => this.scrollToBottom(), 100);
			}
		});
	}

	ngOnInit(): void {
		// Lấy current user ID từ localStorage hoặc auth service
		const userStr = localStorage.getItem('user');
		if (userStr) {
			const user = JSON.parse(userStr);
			this.currentUserId.set(user.id || 1);
		}

		// Load danh sách conversation
		this.loadConversations();

		// Khởi động SignalR
		this.initSignalR();

		// Setup typing debounce
		this.setupTypingHandler();
	}

	ngOnDestroy(): void {
		this.destroy$.next();
		this.destroy$.complete();
		this.signalRService.stopConnection();
	}

	private initSignalR(): void {
		const token = localStorage.getItem('token') || '';

		this.signalRService.startConnection(token).then(() => {
			// Lắng nghe tin nhắn mới
			this.signalRService.messageReceived$
				.pipe(takeUntil(this.destroy$))
				.subscribe(message => {
					if (message) {
						this.handleNewMessage(message);
					}
				});

			// Lắng nghe user typing
			this.signalRService.userTyping$
				.pipe(takeUntil(this.destroy$))
				.subscribe(senderId => {
					if (senderId === this.selectedUserId()) {
						this.isTyping.set(true);
					}
				});

			// Lắng nghe user stopped typing
			this.signalRService.userStoppedTyping$
				.pipe(takeUntil(this.destroy$))
				.subscribe(senderId => {
					if (senderId === this.selectedUserId()) {
						this.isTyping.set(false);
					}
				});
		}).catch(err => {
			console.error('SignalR connection failed:', err);
			this.toast.error('Không thể kết nối đến server chat', 'Lỗi');
		});
	}

	private setupTypingHandler(): void {
		this.typingSubject
			.pipe(
				takeUntil(this.destroy$),
				debounceTime(300),
				distinctUntilChanged()
			)
			.subscribe(() => {
				if (this.selectedUserId()) {
					this.signalRService.notifyTyping(this.selectedUserId()!);
				}
			});
	}

	loadConversations(): void {
		this.chatService.getConversationList()
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (response) => {
					if (response.succeeded) {
						this.conversations.set(response.data || []);
					}
				},
				error: (error) => {
					console.error('Error loading conversations:', error);
					this.toast.error('Không thể tải danh sách cuộc trò chuyện', 'Lỗi');
				}
			});
	}

	selectConversation(userId: number): void {
		const conv = this.conversations().find(c => c.userId === userId);
		if (!conv) return;

		this.selectedUserId.set(userId);
		this.selectedUserName.set(conv.userName);
		this.loadMessages(userId);
	}

	loadMessages(correspondentId: number): void {
		this.loadingMessages.set(true);

		this.chatService.getConversation(this.currentUserId(), correspondentId)
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (response) => {
					if (response.succeeded) {
						this.messages.set(response.data || []);

						// Mark all as read
						const unreadMessages = response.data?.filter(m =>
							!m.isRead && m.receiverId === this.currentUserId()
						) || [];

						unreadMessages.forEach(msg => {
							if (msg.id) {
								this.markAsRead(msg.id);
							}
						});
					}
					this.loadingMessages.set(false);
				},
				error: (error) => {
					console.error('Error loading messages:', error);
					this.toast.error('Không thể tải tin nhắn', 'Lỗi');
					this.loadingMessages.set(false);
				}
			});
	}

sendMessage(): void {
	if (!this.newMessage.trim() || !this.selectedUserId()) {
		return;
	}

	this.sendingMessage.set(true);

	const message: IMessage = {
		senderId: this.currentUserId(),
		receiverId: this.selectedUserId()!,
		content: this.newMessage.trim(),
		sentAt: new Date(),
		isRead: false,
    senderName: this.currentUserId() === 1 ? 'Admin' : 'User', // Example names
    senderAvatar: this.currentUserId() === 1 ? 'https://www.pngmart.com/files/21/Admin-Profile-Vector-PNG-Clipart.png' : 'https://www.pngkey.com/png/full/72-729716_user-avatar-png-graphic-free-download-icon.png' // Example avatars
	};

	this.chatService.sendMessage(message)
		.pipe(takeUntil(this.destroy$))
		.subscribe({
			next: (response) => {
				if (response.succeeded) {
					console.log('✅ Message sent successfully:', response.data);

					// ✅ KHÔNG thêm tin nhắn vào list ở đây
					// SignalR sẽ tự động gửi lại qua ReceiveMessage event

					// Clear input
					this.newMessage = '';

					// Notify stopped typing
					this.signalRService.notifyStoppedTyping(this.selectedUserId()!);

					// ✅ KHÔNG gọi updateConversationList ở đây
					// Sẽ được xử lý trong handleNewMessage
				}
				this.sendingMessage.set(false);
			},
			error: (error) => {
				console.error('Error sending message:', error);
				this.toast.error('Không thể gửi tin nhắn', 'Lỗi');
				this.sendingMessage.set(false);
			}
		});
}

	onTyping(): void {
		this.typingSubject.next();

		// Clear previous timeout
		if (this.typingTimeout) {
			clearTimeout(this.typingTimeout);
		}

		// Set timeout to notify stopped typing
		this.typingTimeout = setTimeout(() => {
			if (this.selectedUserId()) {
				this.signalRService.notifyStoppedTyping(this.selectedUserId()!);
			}
		}, 1000);
	}

private handleNewMessage(message: IMessage): void {
	console.log('📩 Handling new message:', message);
	console.log('Current userId:', this.currentUserId());
	console.log('Selected userId:', this.selectedUserId());
	console.log('Message senderId:', message.senderId);
	console.log('Message receiverId:', message.receiverId);

	// ✅ Check if message belongs to current conversation
	const isCurrentConversation =
		(message.senderId === this.selectedUserId() && message.receiverId === this.currentUserId()) ||
		(message.receiverId === this.selectedUserId() && message.senderId === this.currentUserId());

	console.log('Is current conversation:', isCurrentConversation);

	if (isCurrentConversation) {
		// ✅ Add message to list if not already exists
		this.messages.update(msgs => {
			const exists = msgs.some(m => m.id === message.id);
			if (exists) {
				console.log('⚠️ Message already exists, skipping');
				return msgs;
			}
			console.log('✅ Adding message to list');
			return [...msgs, message];
		});

		// Mark as read if message is from other user
		if (message.senderId === this.selectedUserId() && message.id) {
			this.markAsRead(message.id);
		}
	} else {
		console.log('⚠️ Message not for current conversation');
	}

	// Update conversation list
	this.updateConversationList(message);

	// Show notification if not my message and not viewing this conversation
	if (message.senderId !== this.currentUserId()) {
		if (!isCurrentConversation) {
			this.toast.info(`Tin nhắn mới từ ${this.getConversationName(message.senderId)}`, 'Thông báo');
		}
	}
}
	private markAsRead(messageId: string): void {
		this.chatService.markAsRead(messageId)
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (response) => {
					if (response.succeeded) {
						// Update message in list
						this.messages.update(msgs =>
							msgs.map(m => m.id === messageId ? { ...m, isRead: true } : m)
						);

						// Update unread count in conversation list
						this.updateUnreadCount();
					}
				},
				error: (error) => {
					console.error('Error marking message as read:', error);
				}
			});
	}

	private updateConversationList(message: IMessage): void {
		this.conversations.update(convs => {
			const otherUserId = message.senderId === this.currentUserId()
				? message.receiverId
				: message.senderId;

			const existingIndex = convs.findIndex(c => c.userId === otherUserId);

			if (existingIndex >= 0) {
				// Update existing conversation
				const updated = [...convs];
				updated[existingIndex] = {
					...updated[existingIndex],
					lastMessage: message.content,
					lastMessageTime: message.sentAt || new Date(),
					unreadCount: message.senderId !== this.currentUserId()
						? updated[existingIndex].unreadCount + 1
						: updated[existingIndex].unreadCount
				};

				// Move to top
				const [conv] = updated.splice(existingIndex, 1);
				return [conv, ...updated];
			} else {
				// Add new conversation (fetch user info from API in real scenario)
				const newConv: IConversation = {
					userId: otherUserId,
					userName: `User ${otherUserId}`,
					lastMessage: message.content,
					lastMessageTime: message.sentAt || new Date(),
					unreadCount: message.senderId !== this.currentUserId() ? 1 : 0
				};
				return [newConv, ...convs];
			}
		});
	}

	private updateUnreadCount(): void {
		this.conversations.update(convs =>
			convs.map(c =>
				c.userId === this.selectedUserId()
					? { ...c, unreadCount: 0 }
					: c
			)
		);
	}

	private getConversationName(userId: number): string {
		const conv = this.conversations().find(c => c.userId === userId);
		return conv?.userName || `User ${userId}`;
	}

	private scrollToBottom(): void {
		try {
			if (this.messageContainer) {
				const element = this.messageContainer.nativeElement;
				element.scrollTop = element.scrollHeight;
			}
		} catch (err) {
			console.error('Error scrolling to bottom:', err);
		}
	}

	formatTime(date: Date | undefined): string {
		if (!date) return '';

		const now = new Date();
		const messageDate = new Date(date);
		const diffMs = now.getTime() - messageDate.getTime();
		const diffMins = Math.floor(diffMs / 60000);
		const diffHours = Math.floor(diffMs / 3600000);
		const diffDays = Math.floor(diffMs / 86400000);

		if (diffMins < 1) return 'Vừa xong';
		if (diffMins < 60) return `${diffMins} phút trước`;
		if (diffHours < 24) return `${diffHours} giờ trước`;
		if (diffDays < 7) return `${diffDays} ngày trước`;

		return messageDate.toLocaleDateString('vi-VN', {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric'
		});
	}
}
