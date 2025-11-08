import api from './api';
import type { ExportRequestDto } from '../interfaces/dto/ExportRequest';

/**
 * Chama o backend para gerar um PDF e dispara o download.
 * @param analysisId O ID da análise
 * @param payload O DTO contendo a imagem do gráfico e opções
 */
export const exportAnalysisToPdf = async (analysisId: string, payload: ExportRequestDto): Promise<void> => {
    try {
        const response = await api.post(
            `/Export/pdf/${analysisId}`, 
            payload,
            { responseType: 'blob' } // Diz ao Axios para esperar um arquivo
        );

        // 1. Criar um URL temporário para o arquivo (blob)
        const blob = new Blob([response.data], { type: 'application/pdf' });
        const url = window.URL.createObjectURL(blob);

        // 2. Tentar pegar o nome do arquivo do header (opcional, mas bom)
        let filename = `analise_${analysisId}.pdf`;
        const contentDisposition = response.headers['content-disposition'];
        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename="?(.+)"?/);
            if (filenameMatch && filenameMatch.length > 1) {
                filename = filenameMatch[1];
            }
        }

        // 3. Criar um link "invisível" e clicar nele para baixar
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', filename);
        document.body.appendChild(link);
        link.click();

        // 4. Limpar
        link.remove();
        window.URL.revokeObjectURL(url);

    } catch (error) {
        console.error("Erro ao exportar para PDF:", error);
        throw new Error("Falha ao gerar o PDF. Verifique o console.");
    }
};