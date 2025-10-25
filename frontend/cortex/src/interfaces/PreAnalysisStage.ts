import type { Index } from "./Index";

export interface PreAnalysisStage {
    id: number; 
    analysisId: number,
    indexes: Index[];
}