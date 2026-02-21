import {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
  type ReactNode,
} from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";
import { usePost } from "@hook/mutations";
import { useGet } from "@hook/queries";
import type {
  ChatContextType,
  SendMessageType,
  MarkAsReadType,
} from "@type/form";
import { cookies } from "@lib/index";
import { MessageCollectionType } from "@type/collection/message-collection";

const ChatContext = createContext<ChatContextType | null>(null);

export const useChatContext = () => useContext(ChatContext);

export const ChatProvider = ({ children }: { children: ReactNode }) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [messages, setMessages] = useState<any[]>([]);
  const [typingUsers, setTypingUsers] = useState<Set<number>>(new Set());
  const [showChat, setShowChat] = useState(false);
  const [correspondentId, setCorrespondentId] = useState<number>(1);

  // ✅ Use ref to track if connection is being established
  const isConnecting = useRef(false);
  const connectionRef = useRef<HubConnection | null>(null);

  const currentUserId = cookies.get("user_id");
  console.log("chat provider", currentUserId);

  const { data: conversationData } = useGet<MessageCollectionType[]>({
    api: "chat-conversation",
    filter: { userId: currentUserId, correspondentId },
    enable: !!currentUserId && !!correspondentId,
  });

  const sendMessageMutation = usePost<MessageCollectionType, SendMessageType>(
    "chat-send"
  );
  const markAsReadMutation = usePost<string, MarkAsReadType>("chat-mark-read");

  // ✅ Initialize connection ONCE
  useEffect(() => {
    if (isConnecting.current || connectionRef.current) {
      console.log("⚠️ Connection already exists or is being created");
      return;
    }

    isConnecting.current = true;
    console.log("🔧 Creating new SignalR connection...");

    const newConnection = new HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/chatHub`, {
        accessTokenFactory: async () => {
          const token = cookies.get("access_token");
          return token || "";
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        },
      })
      .build();

    connectionRef.current = newConnection;
    setConnection(newConnection);

    return () => {
      console.log("🧹 Cleaning up connection...");
      if (connectionRef.current) {
        connectionRef.current.stop().then(() => {
          console.log("❌ Connection stopped");
          connectionRef.current = null;
          isConnecting.current = false;
        });
      }
    };
  }, []); // ⚠️ Empty dependency array - only run once

  // ✅ Start connection and register events
  useEffect(() => {
    if (!connection) return;

    const startConnection = async () => {
      try {
        // Check if already connected or connecting
        if (
          connection.state === HubConnectionState.Connected ||
          connection.state === HubConnectionState.Connecting
        ) {
          console.log("⚠️ Already connected or connecting");
          return;
        }

        await connection.start();
        console.log("✅ Connected to SignalR Hub!");

        // Register events
        connection.on("ReceiveMessage", (message) => {
          console.log("📩 Message received:", message);
          setMessages((prevMessages) => [...prevMessages, message]);
          markAsReadMutation.mutate({ messageId: message.id });
        });

        connection.on("UserTyping", (senderId) => {
          console.log("📝 User typing:", senderId);
          setTypingUsers((prev) => new Set(prev).add(senderId));
        });

        connection.on("UserStoppedTyping", (senderId) => {
          console.log("✋ User stopped typing:", senderId);
          setTypingUsers((prev) => {
            const newSet = new Set(prev);
            newSet.delete(senderId);
            return newSet;
          });
        });

        // Handle reconnection
        connection.onreconnecting(() => {
          console.log("🔄 Reconnecting...");
        });

        connection.onreconnected(() => {
          console.log("✅ Reconnected!");
        });

        connection.onclose((error) => {
          console.error("❌ Connection closed:", error);
        });
      } catch (error) {
        console.error("❌ Connection failed:", error);
      }
    };

    startConnection();
  }, [connection, markAsReadMutation]);

  // ✅ Update messages from API
  useEffect(() => {
    if (conversationData?.data) {
      setMessages(conversationData.data);
    }
  }, [conversationData]);

  const sendMessage = async (content: string) => {
    if (!correspondentId) {
      console.log("No correspondent selected.");
      return;
    }

    try {
      const result = await sendMessageMutation.mutateAsync({
        content,
        receiverId: correspondentId,
        isRead: false,
      });
      if (result.data) {
        stopTyping();
      }
    } catch (e) {
      console.error("Failed to send message:", e);
    }
  };

  const startTyping = () => {
    if (correspondentId && connection?.state === HubConnectionState.Connected) {
      connection.invoke("UserTyping", correspondentId).catch(console.error);
    }
  };

  const stopTyping = () => {
    if (correspondentId && connection?.state === HubConnectionState.Connected) {
      connection
        .invoke("UserStoppedTyping", correspondentId)
        .catch(console.error);
    }
  };

  const value: ChatContextType = {
    messages,
    sendMessage,
    showChat,
    setShowChat,
    setCorrespondentId,
    currentUserId,
    startTyping,
    stopTyping,
    typingUsers, // ✅ Add this if not in type
  };

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
};
