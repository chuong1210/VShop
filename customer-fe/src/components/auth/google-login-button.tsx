"use client";
import { Button } from "@chakra-ui/react";
import { GoogleLogin } from "@react-oauth/google";
import { GoogleLoginButtonProps } from "@type/ui";
import { FcGoogle } from "react-icons/fc";
import { useRef, useEffect } from "react";

export const GoogleLoginButton = ({
  onClick,
  isLoading,
}: GoogleLoginButtonProps) => {
  const googleLoginRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (googleLoginRef.current) {
      const googleButton =
        googleLoginRef.current.querySelector('div[role="button"]');
      if (googleButton) {
        (googleButton as HTMLElement).style.display = "none";
      }
    }
  }, []);

  return (
    <Button
      w="full"
      variant="outline"
      leftIcon={<FcGoogle />}
      isLoading={isLoading}
      onClick={() => {
        const googleButton = googleLoginRef.current?.querySelector(
          'div[role="button"]'
        ) as HTMLElement;
        googleButton?.click();
      }}
    >
      Continue with Google
      <div ref={googleLoginRef} style={{ position: "absolute", opacity: 0 }}>
        <GoogleLogin onSuccess={onClick} size="large" />
      </div>
    </Button>
  );
};
