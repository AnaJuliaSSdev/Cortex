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
            
            // Se a referência for do documento já selecionado
            if (doc && selectedDocument && doc.id === selectedDocument.id) {
                const page = parseInt(selectedReference.page, 10);
                if (!isNaN(page) && page > 0 && page <= numPages) {
                    setPageNumber(page);
                    // Futuro: Adicionar lógica de scroll/highlight
                }
            } else if (doc) {
                // Se a referência for de OUTRO documento, selecione-o
                setSelectedDocument(doc);
                // O useEffect acima vai carregar o doc, e este useEffect rodará de novo
            }
        }
    }, [selectedReference, numPages, analysisDocuments, referenceDocuments, selectedDocument]);

    const onDocumentLoadSuccess = ({ numPages }: { numPages: number }) => {
        setNumPages(numPages);
        // Se a referência já estiver selecionada, pule para a página
        const page = selectedReference ? parseInt(selectedReference.page, 10) : 1;
        setPageNumber(isNaN(page) ? 1 : page);
    };

    const docsToDisplay = activeTab === 'analysis' ? analysisDocuments : referenceDocuments;

    return (
        <div className={styles.viewerContainer}>
            {/* Abas de Documentos */}
            <div className={styles.tabs}>
                <button 
                    className={activeTab === 'analysis' ? styles.activeTab : ''}
                    onClick={() => setActiveTab('analysis')}
                >
                    Análise ({analysisDocuments.length})
                </button>
                <button 
                    className={activeTab === 'reference' ? styles.activeTab : ''}
                    onClick={() => setActiveTab('reference')}
                >
                    Referência ({referenceDocuments.length})
                </button>
            </div>

            {/* Lista de Documentos da Aba */}
            <ul className={styles.docList}>
                {docsToDisplay.map(doc => (
                    <li 
                        key={doc.id} 
                        className={`${styles.docItem} ${selectedDocument?.id === doc.id ? styles.docActive : ''}`}
                        onClick={() => setSelectedDocument(doc)}
                    >
                        {doc.fileName}
                    </li>
                ))}
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
                        <p className={styles.pageInfo}>
                            Página {pageNumber} de {numPages}
                        </p>
                    </div>
                )}
                
                {/* Visualizador de TXT */}
                {fileContent && (
                    <pre className={styles.txtViewer}>
                        {fileContent}
                    </pre>
                )}
            </div>
        </div>
    );
}