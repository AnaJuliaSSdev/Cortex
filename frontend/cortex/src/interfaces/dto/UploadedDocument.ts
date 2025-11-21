import type { DocumentPurpose } from "../enum/DocumentPurpose";
import type { DocumentType } from "../enum/DocumentType";

export interface UploadedDocument {
    id: string; // ou number
    fileName: string;
    fileSize: number;
    contentType: string;
    fileType: DocumentType;
    gcsFilePath: string;
    purpose: DocumentPurpose;
    // Adicione outros campos que seu DTO de Documento retorna
}