import type { DocumentPurpose } from "../enum/DocumentPurpose";

export interface UploadedDocument {
    id: string; // ou number
    fileName: string;
    fileSize: number;
    contentType: string;
    fileType: string;
    gcsFilePath: string;
    purpose: DocumentPurpose;
    // Adicione outros campos que seu DTO de Documento retorna
}