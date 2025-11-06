import { Link } from 'react-router-dom';
import logoIcon from '../assets/logoIcon.svg';
import './css/Logo.css'

export default function Logo() {
    return (
    <Link to="/" className="logo-section">
        <div className="logo-section">
            <img src={logoIcon} alt="Neurology Icon" className="brain-icon" />
            <h1 className="logo-text">Cortex</h1>
        </div>
    </Link>
        
    );
}