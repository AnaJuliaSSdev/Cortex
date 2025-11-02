import React, { useEffect, useState } from 'react';
import styles from './css/IndexFormModal.module.css';
import { createIndex, updateIndex } from '../services/analysisService';
import type { CreateIndexPayload } from '../interfaces/dto/CreateIndexPayload';
import type { Index } from '../interfaces/Index';
import AlertComponent from './Alert'; // Reutilize seu componente de Alerta
import type { UpdateIndexPayload } from '../interfaces/dto/UpdateIndexPayload';

interface IndexFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  preAnalysisStageId: number;
  indexToEdit?: Index | null; // Opcional: o índice a ser editado
  onIndexAdded: (newIndex: Index) => void;
  onIndexUpdated: (updatedIndex: Index) => void;
}

export default function IndexFormModal({ 
   isOpen, 
    onClose, 
    preAnalysisStageId,
    indexToEdit,
    onIndexAdded,
    onIndexUpdated
}: IndexFormModalProps) {
    
    // Determina o modo (Criar ou Editar)
    const isEditMode = !!indexToEdit;
    
    // Estado do formulário
    const [indexName, setIndexName] = useState('');
    const [indexDescription, setIndexDescription] = useState('');
    const [indicatorName, setIndicatorName] = useState('');
    
    // Estado de UI
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const isFormValid = indexName.trim() !== '' && indicatorName.trim() !== '';

    // 3. Efeito para pré-popular o formulário no modo de edição
    useEffect(() => {
        if (isEditMode && indexToEdit) {
            setIndexName(indexToEdit.name);
            setIndexDescription(indexToEdit.description || '');
            setIndicatorName(indexToEdit.indicator.name);
        } else {
            // Limpa o formulário se estiver em modo de adição
            setIndexName('');
            setIndexDescription('');
            setIndicatorName('');
        }
    }, [indexToEdit, isEditMode, isOpen]); // Roda quando o modal abre ou o índice muda

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!isFormValid) return;

        setIsLoading(true);
        setError(null);

        try { 
            if (isEditMode) {
                // Lógica de ATUALIZAÇÃO
                const payload: UpdateIndexPayload = {
                    indexName: indexName.trim(),
                    indexDescription: indexDescription.trim() || undefined,
                    indicatorName: indicatorName.trim()
                };
                const updatedIndex = await updateIndex(indexToEdit.id, payload);
                onIndexUpdated(updatedIndex); // Chama o callback de atualização
            } else {
                // Lógica de CRIAÇÃO
                const payload: CreateIndexPayload = {
                    preAnalysisStageId: preAnalysisStageId,
                    indexName: indexName.trim(),
                    indexDescription: indexDescription.trim() || undefined,
                    indicatorName: indicatorName.trim()
                };
                const newIndex = await createIndex(payload);
                onIndexAdded(newIndex); // Chama o callback de adição
            }
            onClose(); // Fecha o modal em ambos os casos
        } catch (err) {
            // usar handleApiError aqui
            setError(isEditMode ? "Falha ao atualizar o índice." : "Falha ao criar o índice.");
        } finally {
            setIsLoading(false);
        }
    };

    if (!isOpen) {
        return null;
    }

    return (
        // Overlay
        <div className={styles.overlay}>
            <div className={styles.modal}>
                <form onSubmit={handleSubmit}>
                    <h2 className={styles.title}>
                        {isEditMode ? 'Editar Índice' : 'Adicionar Novo Índice'}
                    </h2>
                    <p className={styles.description}>
                        {isEditMode 
                            ? 'Atualize o nome do índice e seu indicador associado.'
                            : ' Crie um novo indicador e o índice associado. Índices manuais não possuem referências de texto.'
                        }
                    </p>
                    {error && <AlertComponent message={error} type="error" onClose={() => setError(null)} />}

                    <div className={styles.formGroup}>
                        <label htmlFor="indicatorName">Descrição do Indicador *</label>
                        <input
                            id="indicatorName"
                            type="text"
                            value={indicatorName}
                            onChange={(e) => setIndicatorName(e.target.value)}
                            placeholder="Ex: Presença de termos e expressões como 'didática'..."
                            required
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label htmlFor="indexName">Nome do Índice *</label>
                        <input
                            id="indexName"
                            type="text"
                            value={indexName}
                            onChange={(e) => setIndexName(e.target.value)}
                            placeholder="Ex: Saber Pedagógico e Didático"
                            required
                        />
                    </div>

                    <div className={styles.formGroup}>
                        <label htmlFor="indexDescription">Descrição do Índice (Opcional)</label>
                        <textarea
                            id="indexDescription"
                            value={indexDescription}
                            onChange={(e) => setIndexDescription(e.target.value)}
                            placeholder="Ex: Refere-se ao conhecimento sobre o processo de ensino-aprendizagem..."
                        />
                    </div>

                    <footer className={styles.footer}>
                        <button type="button" className={styles.cancelButton} onClick={onClose} disabled={isLoading}>
                            Cancelar
                        </button>
                        <button type="submit" className={styles.submitButton} disabled={!isFormValid || isLoading}>
                            {isLoading ? 'Salvando...' : 'Salvar Índice'}
                        </button>
                    </footer>
                </form>
            </div>
        </div>
    );
}