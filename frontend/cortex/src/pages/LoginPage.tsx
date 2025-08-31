import "./css/LoginPage.css";
import "../index.css";
import Logo from "../components/Logo.tsx";
import EmailInput from "../components/EmailInput.tsx";
import PasswordInput from "../components/PasswordInput.tsx";
import FormButton from "../components/FormButton.tsx";

import { useState } from "react";

export default function LoginPage() {
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");

	const handleSubmit = (e: React.FormEvent) => {
		e.preventDefault();
		console.log("Tentando logar com:", { email, password });
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

						<FormButton text="Entrar"></FormButton>

						<div className="forgot-password">
							<a href="#">Esqueceu a senha?</a>
						</div>
					</form>
				</div>
			</div>
		</div>
	);
}
