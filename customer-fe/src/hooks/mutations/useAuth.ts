import { useMutation } from '@tanstack/react-query';

const useLoginMutate = () => {
	return useMutation<ResponseType<>, Error, >({
		mutationFn: async (data) => {
			const request = await http.post('login', data);

			return request.data;
		},
	
	});
};

const useRegisterMutate = () => {
	return useMutation<ResisterType<>, Error, RegisterType>({
		mutationFn: async (data) => {
			const request = http.post('register', data);

			return request;
		},
	});
};


export { useLoginMutate, useRegisterMutate};
