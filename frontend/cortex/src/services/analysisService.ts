import type { Analysis } from '../interfaces/Analysis';
import type { AnalysisDto } from '../interfaces/dto/AnalysisDto';
import type { AnalysisExecutionResult } from '../interfaces/dto/AnalysisExecutionResult';
import type { CreateAnalysisPayload } from '../interfaces/dto/CreateAnalysisPayload';
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
 * Busca todas as análises do usuário logado.
 * @returns Uma lista de análises.
 */
export const getAnalyses = async (): Promise<AnalysisDto[]> => {
    try {
        // Assumindo que a rota GET /Analysis retorna a lista (padrão REST)
        const response = await api.get<AnalysisDto[]>('/analysis');
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