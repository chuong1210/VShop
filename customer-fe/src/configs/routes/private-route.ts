const privateRoute = {
	'profile': '/profile',

};

const privateRoutes: string[] = Object.keys(privateRoute).map((key) => {
	const _key = key as keyof typeof privateRoute;

	return privateRoute[_key];
});

export { privateRoute, privateRoutes };
