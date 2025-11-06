import type { Analysis } from '../interfaces/Analysis';
import type { AnalysisDto } from '../interfaces/dto/AnalysisDto';
import type { AnalysisExecutionResult } from '../interfaces/dto/AnalysisExecutionResult';
import type { CreateAnalysisPayload } from '../interfaces/dto/CreateAnalysisPayload';
import type { CreateIndexPayload } from '../interfaces/dto/CreateIndexPayload';
import type { PaginatedResult, PaginationParams } from '../interfaces/dto/Pagination';
import type { UpdateIndexPayload } from '../interfaces/dto/UpdateIndexPayload';
import type { Index } from '../interfaces/Index';
import api from './api';

/**
 * Função para criar uma nova análise.
 * @param createDto - O objeto contendo o título da análise.
 * @returns A análise recém-criada.
 */
export const createAnalysis = async (createDto: CreateAnalysisPayload): Promise<Analysis> => {
    try {
        const response = await api.post<Analysis>('/Analysis', createDto); // Assumindo que a rota seja /api/Analyses
        return response.data;
    } catch (error) {
        console.error("Erro ao criar a análise:", error);
        // Você pode tratar o erro de forma mais robusta aqui (ex: retornando uma mensagem)
        throw error;
    }
};

/**
 * Salva a pergunta central de uma análise existente.
 * @param analysisId O ID da análise a ser atualizada.
 * @param question A pergunta central.
 */
export const postAnalysisQuestion = async (analysisId: string, question: string): Promise<void> => {
    // O backend espera um objeto com a chave "Question"
    const payload = { question };
    try {
        // Seu endpoint é /api/Analysis/{id}/question (assumindo controller "Analysis")
        await api.post(`/Analysis/${analysisId}/question`, payload);
    } catch (error) {
        console.error("Erro ao salvar a pergunta da análise:", error);
        throw error;
    }
};

/**
 * Dispara o início do processamento de uma análise no backend.
 * @param analysisId O ID da análise a ser iniciada.
 * @returns O resultado da execução da análise.
 */
export const startAnalysis = async (analysisId: string): Promise<any> => { 
    // O tipo 'any' pode ser trocado por uma interface mais específica 
    // para o seu `AnalysisExecutionResult` quando você a definir no frontend.
    try {
        const response = await api.post(`/Analysis/${analysisId}`);
        return response.data;
    } catch (error) {
        console.error("Erro ao iniciar a análise:", error);
        throw error;
    }
};

/**
 * Busca uma lista paginada de análises do usuário logado.
 * @returns Um objeto PaginatedResult com as análises.
 */
export const getAnalyses = async (params: PaginationParams): Promise<PaginatedResult<AnalysisDto>> => {
    try {
        const response = await api.get<PaginatedResult<AnalysisDto>>('/analysis', { 
            params: {
                pageNumber: params.pageNumber,
                pageSize: params.pageSize
            } 
        });
        return response.data;
    } catch (error) {
        console.error("Erro ao buscar as análises:", error);
        throw error;
    }
};

/**
 * Continua a análise para a próxima etapa (ex: Exploração).
 * @param analysisId O ID da análise a ser continuada.
 * @returns O objeto AnalysisExecutionResult atualizado.
 */
export const continueAnalysis = async (analysisId: string): Promise<AnalysisExecutionResult> => {
    try {
        const response = await api.post<AnalysisExecutionResult>(`/analysis/continue/${analysisId}`);
        return response.data;
    } catch (error) {
        console.error("Erro ao continuar a análise:", error);
        throw error;
    }
};

/**
 * Busca o estado completo de uma análise pelo seu ID.
 * @param analysisId O ID da análise a ser buscada.
 * @returns O objeto AnalysisExecutionResult com o estado atual.
 */
export const getAnalysisState = async (analysisId: string): Promise<AnalysisExecutionResult> => {
    try {
        const response = await api.get<AnalysisExecutionResult>(`/analysis/state/${analysisId}`);
        return response.data;
    } catch (error) {
        console.error(`Erro ao buscar o estado da análise ${analysisId}:`, error);
        throw error;
    }
};

/**
 * Cria um novo Índice (e seu Indicador) manualmente.
 * @param payload Os dados do novo índice
 * @returns O objeto Index recém-criado
 */
export const createIndex = async (payload: CreateIndexPayload): Promise<Index> => {
    try {
        const response = await api.post<Index>('/indexes/createManual', payload);
        return response.data;
    } catch (error) {
        console.error("Erro ao criar o índice:", error);
        throw error;
    }
};

/**
 * Atualiza um Índice (e seu Indicador) existente.
 * @param indexId O ID do índice a ser atualizado
 * @param payload Os novos dados do índice
 * @returns O objeto Index atualizado
 */
export const updateIndex = async (indexId: number, payload: UpdateIndexPayload): Promise<Index> => {
    try {
        const response = await api.put<Index>(`/indexes/${indexId}`, payload);
        return response.data;
    } catch (error) {
        console.error("Erro ao atualizar o índice:", error);
        throw error;
    }
};

/**
 * Exclui um Índice da análise.
 * @param indexId O ID do índice a ser excluído
 */
export const deleteIndex = async (indexId: number): Promise<void> => {
    try {
        await api.delete(`/indexes/${indexId}`);
    } catch (error) {
        console.error("Erro ao excluir o índice:", error);
        throw error;
    }
};

/**
 * Exclui permanentemente uma análise inteira.
 * @param analysisId O ID da análise a ser excluída
 */
export const deleteAnalysis = async (analysisId: number): Promise<void> => {
    try {
        await api.delete(`/analysis/${analysisId}`);
    } catch (error) {
        console.error(`Erro ao excluir a análise ${analysisId}:`, error);
        throw error;
    }
};


/**
 * Exclui um documento de uma análise.
 * @param documentId O ID do documento a ser excluído
 */
export const deleteDocument = async (documentId: string): Promise<void> => {
  try {
    // Chama o endpoint que você criou: [HttpDelete("documents/{documentId}")]
    await api.delete(`/Analysis/documents/${documentId}`);
  } catch (error) {
    console.error(`Erro ao excluir o documento ${documentId}:`, error);
    throw error; // Deixa o handler da página tratar
  }
};