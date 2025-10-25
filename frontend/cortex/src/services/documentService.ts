import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import type { DocumentPurpose } from '../interfaces/enum/DocumentPurpose';
import api from './api';

/**
 * Função para fazer upload de um único arquivo.
 * @param analysisId O ID da análise à qual o documento pertence.
 * @param file O objeto File (do input).
 * @param purpose O propósito do documento (Analysis = 1, Reference = 2).
 * @returns O objeto UploadedDocument vindo do backend.
 */
export const uploadDocument = async (
    analysisId: string,
    file: File,
    purpose: DocumentPurpose
): Promise<UploadedDocument> => {
    
    const formData = new FormData();

    // Adiciona os campos EXATAMENTE como no CreateDocumentDto do C#
    formData.append('File', file);
    formData.append('Purpose', String(purpose));
    formData.append('Title', file.name); // Usar o nome do arquivo como título é um bom padrão inicial.
    // O campo 'Source' é opcional no DTO, então não precisamos adicioná-lo por enquanto.

    try {
        // A rota é /api/Documents/upload/{analysisId}
        const response = await api.post<UploadedDocument>(`/Documents/upload/${analysisId}`, formData, {
            headers: {
                // O header 'Content-Type' é definido automaticamente pelo navegador 
                // para 'multipart/form-data' quando se usa FormData, então não é obrigatório
                // declará-lo aqui, mas não atrapalha.
            },
        });
        
        return response.data;
    
    } catch (error) {
        console.error(`Erro no upload do arquivo ${file.name}:`, error);
        throw new Error('Falha no upload do arquivo.');
    }
};