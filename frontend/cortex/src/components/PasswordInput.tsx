import React, { useState } from 'react';
import './css/PasswordInput.css'; 

import eyeOpenIconUrl from '../assets/eye-open.svg';
import eyeClosedIconUrl from '../assets/eye-closed.svg';

interface PasswordInputProps {
    value: string;
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export default function PasswordInput({ value, onChange }: Readonly<PasswordInputProps>) {
    const [isPasswordVisible, setIsPasswordVisible] = useState(false);

    const togglePasswordVisibility = () => {
        setIsPasswordVisible(!isPasswordVisible);
    };

    return (
        <div className="form-group">
            <div className="password-input-wrapper">
                <input
                    type={isPasswordVisible ? 'text' : 'password'}
                    className="form-input"
                    placeholder="Senha"
                    id="password"
                    value={value}
                    onChange={onChange}
                    required
                />
                <span onClick={togglePasswordVisibility} className="password-toggle-icon">
                    {isPasswordVisible ? (
                        <img className="icon-password" src={eyeClosedIconUrl} alt="Esconder senha" />
                    ) : (
                        <img className="icon-password" src={eyeOpenIconUrl} alt="Mostrar senha" />
                    )}
                </span>
            </div>
        </div>
    );
}