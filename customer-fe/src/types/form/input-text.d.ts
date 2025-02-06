import {  FlexProps, InputProps, TextProps } from '@chakra-ui/react';

type InputTextType = {
	container?: FlexProps;
	label?: TextProps;
	input?: InputProps & {
		label?: string;
		required?: boolean;
		message?: string;
	};
};

export type { InputTextType };
