const parse = (object?: any) => {
		if (object) {
			return JSON.parse(object);
		}

	return object;
};


const HTML = (rawHTML?: string) => {
	return { __html: rawHTML  };
};
export { parse,  HTML };
