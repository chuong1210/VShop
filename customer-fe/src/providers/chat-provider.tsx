import {
  createContext,
  useContext,
  useState,
  useEffect,
  type ReactNode,
} from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  type MessageType,
} from "@microsoft/signalr";
import { useCookies } from "@hook/index";
import { usePost } from "@hook/mutations";
import { useGet } from "@hook/queries";
import type {
  ChatContextType,
  SendMessageType,
  MarkAsReadType,
} from "@type/form";
import { MessageCollectionType } from "@type/collection/message-collection";

const ChatContext = createContext<ChatContextType | null>(null);

export const useChatContext = () => useContext(ChatContext);

export const ChatProvider = ({ children }: { children: ReactNode }) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [messages, setMessages] = useState<MessageType[]>([]);
  const [typingUsers, setTypingUsers] = useState<Set<number>>(new Set()); // 💡 Danh sách người đang nhập

  const [showChat, setShowChat] = useState(false);
  const [correspondentId, setCorrespondentId] = useState<number>(3);

  const cookies = useCookies();
  const currentUserId = cookies.get("user_id") ?? 1;

  const { data: conversationData } = useGet<MessageCollectionType[]>({
    api: "chat-conversation",
    filter: { userId: currentUserId, correspondentId },
    enable: !!currentUserId && !!correspondentId,
  });

  const sendMessageMutation = usePost<MessageCollectionType, SendMessageType>(
    "chat-send"
  );
  const markAsReadMutation = usePost<string, MarkAsReadType>("chat-mark-read");

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/chatHub`, {
        accessTokenFactory: async () => {
          const token = cookies.get("access_token");
          return token ? token : "";
        }, // Lấy JWT token từ localStorage
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => {
          console.log("Connected to SignalR Hub!");

          connection.on("ReceiveMessage", (message) => {
            setMessages((prevMessages) => [...prevMessages, message]);
            markAsReadMutation.mutate(message.id);
          });

          // 📌 Nhận sự kiện khi ai đó đang nhập tin nhắn
          connection.on("UserTyping", (senderId) => {
            setTypingUsers((prev) => new Set(prev).add(senderId));
          });

          // 📌 Nhận sự kiện khi ai đó dừng nhập tin nhắn
          connection.on("UserStoppedTyping", (senderId) => {
            setTypingUsers((prev) => {
              const newSet = new Set(prev);
              newSet.delete(senderId);
              return newSet;
            });
          });
        })
        .catch((e) => console.log("Connection to SignalR Hub failed: ", e));
    }
  }, [connection, markAsReadMutation]);

  useEffect(() => {
    if (conversationData?.data) {
      setMessages(conversationData.data);
    }
  }, [conversationData]);

  const sendMessage = async (content: string) => {
    if (correspondentId) {
      try {
        const result = await sendMessageMutation.mutateAsync({
          content,
          receiverId: correspondentId,
          isRead: false,
        });
        if (result.data) {
          stopTyping(); // ⏹ Khi gửi tin nhắn, dừng nhập
        }
      } catch (e) {
        console.error("Failed to send message:", e);
      }
    } else {
      console.log("No correspondent selected.");
    }
  };

  // 📌 Gửi sự kiện "đang nhập"
  const startTyping = () => {
    if (correspondentId) {
      connection?.invoke("UserTyping", correspondentId).catch(console.error);
    }
  };

  // 📌 Gửi sự kiện "dừng nhập"
  const stopTyping = () => {
    if (correspondentId) {
      connection
        ?.invoke("UserStoppedTyping", correspondentId)
        .catch(console.error);
    }
  };

  const value = {
    messages,
    sendMessage,
    showChat,
    setShowChat,
    setCorrespondentId,
    currentUserId,
    startTyping, // Thêm vào context
    stopTyping, // Thêm vào context
  };

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
};
