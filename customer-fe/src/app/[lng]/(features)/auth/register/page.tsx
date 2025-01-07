"use client";

import AuthSvg from "@asset/svg/svg-auth.svg";
import { Box, Button, Flex } from "@chakra-ui/react";
import { InputText } from "@component/form";
import { InputRadio } from "@component/form/input-radio";
import { Loading } from "@component/ui";
import { useRegisterMutate } from "@hook/mutations";
import { defaultRegisterValues, getRegisterSchema } from "@schema/auth";
import { RegisterType } from "@type/common";
import Link from "next/link";
import { useRouter } from "next/router";
import { Controller, useForm } from "react-hook-form";
import acceptLanguage from "accept-language";

const RegisterPage = () => {
  const router = useRouter();

  const registerMutate = useRegisterMutate();

  const { control, handleSubmit } = useForm({});

  const onSubmit = (value: RegisterType) => {
    registerMutate.mutate(value, {
      onSuccess() {
        router.push("login");
      },
      onError(error) {
        console.log("🚀 ~ onError ~ error:", error);
      },
    });
  };

  return (
    <Flex backgroundColor="blackAlpha.100" minHeight="120vh">
      <Loading show={registerMutate.isPending} />

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
          <Text fontWeight="medium" textAlign="center">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit suspendisse.
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
          <Text
            mb={3}
            fontWeight="medium"
            fontSize="lg"
            textColor="gray.500"
          ></Text>

          <Text mb={10} fontSize="3xl" fontWeight="bold"></Text>

          <Flex flexDirection="column" width="100%" gap={5}>
            <Controller
              control={control}
              name="email"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    label: "email",
                    placeholder: "email",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="phoneNumber"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    label: "Phone Number",
                    placeholder: "Phone Number",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="userName"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    label: "Username",
                    placeholder: "Username",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="name"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    label: "Name",
                    placeholder: "Name",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="gender"
              render={({
                field: { value, onChange },
                fieldState: { error },
              }) => (
                <InputRadio
                  options={[
                    { code: "Nam", label: "Male" },
                    { code: "Nữ", label: "Female" },
                  ]}
                  input={{
                    value,
                    onChange,
                    message: error?.message,
                    label: "Gender",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="password"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    type: "password",
                    label: "Password",
                    placeholder: "Password",
                  }}
                />
              )}
            />

            <Controller
              control={control}
              name="confirmPassword"
              render={({
                field: { name, value, onChange },
                fieldState: { error },
              }) => (
                <InputText
                  input={{
                    name,
                    value,
                    onChange,
                    message: error?.message,
                    type: "password",
                    label: "Confirm Password",
                    placeholder: "Confirm Password",
                  }}
                />
              )}
            />

            <Button colorScheme="green" onClick={handleSubmit(onSubmit)}>
              {"Register"}
            </Button>

            <Box mt={4}>
              <Text fontWeight="medium" acceptLanguage="center">
                {"Already have account?"}{" "}
                <Link href="login" color="green" display="block">
                  {"Login here"}
                </Link>
              </Text>
            </Box>
          </Flex>
        </Flex>
      </Box>
    </Flex>
  );
};

export default RegisterPage;
