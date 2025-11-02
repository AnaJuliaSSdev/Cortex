import type { PreAnalysisStage } from "../PreAnalysisStage";
import type { ExplorationOfMaterialStage } from "./AnalysisResult";
import type { UploadedDocument } from "./UploadedDocument";

export interface AnalysisExecutionResult {
    referenceDocuments: UploadedDocument[];
    analysisDocuments: UploadedDocument[];
    preAnalysisResult?: PreAnalysisStage;
    explorationOfMaterialStage?: ExplorationOfMaterialStage;
    isSuccess: boolean;
    errorMessage: string;
    promptResult: string;
    analysisTitle: string;
    analysisQuestion: string;
}