"use client";

import AuthSvg from "@asset/svg/svg-auth.svg";
import { Box, Button, Flex, Text, VStack, Icon } from "@chakra-ui/react";
import { Link, Loading } from "@component/ui";
import { useRouter, useTranslation } from "@hook/index";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { CheckCircle, XCircle } from "lucide-react";
import { useVerifyEmailMutate } from "@hook/mutations";

const VerifyEmailPage = () => {
  const { t } = useTranslation();
  const router = useRouter();
  const searchParams = useSearchParams();
  const verifyEmailMutate = useVerifyEmailMutate();
  const [verificationStatus, setVerificationStatus] = useState<
    "pending" | "success" | "error"
  >("pending");

  const [hasVerified, setHasVerified] = useState<boolean>(false); // Thêm state kiểm soát

  const token = searchParams.get("token");
  // const token1 = token ? decodeURIComponent(token) : null;
  // const encodedToken = token ? encodeURIComponent(token) : null;

  console.log("Token:", token);

  // Kiểm tra token nếu không có thì dừng
  useEffect(() => {
    if (!token) {
      toast.error(t("auth:invalid_verification_link"));
      setVerificationStatus("error");
      return;
    }
    // Chỉ gọi mutate khi chưa gọi lần nào
    if (!hasVerified) {
      setHasVerified(true);
      verifyEmailMutate.mutate(
        { token },
        {
          onSuccess: () => {
            setVerificationStatus("success");
            toast.success(t("auth:email_verified_success"));
            setTimeout(() => {
              router.push("login");
            }, 3000);
          },
          onError: (error) => {
            setVerificationStatus("error");
            toast.error(error.message || t("auth:email_verification_failed"));
          },
        }
      );
      {
        setVerificationStatus("error");
        toast.error(t("auth:invalid_verification_link"));
      }
    }
  }, [token, t, hasVerified, verifyEmailMutate, router]); // Chỉ chạy khi token hoặc hasVerified thay đổi

  return (
    <Flex flexWrap="wrap" backgroundColor="white" minHeight="100vh">
      <Loading show={verifyEmailMutate.isPending} />

      <Box flex={1}>
        <Flex
          px={8}
          py={16}
          gap={6}
          height="100%"
          flexDirection="column"
          justifyContent="center"
          alignItems="center"
          display={{
            base: "none",
            lg: "flex",
          }}
        >
          <Link mb={5} href="home">
            <Text fontSize="4xl" fontWeight="bold">
              {t("app:name")}
            </Text>
          </Link>

          <Text fontWeight="medium" textAlign="center">
            {t("auth:verify_email_description")}
          </Text>

          <Box mt={12}>
            <AuthSvg />
          </Box>
        </Flex>
      </Box>

      <Box flex={1} borderLeft="1px" borderLeftColor="gray.200">
        <Flex
          height="100%"
          flexDirection="column"
          justify="center"
          alignItems="center"
          px={10}
        >
          <VStack spacing={6} align="center">
            {verificationStatus === "success" ? (
              <>
                <Icon as={CheckCircle} w={16} h={16} color="green.500" />
                <Text fontSize="2xl" fontWeight="bold" color="green.500">
                  {t("auth:email_verified_success")}
                </Text>
                <Text color="gray.600">{t("auth:redirecting_to_login")}</Text>
              </>
            ) : verificationStatus === "error" ? (
              <>
                <Icon as={XCircle} w={16} h={16} color="red.500" />
                <Text fontSize="2xl" fontWeight="bold" color="red.500">
                  {t("auth:email_verification_failed")}
                </Text>
                <Button
                  colorScheme="green"
                  onClick={() => router.push("login")}
                >
                  {t("auth:back_to_login")}
                </Button>
              </>
            ) : (
              <Text fontSize="xl" color="gray.600">
                {t("auth:verifying_email")}
              </Text>
            )}
          </VStack>
        </Flex>
      </Box>
    </Flex>
  );
};

export default VerifyEmailPage;
