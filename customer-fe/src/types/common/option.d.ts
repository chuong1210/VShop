import { iconConfig, routeConfig } from '@config/icon-config';

type OptionType<CodeType = string> = {
	code: CodeType;
	label?: string;
	subLabel?: string;
	shouldShow?: boolean;
	disable?: boolean;
	badge?: number;
	divide?: boolean;
	icon?: keyof typeof iconConfig;
	options?: OptionType[];
	href?: keyof typeof routeConfig;
};

export type { OptionType };
