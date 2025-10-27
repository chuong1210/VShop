import {
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  IconButton,
  MenuDivider,
} from "@chakra-ui/react";
import { ChatIcon } from "@chakra-ui/icons";
import { motion, AnimatePresence } from "framer-motion";
import { useChatContext } from "@provider/chat-provider";
import { useCookies } from "@hook/index";

const MotionIconButton = motion(IconButton);

const ChatBubble = () => {
  const cookies = useCookies();
  const chatContext = useChatContext();
  if (!chatContext) return null;

  const { setShowChat, showChat } = chatContext;
  const primaryColor = "rgba(0, 150, 136, 0.8)";

  const handleAdminChat = () => setShowChat(!showChat);
  const handleBotChat = () => {
    const accessToken = cookies.get("access_token");
    const chatUrl = `http://localhost:5001/?token=${encodeURIComponent(
      accessToken
    )}`;
    window.location.href = chatUrl;
  };

  return (
    <AnimatePresence>
      <Menu>
        <MenuButton
          as={MotionIconButton}
          icon={<ChatIcon />}
          position="fixed"
          bottom={5}
          right={5}
          aria-label="Open chat options"
          boxShadow="lg"
          bg={primaryColor}
          color="white"
          _hover={{ bg: "rgba(0, 150, 136, 0.9)" }}
          _active={{ bg: "rgba(0, 150, 136, 1)" }}
          initial={{ opacity: 0, scale: 0.8 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.8 }}
          {...({
            transition: { type: "spring", stiffness: 260, damping: 20 },
          } as any)}
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.95 }}
        />
        <MenuList minWidth="200px" borderRadius="md" boxShadow="lg">
          <MenuItem icon={<ChatIcon />} onClick={handleAdminChat}>
            Chat với Admin
          </MenuItem>
          <MenuDivider />
          <MenuItem onClick={handleBotChat}>Chat với Bot</MenuItem>
        </MenuList>
      </Menu>
    </AnimatePresence>
  );
};

export default ChatBubble;
