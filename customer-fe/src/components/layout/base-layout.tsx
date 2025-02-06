import { ReactNode } from "react";
import { Footer, Header } from ".";
import { Box } from "@chakra-ui/react";
import { MAX_WIDTH } from "@config/index";
import { ChatProvider } from "@provider/chat-provider";
import ChatBubble from "@component/form/chat-bubble";
import ChatBox from "@component/pages/home/home-chat";

const BaseLayout = ({ children }: { children: ReactNode }) => {
  return (
    <ChatProvider>
      <Box background="gray.50">
        <ChatBubble />
        <ChatBox />
        <Header />
        <Box minH="100vh" maxW={MAX_WIDTH} mx="auto">
          {children}
        </Box>

        <Footer />
      </Box>
    </ChatProvider>
  );
};

export { BaseLayout };
