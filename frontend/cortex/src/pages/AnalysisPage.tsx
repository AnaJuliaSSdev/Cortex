import React, { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import FileUploader from '../components/FileUploader';
import FileList from '../components/FileList';
import styles from './css/AnalysisPage.module.css'; // Novo CSS
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { DocumentPurpose } from '../interfaces/enum/DocumentPurpose';
import Logo from '../components/Logo';
import { continueAnalysis, getAnalysisState, postAnalysisQuestion, startAnalysis } from '../services/analysisService';
import type { AnalysisExecutionResult } from '../interfaces/dto/AnalysisExecutionResult';
import PreAnalysisResults from '../components/PreAnalysisResults';
import LoadingSpinner from '../components/LoadingSpinner';
import ExplorationResults from '../components/ExplorationResults';
import { handleApiError, type ApiErrorMap } from '../utils/errorUtils';
import { ErrorState } from '../components/ErrorState';
import Alert, { type AlertType } from '../components/Alert';
import type { Index } from '../interfaces/Index';


// Dicionário de erros para AÇÕES (iniciar, continuar)
const analysisActionErrorMap: ApiErrorMap = {
    byStatusCode: {
        500: "Ocorreu um erro interno no servidor ao processar sua análise. Tente novamente."
        // Adicione outros erros específicos de 'start' ou 'continue'
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

// Componente de Layout (pode ser movido para components/Layout.tsx)
const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className={styles.layout}>
        <header className={styles.header}>
            <Logo></Logo>
        </header>
        <main className={styles.mainContent}>
            {children}
        </main>
    </div>
);


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
    const [alertInfo, setAlertInfo] = useState<{ message: string; type: AlertType } | null>(null);

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
        setAnalysisDocuments((prevDocs) => [...prevDocs, doc]);
    };

    const handleReferenceUpload = (doc: UploadedDocument) => {
        setReferenceDocuments((prevDocs) => [...prevDocs, doc]);
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
        setAlertInfo(null); // Limpa alertas anteriores

        try {
            // Passo 1: Enviar e salvar a pergunta central
            await postAnalysisQuestion(id, question);
            console.log('Pergunta salva com sucesso!');

            // Passo 2: Iniciar a análise (agora que a pergunta e os docs estão lá)
            console.log('Iniciando a análise...');
            const result = await startAnalysis(id);

            if (result.isSuccess) {
                setAnalysisResult(result);
            } else {
                // Se o backend retornar isSuccess = false
                setAlertInfo({ message: result.errorMessage || 'Ocorreu um erro desconhecido ao processar a análise.', type: "error" });
            }

        } catch (error) {
            const friendlyMessage = handleApiError(error, analysisActionErrorMap);
            setAlertInfo({ message: friendlyMessage, type: "error" });
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
            } else {
                console.log("deu erro tinha q lançar excecao")
                setAlertInfo({ message: result.errorMessage || 'Falha ao continuar a análise.', type: "error" });
            }
        } catch (error) {
            console.log("deu erro tinha q lançar excecao")
            const friendlyMessage = handleApiError(error, analysisActionErrorMap);
            setAlertInfo({ message: friendlyMessage, type: "error" });
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
            <MainLayout>
                <LoadingSpinner messages={loadingMessages} />
            </MainLayout>
        );
    }

    if (!analysisResult) {
        return <ErrorState message={error || "Não foi possível carregar os dados da análise."} />;
    }

    if (analysisResult?.explorationOfMaterialStage) {
        return (
            <MainLayout>
                <h1 className={styles.pageTitle}>Análise: {analysisResult.analysisTitle}</h1>
                <ExplorationResults
                    explorationStage={analysisResult.explorationOfMaterialStage}
                />
            </MainLayout>
        );
    }

    if (analysisResult?.preAnalysisResult) {
        return (
            <MainLayout>
                <h1 className={styles.pageTitle}>Análise: {analysisResult.analysisTitle}</h1>
                <PreAnalysisResults
                    preAnalysisResult={analysisResult.preAnalysisResult}
                    analysisDocuments={analysisDocuments}
                    referenceDocuments={referenceDocuments}
                    onIndexAdded={handleIndexAdded}
                    onContinue={handleContinueToExploration}
                    onIndexUpdated={handleIndexUpdated}
                    onIndexDeleted={handleIndexDeleted}
                    alertInfo={alertInfo}
                    onCloseAlert={() => setAlertInfo(null)}
                />
            </MainLayout>
        );
    }

    return (
        <MainLayout>
            <form onSubmit={handleStartAnalysis} className={styles.analysisForm}>
                <h1 className={styles.pageTitle}>Configurar Análise: {analysisResult.analysisTitle}</h1>

                {alertInfo && (
                    <Alert
                        message={alertInfo.message}
                        type={alertInfo.type}
                        onClose={() => setAlertInfo(null)}
                    />
                )}

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
                    <FileList files={analysisDocuments} />
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
                    <FileList files={referenceDocuments} />
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
        </MainLayout>
    );
}