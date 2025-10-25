import type { PreAnalysisStage } from "../PreAnalysisStage";
import type { UploadedDocument } from "./UploadedDocument";

export interface AnalysisExecutionResult {
    referenceDocuments: UploadedDocument[];
    analysisDocuments: UploadedDocument[];
    preAnalysisResult: PreAnalysisStage;
    isSuccess: boolean;
    errorMessage: string;
    promptResult: string; // Isso parece ser o texto puro do Gemini, pode ser útil
}