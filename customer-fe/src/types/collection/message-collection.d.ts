  type MessageCollectionType =BaseCollectionType&{
    id: number;
    content?: string;
    senderId?: string;
    receiverId: string;
    timestamp: string;
  }
  export type { MessageCollectionType };
