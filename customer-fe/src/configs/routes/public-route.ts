const pubicRoute = {
	'root': '/',
	'product-detail': '/product-detail',
	'category-detail': '/category-detail',
};

const pubicRoutes: string[] = Object.keys(pubicRoute).map((key) => {
	const _key = key as keyof typeof pubicRoute;

	return pubicRoute[_key];
});

export { pubicRoute, pubicRoutes };
