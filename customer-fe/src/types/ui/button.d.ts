import { ButtonProps as ChakraButtonProps } from '@chakra-ui/react';

type ButtonProps = ChakraButtonProps & {
	checkLogin?: boolean;
};
interface GoogleLoginButtonProps {
	onClick: (t: any) => void;
	isLoading?: boolean;
  }
  
export type { ButtonProps ,GoogleLoginButtonProps};
