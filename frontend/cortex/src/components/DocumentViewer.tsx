import { useState, useEffect } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import 'react-pdf/dist/Page/TextLayer.css';

import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import api from '../services/api'; // Precisamos do 'api' para o download
import styles from './css/DocumentViewer.module.css';
import type { IndexReference } from '../interfaces/IndexReference';

import workerSrc from 'pdfjs-dist/build/pdf.worker.min.mjs?url';
pdfjs.GlobalWorkerOptions.workerSrc = workerSrc;

interface DocumentViewerProps {
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    selectedReference: IndexReference | null;
}

export default function DocumentViewer({ 
    analysisDocuments, 
    referenceDocuments,
    selectedReference 
}: DocumentViewerProps) {
    
    type ViewTab = 'analysis' | 'reference';
    const [activeTab, setActiveTab] = useState<ViewTab>('analysis');
    const [selectedDocument, setSelectedDocument] = useState<UploadedDocument | null>(null);
    const [fileUrl, setFileUrl] = useState<string | null>(null); // Para PDF (blob URL)
    const [fileContent, setFileContent] = useState<string | null>(null); // Para TXT
    const [isLoading, setIsLoading] = useState(false);
    const [numPages, setNumPages] = useState<number>(0);
    const [pageNumber, setPageNumber] = useState(1);

    // Efeito para carregar o documento do backend quando selecionado
    useEffect(() => {
        if (!selectedDocument) {
            setFileUrl(null);
            setFileContent(null);
            return;
        }

        const fetchDocument = async () => {
            setIsLoading(true);
            try {
                // Usando o endpoint de download que você criou
                const response = await api.get(`/documents/download/${selectedDocument.id}`, {
                    responseType: 'blob', // Importante: o backend retorna um arquivo
                });
                
                const blob: Blob = response.data;

                if (blob.type === 'application/pdf') {
                    // Cria uma URL temporária para o blob
                    const url = URL.createObjectURL(blob);
                    setFileUrl(url);
                    setFileContent(null);
                } else if (blob.type === 'text/plain') {
                    // Lê o blob como texto
                    const text = await blob.text();
                    setFileContent(text);
                    setFileUrl(null);
                } else {
                    console.error("Tipo de arquivo não suportado ou desconhecido:", blob.type);
                }

            } catch (error) {
                console.error("Erro ao baixar o documento:", error);
            } finally {
                setIsLoading(false);
            }
        };

        fetchDocument();
        
        // Limpa a URL do blob quando o componente é desmontado
        return () => {
            if (fileUrl) {
                URL.revokeObjectURL(fileUrl);
            }
        };
    }, [selectedDocument]);

    // Efeito para pular para a página quando uma referência é clicada
    useEffect(() => {
        if (selectedReference) {
            const doc = [...analysisDocuments, ...referenceDocuments].find(
                // Compara o GCS path do documento com o URI da referência
                d => d.gcsFilePath === selectedReference.sourceDocumentUri
            );
            
            if (!doc) return;

            // Determina para qual aba o documento pertence
            const isAnalysisDoc = analysisDocuments.some(d => d.id === doc.id);
            const targetTab = isAnalysisDoc ? 'analysis' : 'reference';
            
            // Força a troca da aba, se necessário
            if (activeTab !== targetTab) {
                setActiveTab(targetTab);
            }

            // Se a referência for do documento já selecionado
            if (selectedDocument?.id !== doc.id) {
                // Se o documento for diferente, define-o como selecionado
                // O 'onDocumentLoadSuccess' cuidará de pular a página
                setSelectedDocument(doc);
            } else {
                const page = parseInt(selectedReference.page, 10);
                if (!isNaN(page) && page > 0 && page <= numPages) {
                    setPageNumber(page);
                }
            }
        }
    }, [selectedReference, numPages, analysisDocuments, referenceDocuments, selectedDocument]);

    const onDocumentLoadSuccess = ({ numPages }: { numPages: number }) => {
        setNumPages(numPages);
        // Se a referência já estiver selecionada, pule para a página
        const page = selectedReference ? parseInt(selectedReference.page, 10) : 1;
        setPageNumber(isNaN(page) ? 1 : page);
    };

    // Esta função controla a troca de abas e limpa o visualizador.
    const handleChangeTab = (tab: ViewTab) => {
        setActiveTab(tab);
        setSelectedDocument(null); // Limpa o documento selecionado
        setFileUrl(null);           // Limpa o visualizador de PDF
        setFileContent(null);       // Limpa o visualizador de TXT
        setNumPages(0);
        setPageNumber(1);
    };

    const docsToDisplay = activeTab === 'analysis' ? analysisDocuments : referenceDocuments;

    /** Navega para a página anterior, se não estiver na primeira */
    const goToPrevPage = () => {
        setPageNumber(prevPageNumber => Math.max(prevPageNumber - 1, 1));
    };

    /** Navega para a próxima página, se não estiver na última */
    const goToNextPage = () => {
        setPageNumber(prevPageNumber => Math.min(prevPageNumber + 1, numPages));
    };

    return (
        <div className={styles.viewerContainer}>
            {/* Abas de Documentos */}
            <div className={styles.tabs}>
                <button 
                    className={activeTab === 'analysis' ? styles.activeTab : ''}
                    onClick={() => handleChangeTab('analysis')}
                >
                    Análise ({analysisDocuments.length})
                </button>
                <button 
                    className={activeTab === 'reference' ? styles.activeTab : ''}
                    onClick={() => handleChangeTab('reference')}
                >
                    Referência ({referenceDocuments.length})
                </button>
            </div>

            {/* Lista de Documentos da Aba */}
             <ul className={styles.docList}>
                 {docsToDisplay.length === 0 ? (
                    <li className={styles.docItemEmpty}>
                        Nenhum documento de referência foi adicionado.
                    </li>
                ) : (
                    docsToDisplay.map(doc => (
                     <li 
                         key={doc.id} 
                         className={`${styles.docItem} ${selectedDocument?.id === doc.id ? styles.docActive : ''}`}
                         onClick={() => setSelectedDocument(doc)}
                     >
                         {doc.fileName}
                     </li>
                 ))
                )}
            </ul>

            {/* O Visualizador em si */}
            <div className={styles.contentArea}>
                {isLoading && <p>Carregando documento...</p>}
                {!selectedDocument && !isLoading && <p>Selecione um documento para visualizar.</p>}
                
                {/* Visualizador de PDF */}
                {fileUrl && (
                    <div className={styles.pdfViewer}>
                        <Document
                            file={fileUrl}
                            onLoadSuccess={onDocumentLoadSuccess}
                        >
                            <Page pageNumber={pageNumber} />
                        </Document>
                        
                        {/* Controles de Paginação */}
                        <div className={styles.paginationControls}>
                            <button 
                                className={styles.paginationButton}
                                onClick={goToPrevPage}
                                disabled={pageNumber <= 1}
                                title="Página Anterior"
                            >
                                ‹ Anterior
                            </button>
                            
                            <p className={styles.pageInfo}>
                                Página {pageNumber} de {numPages}
                            </p>
                            
                            <button 
                                className={styles.paginationButton}
                                onClick={goToNextPage}
                                disabled={pageNumber >= numPages}
                                title="Próxima Página"
                            >
                                Próxima ›
                            </button>
                        </div>
                    </div>
                )}
                {/* --- FIM DA MUDANÇA 2 --- */}
                
                {/* Visualizador de TXT (Não muda) */}
                {fileContent && (
                    <pre className={styles.txtViewer}>
                        {fileContent}
                    </pre>
                )}
            </div>
        </div>
    );
}