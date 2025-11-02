import React from 'react';
import styles from './css/ConfirmModal.module.css';

interface ConfirmModalProps {
  isOpen: boolean;
  title: string;
  message: React.ReactNode;
  confirmText?: string;
  cancelText?: string;
  onClose: () => void;
  onConfirm: () => void;
  isConfirming?: boolean; // Para mostrar um estado de loading
}

export default function ConfirmModal({
  isOpen,
  title,
  message,
  confirmText = "Confirmar",
  cancelText = "Cancelar",
  onClose,
  onConfirm,
  isConfirming = false
}: ConfirmModalProps) {
  
  if (!isOpen) return null;

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <h2 className={styles.title}>{title}</h2>
        <div className={styles.message}>{message}</div>
        <footer className={styles.footer}>
          <button 
            type="button" 
            className={styles.cancelButton} 
            onClick={onClose} 
            disabled={isConfirming}
          >
            {cancelText}
          </button>
          <button 
            type="button" 
            className={styles.confirmButton} 
            onClick={onConfirm}
            disabled={isConfirming}
          >
            {isConfirming ? 'Excluindo...' : confirmText}
          </button>
        </footer>
      </div>
    </div>
  );
}