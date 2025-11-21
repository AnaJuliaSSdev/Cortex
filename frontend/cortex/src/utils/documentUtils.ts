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