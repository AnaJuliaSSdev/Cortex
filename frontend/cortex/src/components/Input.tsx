import "./css/Input.css";

interface PasswordInputProps {
	value: string;
	type: string;
	icon: any;
	placeholder: string;
	id: string;
	onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export default function Input({
	value,
	onChange,
	type,
	icon,
	placeholder,
	id,
}: Readonly<PasswordInputProps>) {
	return (
		<div className="form-group">
			<div className="input-wrapper">
				<input
					type={type}
					className="form-input"
					placeholder={placeholder}
					id={id}
					value={value}
					onChange={onChange}
					required
				/>
				<span className="input-icon-span">
					{icon}
				</span>
			</div>
		</div>
	);
}
