import "./css/RegisterPage.css";
import "../index.css";
import Logo from "../components/Logo.tsx";
import EmailInput from "../components/EmailInput.tsx";
import PasswordInput from "../components/PasswordInput.tsx";
import FormButton from "../components/FormButton.tsx";
import Input from "../components/Input.tsx";

import { useState } from "react";
import type UserRegisterDto from "../interfaces/dto/UserRegisterDto.ts";
import api from "../services/api.ts";
import { useNavigate } from "react-router-dom";
import axios from "axios";

export default function RegisterPage() {
	const [fullName, setFullName] = useState("");
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");
	const navigate = useNavigate();

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		const userDTO: UserRegisterDto = {
			fullName: fullName,
			email: email,
			password: password,
		};

		try {
			await api.post("/users/register", userDTO);

			alert("Cadastro realizado com sucesso! Você será redirecionado para o login.");

			navigate("/login");
		} catch (error) {
			console.error("Erro ao cadastrar usuário:", error);

			if (axios.isAxiosError(error) && error.response) {
				const errorMessage =
					error.response.data?.message ||
					error.response.data?.error ||
					`Erro ${error.response.status}`;
				alert(`Falha no cadastro: ${errorMessage}`);
			} else {
				alert("Ocorreu um erro inesperado. Tente novamente.");
			}
		}
	};

	return (
		<div id="register-body">
			<div className="container">
				<Logo />
				<div className="register-card">
					<h2 className="register-title">Cadastrar</h2>

					<form onSubmit={handleSubmit}>
						<Input
							value={fullName}
							type={"text"}
							iconName={"person.svg"}
							placeholder={"Nome completo"}
							id={"input-name"}
							onChange={(e) => setFullName(e.target.value)}
						/>

						<EmailInput value={email} onChange={(e) => setEmail(e.target.value)} />

						<PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} />

						<FormButton text="Cadastrar"></FormButton>

						<div className="login-link">
							<a href="/login">Login</a>
						</div>
					</form>
				</div>
			</div>
		</div>
	);
}
