import { IconButton } from "@chakra-ui/react";
import { ChatIcon } from "@chakra-ui/icons";
import { useChatContext } from "@provider/chat-provider";
import { motion, AnimatePresence } from "framer-motion";

const MotionIconButton = motion(IconButton);

const ChatBubble = () => {
  const chatContext = useChatContext();
  if (!chatContext) {
    return null;
  }
  const { setShowChat, showChat } = chatContext;
  const primaryColor = "rgba(0, 150, 136, 0.8)"; // Light blue-green color

  return (
    <AnimatePresence>
      {/* {!showChat && ( */}
      <MotionIconButton
        icon={<ChatIcon />}
        onClick={() => setShowChat(!showChat)} // true nếu là vậy thì bubble sẽ mấy
        position="fixed"
        bottom={5}
        right={5}
        size="lg"
        isRound
        aria-label="Open chat"
        boxShadow="lg"
        bg={primaryColor}
        color="white"
        colorScheme="teal"
        _hover={{
          bg: "rgba(0, 150, 136, 0.9)",
        }}
        _active={{
          bg: "rgba(0, 150, 136, 1)",
        }}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.8 }}
        transition={{
          type: "spring",
          stiffness: 260,
          damping: 20,
        }}
        whileHover={{
          scale: 1.1,
          transition: { duration: 0.2 },
        }}
        whileTap={{
          scale: 0.95,
          transition: { duration: 0.1 },
        }}
      />
    </AnimatePresence>
  );
};

export default ChatBubble;
