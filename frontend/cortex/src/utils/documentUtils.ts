import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';

/**
 * Encontra o nome amigável do arquivo a partir do seu URI de armazenamento (GCS).
 * @param uri O URI completo do documento (ex: gs://...)
 * @param documents A lista completa de documentos disponíveis na análise
 * @returns O nome do arquivo (ex: "Entrevista.pdf") ou uma mensagem de fallback.
 */
export const getFileNameFromUri = (uri: string, documents: UploadedDocument[]): string => {
    if (!uri) return "Documento desconhecido";
    console.log(documents)
    console.log(uri);
    const doc = documents.find(d => d.gcsFilePath === uri);
    console.log(doc)
    return doc ? doc.fileName : "Documento não encontrado";
};