// ... (suas interfaces Index, Indicator, etc. existentes)

import type { Index } from "../Index";

// Corresponde ao seu Cortex.Models.RegisterUnit
export interface RegisterUnit {
    id: number;
    text: string;
    sourceDocumentUri: string;
    page: string;
    line: string;
    justification: string;
    categoryId: number;
    foundIndices: Index[]; // A lista de Índices que esta unidade contém
}

// Corresponde ao seu Cortex.Models.Category
export interface Category {
    id: number;
    name: string;
    definition: string;
    frequency: number; // Contagem das unidades de registro
    explorationOfMaterialStageId: number;
    registerUnits: RegisterUnit[];
}

// Corresponde ao seu Cortex.Models.ExplorationOfMaterialStage
export interface ExplorationOfMaterialStage {
    id: number;
    analysisId: number;
    categories: Category[];
}