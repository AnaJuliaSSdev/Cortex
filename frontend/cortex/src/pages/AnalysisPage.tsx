import React, { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import FileUploader from '../components/FileUploader';
import FileList from '../components/FileList';
import styles from './css/AnalysisPage.module.css'; // Novo CSS
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { DocumentPurpose } from '../interfaces/enum/DocumentPurpose';
import { continueAnalysis, deleteDocument, getAnalysisState, postAnalysisQuestion, startAnalysis } from '../services/analysisService';
import type { AnalysisExecutionResult } from '../interfaces/dto/AnalysisExecutionResult';
import PreAnalysisResults from '../components/PreAnalysisResults';
import LoadingSpinner from '../components/LoadingSpinner';
import ExplorationResults from '../components/ExplorationResults';
import { handleApiError, type ApiErrorMap } from '../utils/errorUtils';
import { ErrorState } from '../components/ErrorState';
import type { Index } from '../interfaces/Index';
import ConfirmModal from '../components/ConfirmModal';
import type ToastState from '../interfaces/dto/ToastState';
import type { AlertColor } from '@mui/material/Alert';
import Toast from '../components/Toast';

// Dicionário de erros para AÇÕES (iniciar, continuar)
const analysisActionErrorMap: ApiErrorMap = {
    byStatusCode: {
        500: "Ocorreu um erro interno no servidor ao processar sua análise. Tente novamente.", 
        404: "Análise não encontrada. Verifique o link ou crie uma nova análise.",
        403: "Você não tem permissão para acessar esta análise.",
        413: "O tamanho total dos documentos enviados excede o limite permitido. Remova alguns arquivos e tente novamente.", 
        400: "Requisição inválida. Verifique os dados enviados e tente novamente.", 
         
    },
    default: "Uma falha inesperada ocorreu. Verifique sua conexão e tente novamente."
};

// Textos que vão ciclar para dar sensação de progresso
const preAnalysisMessages = [
    'Preparando o envio dos documentos...',
    'Processando documentos de análise...',
    'Aplicando inteligência...',
    'Extraindo índices e indicadores...',
    'Compilando referências...',
    'Quase pronto, organizando os resultados...',
];


const explorationMessages = [
    'Continuando análise...',
    'Processando unidades de registro...',
    'Gerando categorias...',
    'Contando índices...',
    'Quase lá...'
];

const initialLoadingMessages = ['Carregando sua análise...'];

const analysisPageErrorMap: ApiErrorMap = {
    byStatusCode: {
        404: "Análise não encontrada. Verifique o link ou crie uma nova análise.",
        403: "Você não tem permissão para acessar esta análise.",
        500: "Ocorreu um erro no servidor ao buscar os dados."
    },
    default: "Não foi possível carregar a análise. Tente novamente."
};

const MAX_TOTAL_SIZE_MB = 100;
const MAX_TOTAL_SIZE_BYTES = MAX_TOTAL_SIZE_MB * 1024 * 1024; 


export default function AnalysisPage() {
    const { id } = useParams<{ id: string }>();
    if (!id) return <p>ID da análise não encontrado.</p>; // Proteção simples

    const [question, setQuestion] = useState('');
    const [analysisDocuments, setAnalysisDocuments] = useState<UploadedDocument[]>([]);
    const [referenceDocuments, setReferenceDocuments] = useState<UploadedDocument[]>([]);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [loadingMessages, setLoadingMessages] = useState(preAnalysisMessages);
    const [isLoading, setIsLoading] = useState(true); // Começa carregando
    const [error, setError] = useState<string | null>(null);
    const [analysisResult, setAnalysisResult] = useState<AnalysisExecutionResult | null>(null);
    const [docToDelete, setDocToDelete] = useState<UploadedDocument | null>(null);
    const [isDeletingDoc, setIsDeletingDoc] = useState(false);
    const [toast, setToast] = useState<ToastState>({
        open: false,
        message: '',
        type: 'info'
    });

    // Função auxiliar para mostrar o toast
    const showToast = (message: string, type: AlertColor) => {
        setToast({ open: true, message, type });
    };

    // Função para fechar o toast
    const closeToast = () => {
        setToast(prev => ({ ...prev, open: false }));
    };

    useEffect(() => {
        const fetchAnalysis = async () => {
            try {
                setIsLoading(true);
                setError(null);
                const result = await getAnalysisState(id);
                setAnalysisResult(result);
                setAnalysisDocuments(result.analysisDocuments || []);
                setReferenceDocuments(result.referenceDocuments || []);
                setQuestion(result.analysisQuestion || '');
            } catch (err) {
                const friendlyMessage = handleApiError(err, analysisPageErrorMap);
                setError(friendlyMessage);
            } finally {
                setIsLoading(false);
            }
        };
        fetchAnalysis();
    }, [id]); // Roda sempre que o ID na URL mudar

    // Validação para habilitar o botão de iniciar
    const isFormValid = question.trim() !== '' && analysisDocuments.length > 0;


    // Callbacks para os uploaders
    const handleAnalysisUpload = (doc: UploadedDocument) => {
        const docWithCorrectPurpose = { ...doc, purpose: DocumentPurpose.Analysis };
        setAnalysisDocuments((prevDocs) => [...prevDocs, docWithCorrectPurpose]);
    };

    const handleReferenceUpload = (doc: UploadedDocument) => {
        const docWithCorrectPurpose = { ...doc, purpose: DocumentPurpose.Reference };
        setReferenceDocuments((prevDocs) => [...prevDocs, docWithCorrectPurpose]);
    };

    const handleConfirmDeleteDoc = async () => {
        if (!docToDelete) return;

        setIsDeletingDoc(true);
        try {
            // Chama a API
            await deleteDocument(docToDelete.id);

            // Remove o documento do estado local
            if (docToDelete.purpose === DocumentPurpose.Analysis) {
                setAnalysisDocuments(prev => prev.filter(d => d.id !== docToDelete.id));
            } else {
                setReferenceDocuments(prev => prev.filter(d => d.id !== docToDelete.id));
            }

            showToast("Documento excluído com sucesso.", "success");        
        } catch (error) {
            showToast("Falha ao excluir o documento.", "error");
        } finally {
            setIsDeletingDoc(false);
            setDocToDelete(null); // Fecha o modal
        }
    };

    // Função para enviar a pergunta e "iniciar" a análise
    /**
      * Orquestra o processo final:
      * 1. Salva a pergunta no backend.
      * 2. Dispara o início da análise.
      */
    const handleStartAnalysis = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!isFormValid) return;

        setIsSubmitting(true);

        try {
            // Passo 1: Enviar e salvar a pergunta central
            await postAnalysisQuestion(id, question);

            // Passo 2: Iniciar a análise (agora que a pergunta e os docs estão lá)
            const result = await startAnalysis(id);

            if (result.isSuccess) {
                setAnalysisResult(result);
                setAnalysisDocuments(result.analysisDocuments || []);
                setReferenceDocuments(result.referenceDocuments || []);
            } else {
                const friendlyMessage = handleApiError(result, analysisActionErrorMap);
                showToast(friendlyMessage || 'Ocorreu um erro desconhecido ao processar a análise.', "error");
            }
        } catch (error) {
            const friendlyMessage = handleApiError(error, analysisActionErrorMap);
            showToast(friendlyMessage, "error");
        } finally {
            setIsSubmitting(false);
        }
    };

    // Função para a ETAPA 2 (Pré-Análise -> Exploração)
    const handleContinueToExploration = async () => {
        setLoadingMessages(explorationMessages);
        setIsSubmitting(true);
        try {
            const result = await continueAnalysis(id);
            if (result.isSuccess) {
                setAnalysisResult(result); // Atualiza o estado com o NOVO resultado
                setAnalysisDocuments(result.analysisDocuments || []);
                setReferenceDocuments(result.referenceDocuments || []);
            } else {
                const friendlyMessage = handleApiError(result, analysisActionErrorMap);
                showToast(friendlyMessage || 'Falha ao continuar a análise.', "error");            
        }
        } catch (error) {
            const friendlyMessage = handleApiError(error, analysisActionErrorMap);
            showToast(friendlyMessage, "error");            
        } finally {
            setIsSubmitting(false);
        }
    };

    // Função call back da modal do índice
    const handleIndexAdded = (newIndex: Index) => {
        if (!analysisResult || !analysisResult.preAnalysisResult) return;

        // Atualiza o estado da análise com o novo índice na lista
        setAnalysisResult({
            ...analysisResult,
            preAnalysisResult: {
                ...analysisResult.preAnalysisResult,
                indexes: [
                    ...analysisResult.preAnalysisResult.indexes, 
                    newIndex
                ]
            }
        });
    };

    //Callback para Atualizar o index
    const handleIndexUpdated = (updatedIndex: Index) => {
        if (!analysisResult?.preAnalysisResult) return;
        
        setAnalysisResult({
            ...analysisResult,
            preAnalysisResult: {
                ...analysisResult.preAnalysisResult,
                // Mapeia a lista, encontra o índice antigo e o substitui
                indexes: analysisResult.preAnalysisResult.indexes.map(idx =>
                    idx.id === updatedIndex.id ? updatedIndex : idx
                )
            }
        });
    };

    //Callback para Excluir o index
    const handleIndexDeleted = (deletedIndexId: number) => {
        if (!analysisResult?.preAnalysisResult) return;
        
        setAnalysisResult({
            ...analysisResult,
            preAnalysisResult: {
                ...analysisResult.preAnalysisResult,
                // Filtra a lista, removendo o índice excluído
                indexes: analysisResult.preAnalysisResult.indexes.filter(idx =>
                    idx.id !== deletedIndexId
                )
            }
        });
    };



    //tamanho total usado para cada tipo, usando useMemo
    const analysisSizeUsed = useMemo(() => {
        return analysisDocuments.reduce((sum, doc) => sum + doc.fileSize, 0);
    }, [analysisDocuments]);

    const referenceSizeUsed = useMemo(() => {
        return referenceDocuments.reduce((sum, doc) => sum + doc.fileSize, 0);
    }, [referenceDocuments]);

    if (isLoading) {
        return <LoadingSpinner messages={initialLoadingMessages} />;
    }

    if (isSubmitting) {
        return (
            <>
                <LoadingSpinner messages={loadingMessages} />
            </>
        );
    }

    if (!analysisResult) {
        return <ErrorState message={error || "Não foi possível carregar os dados da análise."} />;
    }

    if (analysisResult?.explorationOfMaterialStage) {
        return (
            <> 
                <Toast open={toast.open} message={toast.message} type={toast.type} onClose={closeToast} />
                <h1 className={styles.pageTitle}>Análise: {analysisResult.analysisTitle}</h1>
                <p>{analysisResult.analysisQuestion}</p>
                <ExplorationResults
                    explorationStage={analysisResult.explorationOfMaterialStage}
                    analysisDocuments={analysisDocuments}
                    referenceDocuments={referenceDocuments}
                    analysisId={id}
                />
            </>
        );
    }

    if (analysisResult?.preAnalysisResult) {
        return (
            <>
                <Toast open={toast.open} message={toast.message} type={toast.type} onClose={closeToast} />
                <h1 className={styles.pageTitle}>Análise: {analysisResult.analysisTitle}</h1>
                <PreAnalysisResults
                    preAnalysisResult={analysisResult.preAnalysisResult}
                    analysisDocuments={analysisDocuments}
                    referenceDocuments={referenceDocuments}
                    onIndexAdded={handleIndexAdded}
                    onContinue={handleContinueToExploration}
                    onIndexUpdated={handleIndexUpdated}
                    onIndexDeleted={handleIndexDeleted}         
                    onShowToast={showToast}     
                />
            </>
        );
    }

    return (
        <>
            <Toast open={toast.open} message={toast.message} type={toast.type} onClose={closeToast} />
            <form onSubmit={handleStartAnalysis} className={styles.analysisForm}>
                <h1 className={styles.pageTitle}>Configurar Análise: {analysisResult.analysisTitle}</h1>
                {/* Pergunta Central */}
                <section className={styles.formSection}>
                    <label htmlFor="question" className={styles.label}>
                        Pergunta Central da Análise <span className={styles.required}>*</span>
                    </label>
                    <p className={styles.description}>
                        Qual é a pergunta principal que esta análise de conteúdo busca responder?
                    </p>
                    <textarea
                        id="question"
                        value={question}
                        onChange={(e) => setQuestion(e.target.value)}
                        className={styles.textarea}
                        placeholder="Ex: Qual é a percepção dos usuários sobre a nova funcionalidade?"
                    />
                </section>

                {/* Documentos de Análise (Obrigatório) */}
                <section className={styles.formSection}>
                    <FileUploader
                        analysisId={id}
                        purpose={DocumentPurpose.Analysis}
                        title="Documentos de Análise *"
                        description="Arraste e solte os arquivos que serão analisados."
                        onUploadSuccess={handleAnalysisUpload}
                        currentTotalSize={analysisSizeUsed}
                        maxTotalSize={MAX_TOTAL_SIZE_BYTES}
                    />
                   <FileList 
                        files={analysisDocuments} 
                        onDeleteClick={setDocToDelete} 
                    />
                </section>

                {/* Documentos de Referência (Opcional) */}
                <section className={styles.formSection}>
                    <FileUploader
                        analysisId={id}
                        purpose={DocumentPurpose.Reference}
                        title="Documentos de Referência (Opcional)"
                        description="Arraste e solte arquivos de apoio (ex: bibliografia, guias)"
                        onUploadSuccess={handleReferenceUpload}
                        currentTotalSize={referenceSizeUsed}
                        maxTotalSize={MAX_TOTAL_SIZE_BYTES}
                    />
                    <FileList files={referenceDocuments}  onDeleteClick={setDocToDelete}  />
                </section>

                {/* 4. Ação Final */}
                <footer className={styles.footer}>
                    <button
                        type="submit"
                        className={styles.submitButton}
                        disabled={!isFormValid || isSubmitting}
                    >
                        {isSubmitting ? 'Iniciando...' : 'Iniciar Pré Análise'}
                    </button>
                    {!isFormValid && (
                        <p className={styles.validationMessage}>
                            Preencha a Pergunta Central e envie pelo menos um Documento de Análise.
                        </p>
                    )}
                </footer>
            </form>
            <ConfirmModal
                isOpen={!!docToDelete}
                title="Excluir Documento"
                message={
                    <p>
                        Você tem certeza que deseja excluir o documento 
                        <strong> "{docToDelete?.fileName}"</strong>?
                        <br/><br/>
                        Esta ação não pode ser desfeita.
                    </p>
                }
                confirmText="Sim, Excluir"
                isConfirming={isDeletingDoc}
                onClose={() => setDocToDelete(null)}
                onConfirm={handleConfirmDeleteDoc}
            />
        </>
    );
}