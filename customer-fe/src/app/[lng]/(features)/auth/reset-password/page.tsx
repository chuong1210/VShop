"use client";

import { useState } from "react";
import {
  Box,
  Button,
  Container,
  Flex,
  Heading,
  Input,
  Text,
  useToast,
  VStack,
} from "@chakra-ui/react";
import { motion } from "framer-motion";
import { useSpring, animated } from "react-spring";
import Image from "next/image";

const MotionBox = motion(Box);

export default function ResetPasswordPage() {
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const toast = useToast();

  const fadeIn = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 },
    config: { duration: 1000 },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 2000));
    setIsLoading(false);
    toast({
      title: "Reset link sent",
      description: "Please check your email for the password reset link.",
      status: "success",
      duration: 5000,
      isClosable: true,
    });
  };

  return (
    <Container maxW="container.xl" p={0}>
      <Flex
        h={{ base: "auto", md: "100vh" }}
        py={[0, 10, 20]}
        direction={{ base: "column-reverse", md: "row" }}
      >
        <VStack
          w={{ base: "full", md: "50%" }}
          h="full"
          p={10}
          spacing={10}
          alignItems="flex-start"
          bg="white"
        >
          <MotionBox
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
          >
            <Heading size="2xl">Reset your password</Heading>
          </MotionBox>
          <animated.div style={fadeIn}>
            <Text>
              Enter your email address and we&apos;ll send you a link to reset
              your password.
            </Text>
          </animated.div>
          <form onSubmit={handleSubmit} style={{ width: "100%" }}>
            <VStack spacing={5} align="stretch">
              <Input
                placeholder="Enter your email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                size="lg"
                type="email"
                required
              />
              <Button
                colorScheme="blue"
                size="lg"
                width="full"
                type="submit"
                isLoading={isLoading}
              >
                Send Reset Link
              </Button>
            </VStack>
          </form>
        </VStack>
        <MotionBox
          w={{ base: "full", md: "50%" }}
          h={{ base: "30vh", md: "full" }}
          bg="blue.50"
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5, delay: 0.2 }}
        >
          <Flex h="full" align="center" justify="center">
            <Image
              src="/reset-password-illustration.svg"
              alt="Reset Password Illustration"
              width={400}
              height={400}
            />
          </Flex>
        </MotionBox>
      </Flex>
    </Container>
  );
}
