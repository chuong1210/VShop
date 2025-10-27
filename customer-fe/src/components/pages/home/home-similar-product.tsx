import { Box, Text, SimpleGrid } from "@chakra-ui/react";
import { Product } from "@component/ui";
import { useTranslation } from "@hook/index";
import { useGet } from "@hook/queries";
import { skeletons } from "@lib/util";
import { ProductCollectionType } from "@type/collection";

interface HomeSimilarProductsProps {
  productId: number;
}

const HomeSimilarProducts: React.FC<HomeSimilarProductsProps> = ({
  productId,
}) => {
  const { t } = useTranslation();

  const similarQuery = useGet<ProductCollectionType[]>({
    api: "recommendations-similar",
    filter: {
      productId: productId, // Pass the productId as query param
      NumRecs: 8,
      Page: 1,
      PageSize: 8,
      IsAllDetail: true,
    },
  });

  if (similarQuery.isLoading) {
    return (
      <SimpleGrid columns={{ base: 1, md: 2, lg: 4 }} spacing={5}>
        {skeletons<ProductCollectionType>(4).map((_, index) => (
          <Box key={index} height="300px" bg="gray.100" borderRadius="md" />
        ))}
      </SimpleGrid>
    );
  }

  return (
    <Box p={4} mt={8}>
      <Text textAlign="center" mb={6} fontWeight="bold" fontSize="xl">
        {t("common:similar_products") || "Sản phẩm tương tự"}
      </Text>
      <SimpleGrid columns={{ base: 2, md: 3, lg: 4 }} spacing={5}>
        {(similarQuery.data?.data || []).map((product) => (
          <Box key={product.id}>
            <Product data={product} />
          </Box>
        ))}
      </SimpleGrid>
    </Box>
  );
};

export { HomeSimilarProducts };
