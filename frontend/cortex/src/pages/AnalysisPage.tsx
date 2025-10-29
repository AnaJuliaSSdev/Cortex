import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import FileUploader from '../components/FileUploader';
import FileList from '../components/FileList';
import styles from './css/AnalysisPage.module.css'; // Novo CSS
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { DocumentPurpose } from '../interfaces/enum/DocumentPurpose';
import Logo from '../components/Logo';
import { continueAnalysis, postAnalysisQuestion, startAnalysis } from '../services/analysisService';
import type { AnalysisExecutionResult } from '../interfaces/dto/AnalysisExecutionResult';
import PreAnalysisResults from '../components/PreAnalysisResults';
import LoadingSpinner from '../components/LoadingSpinner';
import ExplorationResults from '../components/ExplorationResults';

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
    const [analysisResult, setAnalysisResult] = useState<AnalysisExecutionResult | null>(null);
    
    // (Opcional) Buscar o estado da análise ao carregar a página
    // useEffect(() => {
    //    const fetchAnalysis = async () => {
    //       // Crie uma função getAnalysis(id) no seu service
    //       // const result = await analysisService.getAnalysis(id);
    //       // setAnalysisResult(result);
    //    }
    //    fetchAnalysis();
    // }, [id]);

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
                alert(`Falha ao iniciar a análise: ${result.errorMessage || 'Erro desconhecido.'}`);
            }

        } catch (error) {
            console.error("Ocorreu um erro no processo de iniciar a análise:", error);
            alert('Falha ao iniciar a análise. Verifique o console para mais detalhes.');
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
                alert(`Falha ao continuar a análise: ${result.errorMessage || 'Erro desconhecido.'}`);
            }
        } catch (error) {
            alert('Falha ao continuar a análise. Verifique o console.');
        } finally {
            setIsSubmitting(false);
        }
    };

    if (isSubmitting) {
        return (
            <MainLayout>
                <LoadingSpinner messages={preAnalysisMessages} />
            </MainLayout>
        );
    }

    if (analysisResult?.explorationOfMaterialStage) {
        return (
            <MainLayout>
                <h1 className={styles.pageTitle}>Análise (ID: {id})</h1>
                <ExplorationResults 
                    explorationStage={analysisResult.explorationOfMaterialStage}
                />
                {/* Aqui você também pode renderizar o PreAnalysisResults
                    ou o DocumentViewer, se quiser */}
            </MainLayout>
        );
    }

    if (analysisResult?.preAnalysisResult) {
        return (
            <MainLayout>
                <h1 className={styles.pageTitle}>Análise (ID: {id})</h1>
                <PreAnalysisResults
                    preAnalysisResult={analysisResult.preAnalysisResult}
                    analysisDocuments={analysisResult.analysisDocuments}
                    referenceDocuments={analysisResult.referenceDocuments}
                    onContinue={handleContinueToExploration}
                />
            </MainLayout>
        );
    }

    return (
        <MainLayout>
            <form onSubmit={handleStartAnalysis} className={styles.analysisForm}>
                <h1 className={styles.pageTitle}>Configurar Análise (ID: {id})</h1>

                {/* 1. Pergunta Central */}
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

                {/* 2. Documentos de Análise (Obrigatório) */}
                <section className={styles.formSection}>
                    <FileUploader
                        analysisId={id}
                        purpose={DocumentPurpose.Analysis}
                        title="Documentos de Análise *"
                        description="Arraste e solte os arquivos que serão analisados (.pdf, .txt)"
                        onUploadSuccess={handleAnalysisUpload}
                    />
                    <FileList files={analysisDocuments} />
                </section>

                {/* 3. Documentos de Referência (Opcional) */}
                <section className={styles.formSection}>
                    <FileUploader
                        analysisId={id}
                        purpose={DocumentPurpose.Reference}
                        title="Documentos de Referência (Opcional)"
                        description="Arraste e solte arquivos de apoio (ex: bibliografia, guias)"
                        onUploadSuccess={handleReferenceUpload}
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