"use client";

import { ChakraProvider, ReactQueryProvider } from "@provider/index";
import { GoogleOAuthProvider } from "@react-oauth/google";
import { TokenExpiresModal } from "@root/src/components/modal";
import { PageType } from "@type/common";
import { dir } from "i18next";
// eslint-disable-next-line @next/next/no-document-import-in-page
import { Bounce, ToastContainer } from "react-toastify";
const AppLayout = ({ children, params: { lng } }: PageType) => {
  const clientIdGG = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? "";

  return (
    <html lang={lng} dir={dir(lng)}>
      <body>
        <GoogleOAuthProvider clientId={clientIdGG}>
          <ReactQueryProvider>
            <ChakraProvider>
              {children}

              <TokenExpiresModal />

              <ToastContainer
                position="top-right"
                autoClose={2500}
                hideProgressBar={false}
                newestOnTop={false}
                closeOnClick={true}
                rtl={false}
                pauseOnFocusLoss={true}
                draggable={true}
                pauseOnHover={true}
                transition={Bounce}
                limit={1}
              />
            </ChakraProvider>
          </ReactQueryProvider>
        </GoogleOAuthProvider>
      </body>
    </html>
  );
};

export default AppLayout;
