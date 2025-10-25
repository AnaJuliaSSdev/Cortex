import type { IndexReference } from "./IndexReference";
import type { Indicator } from "./Indicator";

export interface Index {
    id: number;
    name: string;
    description?: string;
    indicatorId: number;
    indicator: Indicator; // Objeto aninhado, como no seu modelo
    preAnalysisStageId: number;
    references: IndexReference[];
}