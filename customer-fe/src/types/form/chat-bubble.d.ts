import { MouseEventHandler } from "react";
import { MessageCollectionType } from "@type/collection/message-collection";

type ChatBubbleProps ={
  onClick: MouseEventHandler<HTMLButtonElement>;
}


 type ChatContextType ={
  messages: MessageCollectionType[]
  sendMessage: (content: string) => void
  showChat: boolean
    setCorrespondentId: (id: number) => void;

  setShowChat: (value: boolean) => void
  currentUserId: number | null
  startTyping: () => void; // ➕ Hàm gửi sự kiện đang nhập
  stopTyping: () => void;  // ➕ Hàm gửi sự kiện dừng nhập
    typingUsers: Set<number>; // ✅ Add this

}

type  SendMessageType= {
  content: string
  receiverId: number
  isRead:boolean
  
}

type  MarkAsReadType= {
  messageId: number
}




  export type { ChatBubbleProps ,ChatContextType, SendMessageType, MarkAsReadType };

