import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { DocumentType } from '../interfaces/enum/DocumentType';

/**
 * Encontra o nome amigável do arquivo a partir do seu URI de armazenamento (GCS).
 * @param uri O URI completo do documento (ex: gs://...)
 * @param documents A lista completa de documentos disponíveis na análise
 * @returns O nome do arquivo (ex: "Entrevista.pdf") ou uma mensagem de fallback.
 */
export const getFileNameFromUri = (uri: string, documents: UploadedDocument[]): string => {
    if (!uri) return "Documento desconhecido";
    
    const doc = documents.find(d => d.gcsFilePath === uri);
    
    if (doc) {
        return formatDisplayFileName(doc.fileName, doc.fileType);
    }
    
    return "Documento não encontrado";
};

/**
 * Formata o nome do arquivo para exibição baseado no seu tipo original.
 * Se o arquivo for do tipo Texto mas estiver salvo como .pdf, mostramos .txt.
 */
export const formatDisplayFileName = (fileName: string, fileType: DocumentType): string => {
    if (!fileName) return "";

    // Se o tipo original for TEXTO, mas a extensão atual for .pdf (conversão do backend)
    // Trocamos visualmente para .txt para não confundir o usuário
    if (fileType === DocumentType.Text && fileName.toLowerCase().endsWith('.pdf')) {
        return fileName.slice(0, -4) + '.txt';
    }

    return fileName;
};

/**
 * Retorna o número da página para exibição.
 * - Se for TXT: Retorna sempre "1" (pois visualmente é contínuo).
 * - Se for PDF: Retorna o número real da página.
 * * @param uri URI do documento no GCS
 * @param page O número da página original salvo na referência
 * @param documents Lista de documentos para verificar o tipo
 */
export const getReferencePageLabel = (uri: string, page: string | number, documents: UploadedDocument[]): string => {
    const doc = documents.find(d => d.gcsFilePath === uri);

    // Se o documento for do tipo TEXTO, visualmente para o usuário é tudo "Página 1"
    // (mesmo que o backend tenha convertido para um PDF de múltiplas páginas)
    if (doc && doc.fileType === DocumentType.Text) {
        return "1";
    }

    // Caso contrário (PDF nativo), retorna a página real
    return String(page);
};