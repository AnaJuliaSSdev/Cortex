import "./css/RegisterPage.css";
import "../index.css";
import Logo from "../components/Logo.tsx";
import EmailInput from "../components/EmailInput.tsx";
import PasswordInput from "../components/PasswordInput.tsx";
import FormButton from "../components/FormButton.tsx";
import Input from "../components/Input.tsx";
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined';

import { useState } from "react";
import type UserRegisterDto from "../interfaces/dto/UserRegisterDto.ts";
import api from "../services/api.ts";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import type { AlertType } from "../components/Alert.tsx";
import Alert from "../components/Alert.tsx";
import { handleApiError, type ApiErrorMap } from "../utils/errorUtils.ts";
import MiniLoader from "../components/MiniLoader.tsx";

const registerErrorMap: ApiErrorMap = {
	byStatusCode: {
		400: "Os dados enviados estão em formato incorreto.",
		409: "E-mail já cadastrado.",
		500: "Ocorreu um erro inesperado. Por favor, tente novamente."
	},
	// Mensagem padrão para qualquer outro erro
	default: "Não foi possível conectar ao servidor. Tente novamente em alguns instantes."
};

export default function RegisterPage() {
	const [fullName, setFullName] = useState("");
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");
	const [alertInfo, setAlertInfo] = useState<{ message: string; type: AlertType } | null>(null);
	const [error, setError] = useState<string | null>(null);
	const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(false);
	const isPasswordValid = password.length >= 6;

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		if (!isPasswordValid) {
            setError("A senha precisa ter no mínimo 6 caracteres.");
            return;
        }
		setAlertInfo(null); // Limpa alertas anteriores
		setError(null);
		setIsLoading(true);
		

		const userDTO: UserRegisterDto = {
			fullName: fullName,
			email: email,
			password: password,
		};

		try {
			await api.post("/users/register", userDTO);

			setAlertInfo({ message: "Cadastro realizado com sucesso! Você será redirecionado para o login.", type: "success" });

			navigate("/login");
		} catch (err) {
			console.error("Erro ao cadastrar usuário:", error);

			if (axios.isAxiosError(err) && err.response) {				
				 const friendlyMessage = handleApiError(err, registerErrorMap);
				 setError(friendlyMessage);
			} else {
				setError(registerErrorMap.default);
			}
		} finally {
			setIsLoading(false);
		}
	};

	return (
		<div id="register-body">
			<div className="container">
				<Logo />
				<div className="register-card">
					<h2 className="register-title">Cadastrar</h2>

					<form onSubmit={handleSubmit}>
						{alertInfo && (
							<Alert
								message={alertInfo.message}
								type={alertInfo.type}
								onClose={() => setAlertInfo(null)}
							/>
						)}
						<Input
							value={fullName}
							type={"text"}
							icon={<PersonOutlineOutlinedIcon/>}
							placeholder={"Nome completo"}
							id={"input-name"}
							onChange={(e) => setFullName(e.target.value)}
						/>

						<EmailInput value={email} onChange={(e) => setEmail(e.target.value)} />

						<PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} />

						{error && (
                            <div className="login-error-message">
                                {error}
                            </div>
						)}
						<FormButton disabled={isLoading} text="Cadastrar">
							{isLoading ? <MiniLoader /> : "Cadastrar"}
						</FormButton>

						<div className="login-link">
							<a href="/login">Login</a>
						</div>
					</form>
				</div>
			</div>
		</div>
	);
}
