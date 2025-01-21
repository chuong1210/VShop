import { NextRequest, NextResponse } from 'next/server';
import { cookieConfig, FALLBACK_LNG, LANGUAGES } from './configs';
import { addYears } from 'date-fns';

export const config = {
  matcher: ['/((?!.+\\.[\\w]+$|_next).*)', '/', '/(api|trpc)(.*)'],
};

const middleware = (req: NextRequest): NextResponse => {
  const { pathname, search } = req.nextUrl;
  
  // Kiểm tra xem pathname có bắt đầu bằng language code không
  const pathnameHasLang = LANGUAGES.some(lang => 
    pathname.startsWith(`/${lang}/`) || pathname === `/${lang}`
  );
  
  // Nếu không có language prefix, thêm language và giữ nguyên search params
  if (!pathnameHasLang) {
    let lng = FALLBACK_LNG;
    
    // Lấy language từ cookie nếu có
    if (req.cookies.has(cookieConfig.i18n)) {
      const cookieValue = req.cookies.get(cookieConfig.i18n)?.value;
      if (cookieValue && LANGUAGES.includes(cookieValue)) {
        lng = cookieValue;
      }
    }

	// const urlPattern = new RegExp(`^/(${LANGUAGES.join('|')})/`);
	// if (!urlPattern.test(pathname)) {
	//   const response = NextResponse.redirect(
	// 	new URL(`/${lng}${pathname}${search}`, req.url)
	//   );
  
    const response = NextResponse.redirect(
      new URL(`/${lng}${pathname}${search}`, req.url)
    );
	

    // Set cookie nếu chưa có
    if (!req.cookies.has(cookieConfig.i18n)) {
      response.cookies.set(cookieConfig.i18n, lng, {
        expires: addYears(new Date(), 1)
      });
    }
	

    return response;
  }

  return NextResponse.next();
};

export default middleware;