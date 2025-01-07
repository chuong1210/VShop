
import { configureStore} from '@reduxjs/toolkit';

const reducer = {

};

export const reduxStore = configureStore({
	reducer,
	middleware: (getDefaultMiddleware) => {

	},
});
