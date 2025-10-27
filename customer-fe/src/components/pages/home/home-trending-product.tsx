// components/pages/home/HomeTrendingProducts.tsx
// A component for trending products, similar to hot sell but using trending API

import { Box, Grid, GridItem, Text } from "@chakra-ui/react";
import { Product } from "@component/ui";
import { useTranslation } from "@hook/index";
import { useGet } from "@hook/queries";
import { skeletons } from "@lib/util";
import { ProductCollectionType } from "@type/collection";
import Slider from "react-slick";

const HomeTrendingProducts = () => {
  const { t } = useTranslation();

  const trendingQuery = useGet<ProductCollectionType[]>({
    api: "recommendations-trending",
    filter: {
      NumRecs: 10,
      Page: 1,
      PageSize: 6,
      IsAllDetail: true,
    },
  });

  return (
    <Box p={4} mt={8}>
      <Text textAlign="center" mb={8} fontWeight="bold" fontSize="2xl">
        {t("common:trending_products") || "Sản phẩm đang hot"}
      </Text>

      <Box mx="-1rem">
        {trendingQuery.data?.data && trendingQuery.data?.data.length > 5 ? (
          <Slider slidesToShow={5} slidesToScroll={5} dots={true}>
            {(
              trendingQuery.data?.data || skeletons<ProductCollectionType>(10)
            ).map((product) => (
              <Box key={product.id} px={4} pb={1}>
                <Product data={product} />
              </Box>
            ))}
          </Slider>
        ) : (
          <Grid templateColumns="repeat(5, 1fr)" gap={5}>
            {(
              trendingQuery.data?.data || skeletons<ProductCollectionType>(5)
            ).map((product) => (
              <GridItem key={product.id}>
                <Product data={product} />
              </GridItem>
            ))}
          </Grid>
        )}
      </Box>
    </Box>
  );
};

export { HomeTrendingProducts };
