import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext'; // Importe seu hook de autenticação
import Logo from './Logo';
import styles from './css/Header.module.css'; // Criaremos este CSS
import LogoutIcon from '@mui/icons-material/Logout'; // Ícone de "Sair"

export default function Header() {
    const auth = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        auth.logout(); // 1. Limpa o token do localStorage
        navigate('/login'); // 2. Redireciona para o login
    };

    return (
        <header className={styles.header}>
            {/* O Logo agora será clicável (ver Passo 2) */}
            <Logo />
            
            {/* O novo botão de logout */}
            <button onClick={handleLogout} className={styles.logoutButton}>
                <LogoutIcon style={{ fontSize: '1.1rem' }} />
                Sair
            </button>
        </header>
    );
}