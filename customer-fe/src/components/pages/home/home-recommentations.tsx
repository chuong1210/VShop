// components/pages/home/HomeRecommendations.tsx
import { Box, Grid, GridItem, Text, Badge, Flex } from "@chakra-ui/react";
import { Product } from "@component/ui";
import { useTranslation } from "@hook/index";
import { useGet } from "@hook/queries";
import { skeletons } from "@lib/util";
import { ProductCollectionType } from "@type/collection";
import Slider from "react-slick";
import { useState } from "react";

const HomeRecommendations = () => {
  const { t } = useTranslation();
  const [currentPage, setCurrentPage] = useState(0);

  const recommendationsQuery = useGet<ProductCollectionType[]>({
    api: "recommendations-collaborative", // Or "recommendations-hybrid", "recommendations-trending"
    filter: {
      NumRecs: 10, // Number of recommendations
      Page: currentPage + 1,
      PageSize: 6,
      IsAllDetail: true,
    },
  });

  return (
    <Box p={4} mt={8}>
      <Flex justify="space-between" align="center" mb={6}>
        <Text textAlign="center" fontWeight="bold" fontSize="2xl" flex={1}>
          {t("common:recommended_products") || "Sản phẩm gợi ý cho bạn"}
        </Text>
        <Badge colorScheme="teal" variant="subtle">
          Dựa trên lịch sử mua sắm
        </Badge>
      </Flex>

      <Box mx="-1rem">
        {recommendationsQuery.data?.data &&
        recommendationsQuery.data?.data.length > 5 ? (
          <Slider
            slidesToShow={5}
            slidesToScroll={5}
            dots={true}
            responsive={[
              {
                breakpoint: 1024,
                settings: { slidesToShow: 3, slidesToScroll: 3 },
              },
              {
                breakpoint: 768,
                settings: { slidesToShow: 2, slidesToScroll: 2 },
              },
              {
                breakpoint: 480,
                settings: { slidesToShow: 1, slidesToScroll: 1 },
              },
            ]}
          >
            {(
              recommendationsQuery.data?.data ||
              skeletons<ProductCollectionType>(10)
            ).map((product) => (
              <Box key={product.id} px={4} pb={1}>
                <Product data={product} />
              </Box>
            ))}
          </Slider>
        ) : (
          <Grid
            templateColumns={{
              base: "repeat(1, 1fr)",
              md: "repeat(3, 1fr)",
              lg: "repeat(5, 1fr)",
            }}
            gap={5}
          >
            {(
              recommendationsQuery.data?.data ||
              skeletons<ProductCollectionType>(5)
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

export { HomeRecommendations };
