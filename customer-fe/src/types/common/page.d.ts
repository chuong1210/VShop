import { supportLanguage } from '@config/i18n';

type PageParamType = {
	lng: keyof typeof supportLanguage;
};

type Props = {
	params: {
		token: string;
	};
  };
type PageType = never;

export type { PageType, PageParamType,Props };
