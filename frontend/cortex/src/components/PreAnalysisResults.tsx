import { useState } from "react";
import type { UploadedDocument } from "../interfaces/dto/UploadedDocument";
import type { Index } from "../interfaces/Index";
import type { IndexReference } from "../interfaces/IndexReference";
import type { PreAnalysisStage } from "../interfaces/PreAnalysisStage";
import styles from './css/PreAnalysisResults.module.css';
import DocumentViewer from "./DocumentViewer";

interface IndexItemProps {
    index: Index;
    onReferenceClick: (reference: IndexReference) => void;
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
}

interface PreAnalysisResultsProps {
    preAnalysisResult: PreAnalysisStage;
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    onContinue: () => void;
}

// Um componente "filho" para renderizar cada item da lista
const IndexItem: React.FC<IndexItemProps> = ({ 
    index, 
    onReferenceClick,
    analysisDocuments,
    referenceDocuments
}) => {
    // Helper para encontrar o nome do arquivo a partir do URI
    const getFileNameFromUri = (uri: string): string => {
        const allDocuments = [...analysisDocuments, ...referenceDocuments];
        // Compara o URI da referência com o GCSPath do documento
        const doc = allDocuments.find(d => d.gcsFilePath === uri);
        return doc ? doc.fileName : "Documento não encontrado";
    };

    return (
        <li className={styles.indexItem}>
            <div className={styles.indexContent}>
                <span className={styles.indicatorName}>{index.indicator.name}</span>
                <h3 className={styles.indexName}>{index.name}</h3>
                {index.description && <p className={styles.indexDescription}>{index.description}</p>}
                
                {/* Lista de Referências Clicáveis */}
                {index.references.length > 0 && (
                    <>
                        <span className={styles.referenceTitle}>Referências:</span>
                        <ul className={styles.referenceList}>
                            {index.references.map(ref => (
                                <li 
                                    key={ref.id} 
                                    className={styles.referenceItem} 
                                    onClick={() => onReferenceClick(ref)}
                                    title={getFileNameFromUri(ref.sourceDocumentUri)}
                                >
                                    📄 {ref.sourceDocumentUri} (p. {ref.page})
                                    {/* Tooltip com o trecho citado */}
                                    {ref.quotedContent && (
                                        <div className={styles.tooltip}>{ref.quotedContent}</div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    </>
                )}
            </div>
            
            <div className={styles.indexActions}>
                <button title="Editar" className={styles.actionButton}>✏️</button>
                <button title="Excluir" className={styles.actionButton}>🗑️</button>
            </div>
        </li>
    );
};


const PreAnalysisResults: React.FC<PreAnalysisResultsProps> = ({ 
    preAnalysisResult, 
    analysisDocuments, 
    referenceDocuments,
    onContinue
}) => {
    
    const { indexes } = preAnalysisResult;

    // Estado para "linkar" o clique da referência ao visualizador
    const [selectedReference, setSelectedReference] = useState<IndexReference | null>(null);

    const handleReferenceClick = (reference: IndexReference) => {
        setSelectedReference(reference);
    };

    return (
        // Layout de 2 colunas
        <div className={styles.splitLayout}>
            
            {/* Coluna da Esquerda: Resultados */}
            <section className={styles.resultsContainer}>
                <div className={styles.header}>
                    <h2 className={styles.title}>Resultados da Pré-Análise</h2>
                    <button className={styles.primaryButton}>+ Adicionar Novo Índice</button>
                </div>
                
                {indexes.length > 0 ? (
                    <ul className={styles.indexList}>
                        {indexes.map((index) => (
                            <IndexItem 
                                key={index.id} 
                                index={index} 
                                onReferenceClick={handleReferenceClick}
                                analysisDocuments={analysisDocuments}
                                referenceDocuments={referenceDocuments}
                            />
                        ))}
                    </ul>
                ) : (
                    <p>Nenhum índice foi extraído automaticamente. Você pode adicioná-los manualmente.</p>
                )}
                
                <footer className={styles.footer}>
                    <button 
                    onClick={onContinue}
                    className={styles.primaryButton}>Confirmar Índices e Avançar</button>
                </footer>
            </section>

            {/* Coluna da Direita: Visualizador de Documentos */}
            <section className={styles.viewerPane}>
                <DocumentViewer
                    analysisDocuments={analysisDocuments}
                    referenceDocuments={referenceDocuments}
                    selectedReference={selectedReference}
                />
            </section>
        </div>
    );
};

export default PreAnalysisResults;