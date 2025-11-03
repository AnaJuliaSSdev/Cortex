import { useState, useEffect } from 'react';
import styles from './css/ConfirmModal.module.css'; // Reutilizaremos o mesmo CSS
import alertStyles from './css/Alert.module.css'; // Reutilizaremos o CSS do Alert
import ReportProblemIcon from '@mui/icons-material/ReportProblem';



interface DeleteAnalysisModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  analysisName: string; // O nome que o usuário deve digitar
  isConfirming?: boolean;
}

export default function DeleteAnalysisModal({
  isOpen,
  onClose,
  onConfirm,
  analysisName,
  isConfirming = false
}: DeleteAnalysisModalProps) {
  
  const [confirmationText, setConfirmationText] = useState("");

  // Limpa o input sempre que o modal for aberto
  useEffect(() => {
    if (isOpen) {
      setConfirmationText("");
    }
  }, [isOpen]);

  if (!isOpen) return null;

  // O botão de exclusão só é habilitado se o texto for idêntico
  const isMatch = confirmationText === analysisName;

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <h2 className={styles.title}>Excluir esta análise?</h2>
        
        {/* Mensagem de aviso forte */}
        <div className={`${alertStyles.alertBox} ${alertStyles.error}`}>
          <span className={alertStyles.icon}><ReportProblemIcon/></span>
          <p className={alertStyles.message}>
            <strong>Ação irreversível.</strong> Todos os dados, incluindo documentos,
            índices e categorias, serão excluídos permanentemente.
          </p>
        </div>
        
        <div className={styles.message}>
          Para confirmar, digite o nome da análise:
          <br />
          <strong>{analysisName}</strong>
        </div>

        {/* O input de confirmação */}
        <input
          type="text"
          value={confirmationText}
          onChange={(e) => setConfirmationText(e.target.value)}
          className={styles.confirmationInput} // Adicionaremos este estilo
          autoFocus
        />

        <footer className={styles.footer}>
          <button 
            type="button" 
            className={styles.cancelButton} 
            onClick={onClose} 
            disabled={isConfirming}
          >
            Cancelar
          </button>
          <button 
            type="button" 
            className={styles.confirmButton} // O botão de confirmação (vermelho)
            onClick={onConfirm}
            disabled={!isMatch || isConfirming} // Desabilitado se o texto não bater
          >
            {isConfirming ? 'Excluindo...' : 'Excluir esta análise'}
          </button>
        </footer>
      </div>
    </div>
  );
}