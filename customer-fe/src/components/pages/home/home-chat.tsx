"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import {
  Box,
  Flex,
  Text,
  IconButton,
  VStack,
  HStack,
  Avatar,
  useColorModeValue,
  Textarea,
} from "@chakra-ui/react";
import { CloseIcon, ArrowForwardIcon } from "@chakra-ui/icons";
import { useChatContext } from "@provider/chat-provider";
import type { MessageCollectionType } from "@type/collection/message-collection";
import { motion, AnimatePresence } from "framer-motion";

const MotionBox = motion(Box);
const MotionFlex = motion(Flex);

const ChatBox = () => {
  const chatContext = useChatContext();
  const [inputMessage, setInputMessage] = useState("");
  const [textareaHeight, setTextareaHeight] = useState(40);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const bgColor = useColorModeValue("white", "gray.700");
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const primaryColor = "rgba(0, 150, 136, 0.8)";

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, []);

  useEffect(() => {
    if (chatContext?.messages) {
      scrollToBottom();
    }
  }, [chatContext?.messages, scrollToBottom]);

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputMessage(e.target.value);

    // ✅ Trigger typing notification
    if (chatContext && e.target.value) {
      chatContext.startTyping();

      // Clear previous timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }

      // Stop typing after 1 second of inactivity
      typingTimeoutRef.current = setTimeout(() => {
        chatContext.stopTyping();
      }, 1000);
    }

    if (e.target.value === "") {
      resetTextareaHeight();
      chatContext?.stopTyping();
    } else {
      adjustTextareaHeight();
    }
  };

  const adjustTextareaHeight = () => {
    if (textareaRef.current) {
      textareaRef.current.style.height = "auto";
      const height = Math.min(textareaRef.current.scrollHeight, 200);
      textareaRef.current.style.height = `${height}px`;
      setTextareaHeight(height);
    }
  };

  const resetTextareaHeight = () => {
    if (textareaRef.current) {
      textareaRef.current.style.height = "40px";
      setTextareaHeight(40);
    }
  };

  if (!chatContext) {
    return null;
  }

  const {
    messages,
    sendMessage,
    showChat,
    setShowChat,
    currentUserId,
    typingUsers,
  } = chatContext;

  const handleSend = () => {
    if (inputMessage.trim()) {
      sendMessage(inputMessage);
      setInputMessage("");
      resetTextareaHeight();

      // Clear typing timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    }
  };

  // ✅ Check if someone is typing
  const isTyping = typingUsers && typingUsers.size > 0;

  return (
    <AnimatePresence>
      {showChat && (
        <MotionBox
          position="fixed"
          bottom={20}
          right={5}
          width="350px"
          height="500px"
          bg={bgColor}
          borderRadius="lg"
          boxShadow="xl"
          display="flex"
          flexDirection="column"
          zIndex={50}
          initial={{ opacity: 0, y: 50, scale: 0.9 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 50, scale: 0.9 }}
          transition={{ duration: 0.3 }}
        >
          <MotionFlex
            justify="space-between"
            align="center"
            p={3}
            borderBottom="1px"
            borderColor={borderColor}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
          >
            <Text fontWeight="bold" color={primaryColor}>
              Chat với Admin
            </Text>
            <IconButton
              icon={<CloseIcon />}
              size="sm"
              variant="solid"
              onClick={() => setShowChat(false)}
              aria-label="Close chat"
              color={primaryColor}
              bg="lightblue"
              borderRadius="50%"
              w="30px"
              h="30px"
              minW="30px"
              _hover={{ bg: "blue.100" }}
            />
          </MotionFlex>

          <VStack flex={1} overflowY="auto" p={3} spacing={3} align="stretch">
            <AnimatePresence>
              {messages.map((msg: MessageCollectionType, index: number) => (
                <MotionBox
                  key={index}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -20 }}
                  transition={{ duration: 0.3 }}
                >
                  <HStack
                    justify={
                      msg.senderId === currentUserId ? "flex-end" : "flex-start"
                    }
                  >
                    {msg.senderId !== currentUserId && (
                      <Avatar
                        size="sm"
                        name={msg.senderName}
                        src={msg.senderAvatar}
                      />
                    )}
                    <Box
                      maxW="70%"
                      p={2}
                      borderRadius="lg"
                      bg={
                        msg.senderId === currentUserId
                          ? primaryColor
                          : "gray.100"
                      }
                      color={msg.senderId === currentUserId ? "white" : "black"}
                    >
                      <Text fontSize="sm">{msg.content}</Text>
                    </Box>
                    {msg.senderId === currentUserId && (
                      <Avatar size="sm" name="You" src="/your-avatar.jpg" />
                    )}
                  </HStack>
                </MotionBox>
              ))}
            </AnimatePresence>

            {/* ✅ Typing indicator */}
            {isTyping && (
              <HStack justify="flex-start">
                <Text fontSize="sm" color="gray.500" fontStyle="italic">
                  Admin đang nhập...
                </Text>
              </HStack>
            )}

            <div ref={messagesEndRef} />
          </VStack>

          <MotionFlex
            p={3}
            borderTop="1px"
            borderColor={borderColor}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
          >
            <Flex width="100%" alignItems="center">
              <Textarea
                ref={textareaRef}
                value={inputMessage}
                onChange={handleInputChange}
                placeholder="Type a message..."
                mr={2}
                minHeight="40px"
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.shiftKey) {
                    e.preventDefault();
                    handleSend();
                  }
                }}
                focusBorderColor={primaryColor}
                flexGrow={1}
              />
              <IconButton
                icon={<ArrowForwardIcon />}
                onClick={handleSend}
                colorScheme="teal"
                aria-label="Send message"
                bg={primaryColor}
                _hover={{ bg: "rgba(0, 150, 136, 0.9)" }}
                flexShrink={0}
                h={10}
                w={10}
              />
            </Flex>
          </MotionFlex>
        </MotionBox>
      )}
    </AnimatePresence>
  );
};

export default ChatBox;
