import { imageConfig } from "@config/index";
import { ImageProps } from "@type/ui";
import { Image as ChakraImage } from "@chakra-ui/react";

const Image = ({ src, ...props }: ImageProps) => {
  return (
    <ChakraImage
      {...props}
      src={imageConfig[src as keyof typeof imageConfig]?.src || src}
      fallbackSrc="https://png.pngtree.com/png-vector/20201224/ourmid/pngtree-error-404-page-not-found-png-image_2598541.jpg"
    />
  );
};

export { Image };
export type { ImageProps };
