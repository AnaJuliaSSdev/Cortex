import { useState, useEffect } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import 'react-pdf/dist/Page/TextLayer.css';

import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import api from '../services/api';
import styles from './css/DocumentViewer.module.css';
import type { IndexReference } from '../interfaces/IndexReference';

import workerSrc from 'pdfjs-dist/build/pdf.worker.min.mjs?url';
import { DocumentType } from '../interfaces/enum/DocumentType';
pdfjs.GlobalWorkerOptions.workerSrc = workerSrc;

interface DocumentViewerProps {
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    selectedReference: IndexReference | null;
    activeTab: 'analysis' | 'reference';
    onTabChange: (tab: 'analysis' | 'reference') => void;
}

export default function DocumentViewer({
    analysisDocuments,
    referenceDocuments,
    selectedReference,
    activeTab,
    onTabChange
}: DocumentViewerProps) {

    const [selectedDocument, setSelectedDocument] = useState<UploadedDocument | null>(null);
    const [fileUrl, setFileUrl] = useState<string | null>(null);
    const [fileContent, setFileContent] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [numPages, setNumPages] = useState<number>(0);
    const [pageNumber, setPageNumber] = useState(1);
    const [highlightText, setHighlightText] = useState<string | null>(null);

    // Função para limpar o highlight ao selecionar doc manualmente
    const handleSelectDocument = (doc: UploadedDocument) => {
        setSelectedDocument(doc);
        setPageNumber(1);
        setHighlightText(null);
    };

    useEffect(() => {
        setSelectedDocument(null);
        setPageNumber(1);
        setHighlightText(null);
    }, [activeTab]);

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
                if (selectedDocument.fileType === DocumentType.Text) {
                    const response = await api.get(`/documents/${selectedDocument.id}/content`);
                    setFileContent(typeof response.data === 'string' ? response.data : JSON.stringify(response.data));
                    setFileUrl(null);
                } 
                else {
                    const response = await api.get(`/documents/download/${selectedDocument.id}`, {
                        responseType: 'blob',
                    });

                    const blob: Blob = response.data;
                    const url = URL.createObjectURL(blob);
                    setFileUrl(url);
                    setFileContent(null);
                }

            } catch (error) {
                console.error("Erro ao carregar o documento:", error);
            } finally {
                setIsLoading(false);
            }
        };

        fetchDocument();

        return () => {
            if (fileUrl) {
                URL.revokeObjectURL(fileUrl);
            }
        };
    }, [selectedDocument]);

    // Efeito para pular para a página quando uma referência é clicada
    useEffect(() => {
        if (!selectedReference) {
            return;
        }

        const doc = [...analysisDocuments, ...referenceDocuments].find(
            d => d.gcsFilePath === selectedReference.sourceDocumentUri
        );

        if (!doc) {
            return;
        }

        // Normalizar e limpar o texto para highlight
        const textToHighlight = selectedReference.quotedContent?.trim() || null;
        setHighlightText(textToHighlight);

        // Se o documento for diferente, seleciona ele
        if (selectedDocument?.id !== doc.id) {
            setSelectedDocument(doc);
        } else {
            // Se for o mesmo documento, apenas pula para a página
            const page = parseInt(selectedReference.page, 10);
            if (!isNaN(page) && page > 0 && page <= numPages) {
                setPageNumber(page);
            }
        }
    }, [selectedReference, numPages, analysisDocuments, referenceDocuments, selectedDocument]);

    const onDocumentLoadSuccess = ({ numPages }: { numPages: number }) => {
        setNumPages(numPages);
        const page = selectedReference ? parseInt(selectedReference.page, 10) : 1;
        setPageNumber(isNaN(page) ? 1 : page);
    };

    const docsToDisplay = activeTab === 'analysis' ? analysisDocuments : referenceDocuments;

    const goToPrevPage = () => {
        setPageNumber(prevPageNumber => Math.max(prevPageNumber - 1, 1));
        setHighlightText(null);
    };

    const goToNextPage = () => {
        setPageNumber(prevPageNumber => Math.min(prevPageNumber + 1, numPages));
        setHighlightText(null);
    };

    return (
        <div className={styles.viewerContainer}>
            {/* Abas de Documentos */}
            <div className={styles.tabs}>
                <button
                    className={activeTab === 'analysis' ? styles.activeTab : ''}
                    onClick={() => onTabChange('analysis')}
                >
                    Análise ({analysisDocuments.length})
                </button>
                <button
                    className={activeTab === 'reference' ? styles.activeTab : ''}
                    onClick={() => onTabChange('reference')}
                >
                    Referência ({referenceDocuments.length})
                </button>
            </div>

            {/* Lista de Documentos */}
            <ul className={styles.docList}>
                {docsToDisplay.length === 0 ? (
                    <li className={styles.docItemEmpty}>
                        Nenhum documento disponível.
                    </li>
                ) : (
                    docsToDisplay.map(doc => (
                        <li
                            key={doc.id}
                            className={`${styles.docItem} ${selectedDocument?.id === doc.id ? styles.docActive : ''}`}
                            onClick={() => handleSelectDocument(doc)}
                        >
                            {doc.fileName}
                        </li>
                    ))
                )}
            </ul>

            {/* Área de Conteúdo */}
            <div className={styles.contentArea}>
                {isLoading && <p>Carregando documento...</p>}
                {!selectedDocument && !isLoading && <p>Selecione um documento para visualizar.</p>}

                {/* Visualizador de PDF */}
                {fileUrl && (
                    <div className={styles.pdfViewer}>
                        <Document
                            file={fileUrl}
                            onLoadSuccess={onDocumentLoadSuccess}
                            loading={<div>Carregando PDF...</div>}
                        >
                            <Page 
                                pageNumber={pageNumber} 
                                renderTextLayer={false} 
                                renderAnnotationLayer={false}
                                loading={<div>Carregando página...</div>}
                            />
                        </Document>

                        {/* Controles de Paginação */}
                        <div className={styles.paginationControls}>
                            <button
                                className={styles.paginationButton}
                                onClick={goToPrevPage}
                                disabled={pageNumber <= 1}
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
                            >
                                Próxima ›
                            </button>
                        </div>
                    </div>
                )}

                {/* Visualizador de TXT */}
                {fileContent && (
                    <pre className={styles.txtViewer}>
                        {getHighlightedTxtContent(fileContent, highlightText)}
                    </pre>
                )}
            </div>
        </div>
    );
}

// Helper para destacar conteúdo TXT
function getHighlightedTxtContent(content: string, highlight: string | null): React.ReactNode {
    if (!highlight || !content) return content;

    // Normalizar para busca case-insensitive
    const normalizedContent = content.toLowerCase();
    const normalizedHighlight = highlight.toLowerCase();
    const index = normalizedContent.indexOf(normalizedHighlight);

    if (index === -1) return content;

    // Pegar o texto original (com maiúsculas/minúsculas corretas)
    const before = content.substring(0, index);
    const highlighted = content.substring(index, index + highlight.length);
    const after = content.substring(index + highlight.length);

    return (
        <>
            {before}
            <mark className={styles.highlight}>{highlighted}</mark>
            {after}
        </>
    );
}