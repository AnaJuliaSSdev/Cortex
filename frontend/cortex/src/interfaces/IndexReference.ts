export interface IndexReference {
    id: number;
    indexId: number;
    sourceDocumentUri: string; // O URI/ID do arquivo
    page: string;
    line: string;
    quotedContent?: string;
}