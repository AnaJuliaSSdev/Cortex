import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createAnalysis } from '../services/analysisService';

// Definindo as props que o componente vai receber
interface CreateAnalysisModalProps {
    isOpen: boolean;
    onClose: () => void;
}

// Usando React.FC (Function Component) para tipar o componente com suas props
const CreateAnalysisModal: React.FC<CreateAnalysisModalProps> = ({ isOpen, onClose }) => {
    const [title, setTitle] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!title.trim()) {
            setError('O nome da análise é obrigatório.');
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const newAnalysis = await createAnalysis({ title });
            // Sucesso! Feche o modal e navegue para a próxima página.
            onClose();
            // Vamos criar uma página de detalhes da análise para onde o usuário será levado
            navigate(`/analysis/${newAnalysis.id}`); 
        } catch (err) {
            setError('Ocorreu um erro ao criar a análise. Tente novamente.');
            console.error(err);
        } finally {
            setIsLoading(false);
        }
    };

    if (!isOpen) {
        return null;
    }

    return (
        // Overlay (fundo escuro)
        <div style={{
            position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
            backgroundColor: 'rgba(0, 0, 0, 0.5)', display: 'flex',
            alignItems: 'center', justifyContent: 'center'
        }}>
            {/* Conteúdo do Modal */}
            <div style={{
                background: 'var(--background-light)', padding: '2rem',
                borderRadius: '8px', minWidth: '400px',
                boxShadow: '0 4px 6px rgba(0,0,0,0.1)'
            }}>
                <h2 style={{ color: 'var(--text-dark)', marginTop: 0 }}>Criar Nova Análise</h2>
                <p style={{ color: 'var(--text-light)', marginBottom: '1.5rem' }}>
                    Dê um nome para sua nova análise para começar.
                </p>

                <form onSubmit={handleSubmit}>
                    <input
                        type="text"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="Ex: Análise de Sentimentos de Reviews"
                        disabled={isLoading}
                        style={{
                            width: '100%', padding: '0.75rem', border: '1px solid var(--background-medium)',
                            borderRadius: '4px', boxSizing: 'border-box'
                        }}
                    />
                    {error && <p style={{ color: 'var(--primary)', fontSize: '0.875rem' }}>{error}</p>}

                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem', marginTop: '1.5rem' }}>
                        <button type="button" onClick={onClose} disabled={isLoading} style={{
                            padding: '0.5rem 1rem', background: 'transparent', border: 'none',
                            color: 'var(--text-light)', cursor: 'pointer'
                        }}>
                            Cancelar
                        </button>
                        <button type="submit" disabled={isLoading} style={{
                            padding: '0.5rem 1.5rem', background: 'var(--primary)', color: 'white',
                            border: 'none', borderRadius: '4px', cursor: 'pointer'
                        }}>
                            {isLoading ? 'Criando...' : 'Criar Análise'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default CreateAnalysisModal;