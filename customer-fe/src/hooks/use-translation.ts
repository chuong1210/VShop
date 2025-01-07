import { DEFAULT_NS, FALLBACK_LNG, supportLanguage } from '@config/index';
import { PageParamType } from '@type/common';
import { createInstance } from 'i18next';
import { useParams } from 'next/navigation';
import { initReactI18next } from 'react-i18next/initReactI18next';

import enCommon from '../configs/i18n/locales/en/common.json';
import enApp from '../configs/i18n/locales/en/app.json';
import enAuth from '../configs/i18n/locales/en/auth.json';
import enMenu from '../configs/i18n/locales/en/menu.json';
import enRequest from '../configs/i18n/locales/en/request.json';
import enValidation from '../configs/i18n/locales/en/validation.json';

import viCommon from '../configs/i18n/locales/vi/common.json';
import viApp from '../configs/i18n/locales/vi/app.json';
import viAuth from '../configs/i18n/locales/vi/auth.json';
import viMenu from '../configs/i18n/locales/vi/menu.json';
import viRequest from '../configs/i18n/locales/vi/request.json';
import viValidation from '../configs/i18n/locales/vi/validation.json';

const useTranslation = (language?: keyof typeof supportLanguage, options: any = {}) => {
	const { lng } = useParams<PageParamType>();
	const i18nextInstance = createInstance();

	i18nextInstance.use(initReactI18next).init({
		lng: language || lng,
		ns: DEFAULT_NS,
		defaultNS: DEFAULT_NS,
		fallbackLng: FALLBACK_LNG,
		resources: {
			en: {
				common: enCommon,
				app: enApp,
				auth: enAuth,
				menu: enMenu,
				request: enRequest,
				validation: enValidation,
			},
			vi: {
				common: viCommon,
				app: viApp,
				auth: viAuth,
				menu: viMenu,
				request: viRequest,
				validation: viValidation,
			},
		},
	});

	return {
		t: i18nextInstance.getFixedT(language || lng, DEFAULT_NS, options.keyPrefix),
		i18n: i18nextInstance,
		lng,
	};
};

export { useTranslation };
