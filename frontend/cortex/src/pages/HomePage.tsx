import React, { useEffect, useState } from 'react';
import CreateAnalysisModal from '../components/CreateAnalysisModal';
import Logo from '../components/Logo';
import { handleApiError, type ApiErrorMap } from '../utils/errorUtils';
import type { AnalysisDto } from '../interfaces/dto/AnalysisDto';
import { getAnalyses } from '../services/analysisService';
import AnalysisTable from '../components/AnalysisTable';
import { ErrorState } from '../components/ErrorState';
import { EmptyState } from '../components/EmptyState';
import { LoadingState } from '../components/LoadingState';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';

// Componente simples para o layout da página, inspirado nas referências
const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: 'var(--background-light)' }}>
        <header style={{
            padding: '1rem 2rem', borderBottom: '1px solid var(--background-medium)',
            backgroundColor: 'var(--background-light)', color: 'var(--text-dark)',
            display: 'flex', alignItems: 'center', gap: '0.5rem'
        }}>
			<div style={{margin :"0",}}>
				<Logo />
			</div>
        </header>
        <main style={{ flex: 1, padding: '2rem' }}>
            {children}
        </main>
    </div>
);

const homeErrorMap: ApiErrorMap = {
    byStatusCode: {
        500: "Erro ao buscar suas análises. Tente novamente mais tarde."
    },
    default: "Ocorreu um erro inesperado ao carregar seus dados."
};


export default function HomePage() {
    // Estado para controlar se o modal está aberto ou fechado
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [analyses, setAnalyses] = useState<AnalysisDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // EFEITO PARA BUSCAR OS DADOS QUANDO A PÁGINA CARREGA
    useEffect(() => {
        const fetchAnalyses = async () => {
            try {
                setError(null);
                setIsLoading(true);
                const data = await getAnalyses();
                setAnalyses(data);
            } catch (err) {
                const friendlyMessage = handleApiError(err, homeErrorMap);
                setError(friendlyMessage);
            } finally {
                setIsLoading(false);
            }
        };

        fetchAnalyses();
    }, []); // O array vazio [] garante que isso rode apenas uma vez

    // Função para renderizar o conteúdo principal da página
    const renderContent = () => {
        if (isLoading) {
            return <LoadingState />;
        }
        if (error) {
            return <ErrorState message={error} />;
        }
        if (analyses.length === 0) {
            return <EmptyState />;
        }
        return <AnalysisTable analyses={analyses} />;
    };

    return (
        <MainLayout>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
                <h2 style={{ fontSize: '1.75rem', color: 'var(--text-dark)', margin: 0 }}>Suas Análises</h2>
                <button 
                    onClick={() => setIsModalOpen(true)}
                    style={{
                        padding: '0.75rem 1.5rem', background: 'var(--primary)', color: 'white',
                        border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 500,
                        display: 'flex', alignItems: 'center' , gap: '8px'
                    }}
                >
                   <AddCircleOutlineIcon/>  <strong>Criar Nova Análise</strong>
                </button>
            </div>

            {/* Conteúdo Principal agora é dinâmico */}
            {renderContent()}
            
            {/* O Modal (não muda) */}
            <CreateAnalysisModal 
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)} 
            />
        </MainLayout>
    );
}