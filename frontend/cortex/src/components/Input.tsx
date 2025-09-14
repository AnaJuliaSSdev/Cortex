import "./css/Input.css";

interface PasswordInputProps {
	value: string;
	type: string;
	iconName: string;
	placeholder: string;
	id: string;
	onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export default function Input({
	value,
	onChange,
	type,
	iconName,
	placeholder,
	id,
}: Readonly<PasswordInputProps>) {
	function getImageUrl(name: string) {
		return new URL(`../assets/${name}`, import.meta.url).href;
	}

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
					<img className="input-icon" src={getImageUrl(iconName)} alt="Icone acessório" />
				</span>
			</div>
		</div>
	);
}
