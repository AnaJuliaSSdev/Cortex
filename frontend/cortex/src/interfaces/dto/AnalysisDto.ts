import type { AnalysisStatus } from "../enum/AnalysisStatus";

export interface AnalysisDto {
    id: number;
    title: string;
    status: AnalysisStatus;
    createdAt: string; // Datas em JSON são strings (ISO 8601)
    updatedAt?: string;
    documentsCount: number;
    question?: string;
}