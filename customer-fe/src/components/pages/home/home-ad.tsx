import { Grid, GridItem } from "@chakra-ui/react";

const HomeAd = () => {
  return (
    <Grid
      templateColumns="repeat(12, 1fr)"
      templateRows="repeat(2, 1fr)"
      gap={5}
    >
      <GridItem colSpan={9} rowSpan={2}>
        <img
          src="https://cdn.tgdd.vn/2024/06/banner/tuan-le-lenovo-desk-1200x300.png"
          alt=""
        />
      </GridItem>
      <GridItem colSpan={3}>
        <img
          src="https://cdn.tgdd.vn/2024/05/banner/Sticky-Laptop-Gaming-390x97.png"
          alt=""
        />
      </GridItem>
      <GridItem colSpan={3}>
        <img src="https://cdn.tgdd.vn/2023/12/banner/365-390x97.png" alt="" />
      </GridItem>
    </Grid>
  );
};

export { HomeAd };
