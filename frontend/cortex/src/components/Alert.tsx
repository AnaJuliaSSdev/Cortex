import React from 'react';
import styles from './css/Alert.module.css'; // Mude o nome do import
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import InfoOutlineIcon from '@mui/icons-material/InfoOutline';
import ErrorIcon from '@mui/icons-material/Error';

// 1. Definimos os "tipos" de alerta que o componente aceita
export type AlertType = 'error' | 'success' | 'warning' | 'info';

interface AlertProps {
  /** A mensagem a ser exibida. */
  message: string;
  /** Função para fechar o alerta. */
  onClose: () => void;
  /** O tipo de alerta, que controla o estilo e o ícone. */
  type?: AlertType;
}

const Alert: React.FC<AlertProps> = ({ message, onClose, type = 'error' }) => {
  if (!message) {
    return null; // Não renderiza nada se não houver mensagem
  }

  // 2. Lógica para escolher o ícone e a classe CSS com base no tipo
  let icon = <WarningAmberIcon/>;
  let styleClass = styles.error; // O padrão é 'error'

  switch (type) {
    case 'success':
      icon = <CheckCircleOutlineIcon color='success'/>;
      styleClass = styles.success;
      break;
    case 'warning':
      icon = <WarningAmberIcon color='warning'/>;
      styleClass = styles.warning;
      break;
    case 'info':
      icon =  <InfoOutlineIcon color='info'/>;
      styleClass = styles.info;
      break;
    case 'error':
    default:
      icon = <ErrorIcon color='error'/>;
      styleClass = styles.error;
      break;
  }

  return (
    // 3. Aplica a classe de estilo base E a classe do tipo (ex: .alertBox .success)
    <div className={`${styles.alertBox} ${styleClass}`}>
      <span className={styles.icon} role="img" aria-label={type}>
        {icon}
      </span>
      
      <p className={styles.message}>{message}</p>
      
      <button 
        className={styles.closeButton} 
        onClick={onClose} 
        title="Fechar"
      >
        &times;
      </button>
    </div>
  );
};

export default Alert;