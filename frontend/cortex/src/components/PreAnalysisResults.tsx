import { useState } from "react";
import type { UploadedDocument } from "../interfaces/dto/UploadedDocument";
import type { Index } from "../interfaces/Index";
import type { IndexReference } from "../interfaces/IndexReference";
import type { PreAnalysisStage } from "../interfaces/PreAnalysisStage";
import styles from './css/PreAnalysisResults.module.css';
import DocumentViewer from "./DocumentViewer";
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import FindInPageIcon from '@mui/icons-material/FindInPage';
import EditIcon from '@mui/icons-material/Edit';
import DeleteForeverIcon from '@mui/icons-material/DeleteForever';
import IndexFormModal from "./IndexFormModal";
import { deleteIndex } from "../services/analysisService";
import Alert, { type AlertType } from "./Alert";
import ConfirmModal from "./ConfirmModal";
import { getFileNameFromUri, getReferencePageLabel } from "../utils/documentUtils";

interface IndexItemProps {
    index: Index;
    onReferenceClick: (reference: IndexReference) => void;
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    onEditClick: (index: Index) => void;
    onDeleteClick: (index: Index) => void;
}

interface PreAnalysisResultsProps {
    preAnalysisResult: PreAnalysisStage;
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    onContinue: () => void;
    onIndexAdded: (newIndex: Index) => void;
    onIndexUpdated: (updatedIndex: Index) => void;
    onIndexDeleted: (indexId: number) => void;
    alertInfo: { message: string; type: AlertType } | null;
    onCloseAlert: () => void;
}

// Um componente "filho" para renderizar cada item da lista
const IndexItem: React.FC<IndexItemProps> = ({ 
    index, 
    onReferenceClick,
    analysisDocuments,
    referenceDocuments,
    onEditClick,
    onDeleteClick
}) => {
    const allDocs = [...analysisDocuments, ...referenceDocuments];
    return (
        <li className={styles.indexItem}>
            <div className={styles.indexContent}>
                <div>
                    <label className={styles.label}>Indicador</label>
                    <span className={styles.indicatorName}>{index.indicator.name}</span>
                </div>
                <div style={{ marginTop: '1rem' }}>
                    <label className={styles.label}>Índice</label>
                    <h3 className={styles.indexName}>{index.name}</h3>
                </div>
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
                                    title={getFileNameFromUri(ref.sourceDocumentUri, allDocs)}
                                >
                                    <FindInPageIcon/> {getFileNameFromUri(ref.sourceDocumentUri, allDocs)}
                                      {' '}(p. {getReferencePageLabel(ref.sourceDocumentUri, ref.page, allDocs)})
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
               <button title="Editar" className={styles.actionButton} onClick={() => onEditClick(index)}>
                    <EditIcon/>
                </button>
                <button title="Excluir" className={styles.actionButton} onClick={() => onDeleteClick(index)}>
                    <DeleteForeverIcon/>
                </button>
            </div>
        </li>
    );
};


const PreAnalysisResults: React.FC<PreAnalysisResultsProps> = ({ 
    preAnalysisResult, 
    analysisDocuments, 
    referenceDocuments,
    onContinue, 
    onIndexAdded,
    onIndexUpdated,
    onIndexDeleted,
    alertInfo,
    onCloseAlert
}) => {
    
    const { indexes } = preAnalysisResult;

    // Estado para "linkar" o clique da referência ao visualizador
    const [selectedReference, setSelectedReference] = useState<IndexReference | null>(null);

    // Gerenciamento de estado dos modais
    const [isAddModalOpen, setIsAddModalOpen] = useState(false);
    const [indexToEdit, setIndexToEdit] = useState<Index | null>(null);
    const [indexToDelete, setIndexToDelete] = useState<Index | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);
    

    type ViewTab = 'analysis' | 'reference';
    const [activeTab, setActiveTab] = useState<ViewTab>('analysis');

   const handleReferenceClick = (reference: IndexReference) => {
        // 1. Descobre para qual aba o documento pertence
        const doc = [...analysisDocuments, ...referenceDocuments].find(
            d => d.gcsFilePath === reference.sourceDocumentUri
        );
        if (doc) {
            const isAnalysisDoc = analysisDocuments.some(d => d.id === doc.id);
            // 2. Força a troca da aba, se necessário
            setActiveTab(isAnalysisDoc ? 'analysis' : 'reference');
        }
        
        // 3. Define a referência
        setSelectedReference(reference);
    };

    const handleChangeTab = (tab: ViewTab) => {
        setActiveTab(tab);
        setSelectedReference(null); 
    };

    // Função para confirmar e executar a exclusão
    const handleConfirmDelete = async () => {
        if (!indexToDelete) return;
        
        setIsDeleting(true);
        setDeleteError(null);
        try {
            await deleteIndex(indexToDelete.id);
            onIndexDeleted(indexToDelete.id); // Notifica o pai
            setIndexToDelete(null); // Fecha o modal
        } catch (err) {
            setDeleteError("Falha ao excluir o índice. Tente novamente.");
        } finally {
            setIsDeleting(false);
        }
    };

    return (
        // Layout de 2 colunas
        <div className={styles.splitLayout}>
            
            {/* Coluna da Esquerda: Resultados */}
            <section className={styles.resultsContainer}>
                <div className={styles.header}>
                    <h2 className={styles.title}>Resultados da Pré-Análise</h2>
                    <button className={styles.primaryButton} onClick={() => setIsAddModalOpen(true)}>
                         <AddCircleOutlineIcon/> <strong>Adicionar Novo Índice</strong>
                    </button>
                </div>
                
                {alertInfo && (
                    <Alert 
                        message={alertInfo.message}
                        type={alertInfo.type}
                        onClose={onCloseAlert}
                    />
                )}

                {deleteError && <Alert message={deleteError} type="error" onClose={() => setDeleteError(null)} />}

                <div className={styles.scrollableContent}>
                    {indexes.length > 0 ? (
                        <ul className={styles.indexList}>
                            {indexes.map((index) => (
                                <IndexItem 
                                    key={index.id} 
                                    index={index} 
                                    onReferenceClick={handleReferenceClick}
                                    analysisDocuments={analysisDocuments}
                                    referenceDocuments={referenceDocuments}
                                    onEditClick={setIndexToEdit}
                                    onDeleteClick={setIndexToDelete}
                                />
                            ))}
                        </ul>
                    ) : (
                        <p className={styles.emptyMessage}>
                            Nenhum índice foi extraído automaticamente. Você pode adicioná-los manualmente.
                        </p>
                    )}
                </div>
                
                <footer className={styles.footer}>
                    <button 
                        onClick={onContinue}
                        className={styles.primaryButton}
                    >
                        <strong>Confirmar Índices e Avançar</strong>
                    </button>
                </footer>
            </section>

            {/* Coluna da Direita: Visualizador de Documentos */}
            <section className={styles.viewerPane}>
                <DocumentViewer
                    analysisDocuments={analysisDocuments}
                    referenceDocuments={referenceDocuments}
                    selectedReference={selectedReference}
                    activeTab={activeTab}        
                    onTabChange={handleChangeTab} 
                />
            </section>
            
            {/* MODAL PARA ADICIONAR NOVO ÍNDICE */}
            <IndexFormModal
                isOpen={isAddModalOpen || !!indexToEdit}
                onClose={() => {
                    setIsAddModalOpen(false);
                    setIndexToEdit(null);
                }}
                preAnalysisStageId={preAnalysisResult.id}
                indexToEdit={indexToEdit}
                onIndexAdded={(newIndex) => {
                    onIndexAdded(newIndex);
                    setIsAddModalOpen(false); // Fecha o modal
                }}
                onIndexUpdated={(updatedIndex) => {
                    onIndexUpdated(updatedIndex);
                    setIndexToEdit(null); // Fecha o modal
                }}
            />

            {/* Modal de Confirmação (Delete) */}
            <ConfirmModal
                isOpen={!!indexToDelete}
                title="Confirmar Exclusão"
                message={
                    <p>
                        Você tem certeza que deseja excluir o índice <strong>"{indexToDelete?.name}"</strong>?
                        <br/><br/>
                        Esta ação não pode ser desfeita.
                    </p>
                }
                confirmText="Sim, Excluir"
                isConfirming={isDeleting}
                onClose={() => setIndexToDelete(null)}
                onConfirm={handleConfirmDelete}
            />
        </div>
    );
};

export default PreAnalysisResults;