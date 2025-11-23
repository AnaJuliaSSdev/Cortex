import { useState, useEffect, useRef } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import 'react-pdf/dist/Page/TextLayer.css';

import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import api from '../services/api';
import styles from './css/DocumentViewer.module.css';
import type { IndexReference } from '../interfaces/IndexReference';

import workerSrc from 'pdfjs-dist/build/pdf.worker.min.mjs?url';
import { DocumentType } from '../interfaces/enum/DocumentType';
import { highlightTxt } from '../utils/textutils';
import { cacheGet, cacheSet } from '../utils/documentCache';
import { formatDisplayFileName } from '../utils/documentUtils';
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

    const txtContainerRef = useRef<HTMLPreElement>(null);

    // Reset ao mudar aba
    useEffect(() => {
        setSelectedDocument(null);
        setPageNumber(1);
        setHighlightText(null);
    }, [activeTab]);

    const handleSelectDocument = (doc: UploadedDocument) => {
        setSelectedDocument(doc);
        setPageNumber(1);
        setHighlightText(null);
    };

    // Carregar documento (com cache)
    useEffect(() => {
        if (!selectedDocument) {
            setFileUrl(null);
            setFileContent(null);
            return;
        }

        const id = String(selectedDocument.id);
        const cached = cacheGet(id);

        if (cached) {
            setFileContent(cached.content || null);
            setFileUrl(cached.url || null);
            setIsLoading(false);
            return;
        }

        const load = async () => {
            setIsLoading(true);
            try {
                if (selectedDocument.fileType === DocumentType.Text) {
                    const res = await api.get(`/documents/${selectedDocument.id}/content`);
                    const content = typeof res.data === 'string' ? res.data : JSON.stringify(res.data);
                    setFileContent(content);
                    setFileUrl(null);
                    cacheSet(id, { type: 'text', content });
                } else {
                    const res = await api.get(`/documents/download/${selectedDocument.id}`, { responseType: 'blob' });
                    const url = URL.createObjectURL(res.data);
                    setFileUrl(url);
                    setFileContent(null);
                    cacheSet(id, { type: 'pdf', url });
                }
            } catch (e) {
                console.error('Erro ao carregar:', e);
            } finally {
                setIsLoading(false);
            }
        };
        load();
    }, [selectedDocument]);

    // Navegação por referência
    useEffect(() => {
        if (!selectedReference) return;

        const doc = [...analysisDocuments, ...referenceDocuments].find(
            d => d.gcsFilePath === selectedReference.sourceDocumentUri
        );
        if (!doc) return;

        const text = selectedReference.quotedContent?.trim() || null;
        setHighlightText(text);

        // Pega a página da referência
        const targetPage = parseInt(selectedReference.page, 10);

        if (selectedDocument?.id !== doc.id) {
            // Documento diferente: seleciona e define página inicial
            setSelectedDocument(doc);
            // A página será setada no onDocumentLoadSuccess
        } else {
            // Mesmo documento: apenas muda a página se for PDF
            if (doc.fileType !== DocumentType.Text && !isNaN(targetPage) && targetPage > 0) {
                // Força mudança de página mesmo se for a mesma (para re-trigger o highlight)
                if (targetPage <= numPages) {
                    setPageNumber(targetPage);
                }
            }
        }
    }, [selectedReference, analysisDocuments, referenceDocuments]);

    // Scroll para highlight no TXT
    useEffect(() => {
        if (selectedDocument?.fileType === DocumentType.Text && highlightText && txtContainerRef.current) {
            setTimeout(() => {
                const mark = txtContainerRef.current?.querySelector('mark');
                if (mark) mark.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }, 150);
        }
    }, [fileContent, highlightText, selectedDocument]);

    const onDocumentLoadSuccess = ({ numPages: n }: { numPages: number }) => {
        setNumPages(n);
        if (selectedReference) {
            const p = parseInt(selectedReference.page, 10);
            if (!isNaN(p) && p > 0 && p <= n) {
                setPageNumber(p);
                return;
            }
        }
        setPageNumber(1);
    };

    const docs = activeTab === 'analysis' ? analysisDocuments : referenceDocuments;

    return (
        <div className={styles.viewerContainer}>
            <div className={styles.tabs}>
                <button className={activeTab === 'analysis' ? styles.activeTab : ''} onClick={() => onTabChange('analysis')}>
                    Análise ({analysisDocuments.length})
                </button>
                <button className={activeTab === 'reference' ? styles.activeTab : ''} onClick={() => onTabChange('reference')}>
                    Referência ({referenceDocuments.length})
                </button>
            </div>

            <ul className={styles.docList}>
                {docs.length === 0 ? (
                    <li className={styles.docItemEmpty}>Nenhum documento.</li>
                ) : docs.map(doc => (
                    <li
                        key={doc.id}
                        className={`${styles.docItem} ${selectedDocument?.id === doc.id ? styles.docActive : ''}`}
                        onClick={() => handleSelectDocument(doc)}
                    >
                        {formatDisplayFileName(doc.fileName, doc.fileType)}
                    </li>
                ))}
            </ul>

            <div className={styles.contentArea}>
                {isLoading && <p>Carregando...</p>}
                {!selectedDocument && !isLoading && <p>Selecione um documento.</p>}

                {fileUrl && (
                    <div className={styles.pdfViewer}>
                            <Document file={fileUrl} onLoadSuccess={onDocumentLoadSuccess} loading={<div>Carregando PDF...</div>}>
                                <Page
                                    key={`page-${pageNumber}`}
                                    pageNumber={pageNumber}
                                    renderTextLayer={false}
                                    renderAnnotationLayer={false}
                                    loading={<div>Renderizando...</div>}
                                />
                            </Document>
                        <div className={styles.paginationControls}>
                            <button
                                className={styles.paginationButton}
                                onClick={() => { setPageNumber(p => Math.max(1, p - 1))}}
                                disabled={pageNumber <= 1}
                            >
                                ‹ Anterior
                            </button>
                            <p className={styles.pageInfo}>
                                Página {pageNumber} de {numPages}
                            </p>
                            <button
                                className={styles.paginationButton}
                                onClick={() => { setPageNumber(p => Math.min(numPages, p + 1))}}
                                disabled={pageNumber >= numPages}
                            >
                                Próxima ›
                            </button>
                        </div>
                    </div>
                )}

                {fileContent && (
                    <pre className={styles.txtViewer} ref={txtContainerRef}>
                        {highlightTxt(fileContent, highlightText)}
                    </pre>
                )}
            </div>
        </div>
    );
}