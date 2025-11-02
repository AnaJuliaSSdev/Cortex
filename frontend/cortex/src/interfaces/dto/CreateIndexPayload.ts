export interface CreateIndexPayload {
    preAnalysisStageId: number;
    indexName: string;
    indexDescription?: string;
    indicatorName: string;
}