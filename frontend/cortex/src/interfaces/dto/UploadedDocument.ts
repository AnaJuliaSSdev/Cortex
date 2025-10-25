export interface UploadedDocument {
    id: string; // ou number
    fileName: string;
    fileSize: number;
    contentType: string;
    // Adicione outros campos que seu DTO de Documento retorna
}