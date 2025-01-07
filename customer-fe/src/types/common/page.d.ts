import { supportLanguage } from '@config/i18n';

type PageParamType = {
	lng: keyof typeof supportLanguage;
};

type PageType = never;

export type { PageType, PageParamType };
