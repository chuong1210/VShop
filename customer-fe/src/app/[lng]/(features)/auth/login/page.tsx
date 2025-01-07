'use client';

import { Box, Button, Flex } from "@chakra-ui/react";
import { useRouter } from "next/router";
import { Controller, useForm } from 'react-hook-form';
import { Loading } from '@component/ui';
import Link from "next/link";
import AuthSvg from '@asset/svg/svg-auth.svg';
import { InputText } from '@component/form';



const Login = () => {
	const router = useRouter();



	const onSubmit = (value: LoginType) => {

		});
	};

	return (
		<Flex
			flexWrap='wrap'
			backgroundColor='white'
			minHeight='100vh'
		>

			<Box flex={1}>
				<Flex
					px={9}
					py={17}
					gap={9}
					flexDirection='row'
					justifyContent='center'
					alignItems='center'
					height='100%'
					display={{
						base: 'none',
						lg: 'flex',
					}}
				>
					<Link
						href='home'
					>
						<Image
							src='/assets/images/logo.svg'
							width={200}
							height={60}
						/>
					</Link>

				

					<Box mt={12}>
						<AuthSvg />
					</Box>
				</Flex>
			</Box>

		
						<Controller
							name='password'
							render={({ field: { name, value, onChange }, fieldState: { error } }) => (
								<InputText
									input={{
										name,
										value,
										onChange,
										type: 'password',
										label: t('auth:password'),
										placeholder: t('auth:password'),
									}}
								/>
							)}
						/>

						<Button
							colorScheme='green'
						>
							Login
						</Button>

						<Box mt={4}>
							<div>
								<Link
									href='register'
									color='green'
									display='block'
								>
							Đăng ký
								</Link>
							</div>
						</Box>
					</Flex>
				</Flex>
			</Box>
		</Flex>
	);
};

export default Login;
