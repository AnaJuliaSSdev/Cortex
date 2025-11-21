import './css/FormButton.css';

interface FormButtonProps {
    text: string;
    disabled?: boolean;
    children?: React.ReactNode;
}

export default function FormButton({ text, disabled, children }: FormButtonProps) {
    return (
        <button 
            type="submit" 
            className="login-button"
            disabled={disabled} 
        >
            {children ? children : text}
        </button>
    );
}