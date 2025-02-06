import { apiConfig, queryKeyConfig } from '@config/index';
import { UseQueryOptions } from '@tanstack/react-query';
import { KeyValueType } from '@type/common';

type CustomUseQueryOptions = Omit<UseQueryOptions, ''>

type UseGetType<ItemType> = {
	api: keyof typeof apiConfig;
	enable?: boolean;
	filter?: KeyValueType<null | string | undefined | number | boolean>;
	queryKey?: keyof (typeof queryKeyConfig)[keyof typeof apiConfig];
	checkLogin?: boolean;
};

export type { UseGetType };
