import { NextRequest, NextResponse } from 'next/server';
import { cookieConfig, FALLBACK_LNG } from './configs';
import { addYears } from 'date-fns';

export const config = {
	matcher: ['/((?!.+\\.[\\w]+$|_next).*)', '/', '/(api|trpc)(.*)'],
};

const middleware = (req: NextRequest, _res: NextResponse): NextResponse => {
	let lng = FALLBACK_LNG;
	const { pathUrl } = req.nextUrl;

		const cookieValue = req.cookies.get(cookieConfig.i18n)?.value;

		if (cookieValue) {
			lng = cookieValue;
		}

	

	const response = NextResponse.next();

	if (!req.cookies.has(cookieConfig.i18n)) {
		response.cookies.set(cookieConfig.i18n, lng, { expires: addYears(new Date(), 1) });
	}

	return response;
};

export default middleware;
