import './css/FormButton.css';

interface TextButton {
    text : string
}

export default function FormButton({text} : TextButton) {

	return (
		<button type="submit" className="login-button">
			{text}
		</button>
	);
}
