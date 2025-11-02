import React from "react";
import "./css/EmailInput.css";
import MailOutlineIcon from '@mui/icons-material/MailOutline';

interface EmailInputProps {
	value: string;
	onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export default function EmailInput({ value, onChange }: Readonly<EmailInputProps>) {
	return (
		<div className="form-group">
			<div className="email-input-wrapper">
				<input
					type="email"
					className="form-input"
					placeholder="Email"
					id="email"
					value={value}
					onChange={onChange}
					required
				/>
				<span className="email-span">
						<MailOutlineIcon/>
				</span>
			</div>
		</div>
	);
}
