import { cookies, http } from '@lib/index';
import { useMutation } from '@tanstack/react-query';
import {  LoginResponse, LoginType, RegisterType, ResponseType,VerifyEmailType } from '@type/common';

const useLoginMutate = () => {
	return useMutation<ResponseType<LoginResponse>, Error, LoginType>({
		mutationFn: async (data) => {
			const request = await http.post('login', data);

			return request.data;
		},
		onSuccess(response) {
			const expires = new Date(response.data.exp);

			cookies.set('access_token', response.data.token, { expires });
			cookies.set('user_id', response.data.id, { expires });

			cookies.set('expires_at', expires.getTime(), { expires });
			cookies.set('is_login', true, { expires });
		},
	});
};

const useRegisterMutate = () => {
	return useMutation<any, Error, RegisterType>({
		mutationFn: async (data) => {
			const request = http.post('register', data);

			return request;
		},
	});
};


const useGoogleAuthMutate = () => {
	return useMutation<any, Error, string>({
		mutationFn: async (data) => {
			const request = await http.post('google', data
			);
			return request;
		},
		onSuccess(response) {
			const loginData = response.data;
			const expires = new Date(loginData.exp);

			cookies.set('access_token', loginData.token, { expires });
			cookies.set('expires_at', expires.getTime(), { expires });
			cookies.set('is_login', true, { expires });
		},
	});
};
 const useVerifyEmailMutate = () => {
  return useMutation<any, Error, VerifyEmailType>({
    mutationFn: async (data) => {
      const request = await http.post("verify-email", data)
      return request.data
    },
	
  })
}




export { useLoginMutate, useRegisterMutate ,useVerifyEmailMutate,useGoogleAuthMutate};
