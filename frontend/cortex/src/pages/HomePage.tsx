import React, { useCallback, useEffect, useState } from 'react';
import CreateAnalysisModal from '../components/CreateAnalysisModal';
import Logo from '../components/Logo';
import { handleApiError, type ApiErrorMap } from '../utils/errorUtils';
import type { AnalysisDto } from '../interfaces/dto/AnalysisDto';
import { deleteAnalysis, getAnalyses } from '../services/analysisService';
import AnalysisTable from '../components/AnalysisTable';
import { ErrorState } from '../components/ErrorState';
import { EmptyState } from '../components/EmptyState';
import { LoadingState } from '../components/LoadingState';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import type { AlertType } from '../components/Alert';
import Alert from '../components/Alert';
import DeleteAnalysisModal from '../components/DeleteAnalysisModal';
import PaginationControls from '../components/PaginationControls';

// Componente simples para o layout da página, inspirado nas referências
const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: 'var(--background-light)' }}>
        <header style={{
            padding: '1rem 2rem', borderBottom: '1px solid var(--background-medium)',
            backgroundColor: 'var(--background-light)', color: 'var(--text-dark)',
            display: 'flex', alignItems: 'center', gap: '0.5rem'
        }}>
            <div style={{ margin: "0", }}>
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

const PAGE_SIZE = 3;

export default function HomePage() {
    // Estado para controlar se o modal está aberto ou fechado
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [analyses, setAnalyses] = useState<AnalysisDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [analysisToDelete, setAnalysisToDelete] = useState<AnalysisDto | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);
    const [actionAlert, setActionAlert] = useState<{ message: string; type: AlertType } | null>(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);

    // EFEITO PARA BUSCAR OS DADOS QUANDO A PÁGINA CARREGA
   const fetchAnalyses = useCallback(async (page: number) => {
        setIsLoading(true);
        setError(null);
        setActionAlert(null); // Limpa alertas de ação
        try {
            const data = await getAnalyses({ pageNumber: page, pageSize: PAGE_SIZE });
            setAnalyses(data.items);
            setTotalPages(data.totalPages);
            setCurrentPage(data.pageNumber); // Sincroniza a página
        } catch (err) {
            const friendlyMessage = handleApiError(err, homeErrorMap);
            setError(friendlyMessage);
        } finally {
            setIsLoading(false);
        }
    }, []); // O useCallback memoriza a função

    // 7. EFEITO DE BUSCA INICIAL (e ao mudar de página)
    useEffect(() => {
        fetchAnalyses(currentPage);
    }, [currentPage, fetchAnalyses]);

    const handleConfirmDelete = async () => {
        if (!analysisToDelete) return;

        setIsDeleting(true);
        setActionAlert(null); // Limpa alertas antigos
        try {
            await deleteAnalysis(analysisToDelete.id);
            setAnalyses(prev => prev.filter(a => a.id !== analysisToDelete.id)); //atualiza lista no front
            setActionAlert({ message: "Análise excluída com sucesso.", type: "success" });

            if (analyses.length === 1 && currentPage > 1) {
                // Se era o último item, volte uma página
                setCurrentPage(currentPage - 1); // O useEffect cuidará do re-fetch
            } else {
                // Apenas recarregue a página atual
                fetchAnalyses(currentPage);
            }
        } catch (err) {
            setActionAlert({ message: "Falha ao excluir a análise.", type: "error" });
        } finally {
            setIsDeleting(false);
            setAnalysisToDelete(null); // Fecha o modal
        }
    };

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
        return <AnalysisTable onDeleteClick={setAnalysisToDelete} analyses={analyses} />;
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
                        display: 'flex', alignItems: 'center', gap: '8px'
                    }}
                >
                    <AddCircleOutlineIcon />  <strong>Criar Nova Análise</strong>
                </button>
            </div>

            {actionAlert && (
                <Alert 
                    message={actionAlert.message}
                    type={actionAlert.type}
                    onClose={() => setActionAlert(null)}
                />
            )}

            {/* Conteúdo Principal agora é dinâmico */}
            {renderContent()}
            
            {/*CONTROLES DE PAGINAÇÃO */}
            {!isLoading && !error && totalPages > 1 && (
                <PaginationControls
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPageChange={setCurrentPage}
                />
            )}

            {/* O Modal (não muda) */}
            <CreateAnalysisModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
            />

            {/* MODAL DE EXCLUSÃO */}
            {analysisToDelete && (
                <DeleteAnalysisModal
                    isOpen={!!analysisToDelete}
                    onClose={() => setAnalysisToDelete(null)}
                    onConfirm={handleConfirmDelete}
                    analysisName={analysisToDelete.title}
                    isConfirming={isDeleting}
                />
            )}
        </MainLayout>
    );
}