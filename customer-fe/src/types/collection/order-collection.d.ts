
type AddToCartType = {
	productId: number;
	quantity: number;
};

type CartDetailType = {
	details: CartDetailItemType[];
	total: number;
	totalAmount: number;

};


export { AddToCartType, CartDetailType };
