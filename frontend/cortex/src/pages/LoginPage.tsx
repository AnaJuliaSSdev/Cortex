import "./css/LoginPage.css";
import "../index.css";
import Logo from "../components/Logo.tsx";
import EmailInput from "../components/EmailInput.tsx";
import PasswordInput from "../components/PasswordInput.tsx";
import FormButton from "../components/FormButton.tsx";

import { useState } from "react";
import type UserLoginDto from "../interfaces/dto/UserLoginDto.ts";
import api from "../services/api.ts";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { useAuth } from "../contexts/AuthContext.tsx";
import { handleApiError, type ApiErrorMap } from "../utils/errorUtils.ts";

const loginErrorMap: ApiErrorMap = {
    byStatusCode: {
        401: "E-mail ou senha inválidos. Verifique seus dados e tente novamente.",
        404: "E-mail ou senha inválidos. Verifique seus dados e tente novamente.",
        400: "Os dados enviados estão em formato incorreto.",
        500: "Não foi possível conectar ao servidor. Tente novamente em alguns instantes."
    },
    // Mensagem padrão para qualquer outro erro
    default: "Ocorreu um erro inesperado. Por favor, tente novamente."
};

export default function LoginPage() {
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");
	const navigate = useNavigate();
	const auth = useAuth();
	const [error, setError] = useState("");

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setError("");

		const userDTO: UserLoginDto = {
			email: email,
			password: password,
		};

		try {
			const response = await api.post("/users/login", userDTO);
			if (response.data?.token) {
				auth.login(response.data.token);

				navigate("/");
			} else {
				throw new Error("Resposta de login inválida do servidor.");
			}
		} catch (error) {
			if (axios.isAxiosError(error) && error.response) {
				const friendlyMessage = handleApiError(error, loginErrorMap);
            	setError(friendlyMessage);
			} else {
				setError(loginErrorMap.default);
			}
		}
	};

	return (
		<div id="login-body">
			<div className="container">
				<Logo />
				<div className="login-card">
					<h2 className="login-title">Login</h2>

					<form onSubmit={handleSubmit}>
						<EmailInput value={email} onChange={(e) => setEmail(e.target.value)} />

						<PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} />
						{error && (
                            <div className="login-error-message">
                                {error}
                            </div>
                        )}
						<FormButton text="Entrar"></FormButton>

						<div className="register-link">
							<a href="/register">Cadastre-se</a>
						</div>
					</form>
				</div>
			</div>
		</div>
	);
}
