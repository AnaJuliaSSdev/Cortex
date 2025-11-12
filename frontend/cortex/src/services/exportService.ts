import api from './api';
import type { ExportRequestDto } from '../interfaces/dto/ExportRequest';

/**
 * Helper genérico para disparar o download de um blob
 */
function triggerDownload(blob: Blob, filename: string) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
}

/**
 * Extrai o nome do arquivo do header 'content-disposition'
 */
function getFilenameFromHeader(headers: any, defaultName: string): string {
    const contentDisposition = headers['content-disposition'];
    if (contentDisposition) {
        const filenameMatch = contentDisposition.match(/filename="?(.+)"?/);
        if (filenameMatch && filenameMatch.length > 1) {
            return filenameMatch[1];
        }
    }
    return defaultName;
}

/**
 * Chama o backend para gerar um PDF e dispara o download.
 */
export const exportAnalysisToPdf = async (analysisId: string, payload: ExportRequestDto): Promise<void> => {
    try {
        const response = await api.post(
            `/Export/pdf/${analysisId}`, 
            payload,
            { responseType: 'blob' } // Diz ao Axios para esperar um arquivo
        );

        const blob = new Blob([response.data], { type: 'application/pdf' });
        const filename = getFilenameFromHeader(response.headers, `analise_${analysisId}.pdf`);
        triggerDownload(blob, filename);

    } catch (error) {
        console.error("Erro ao exportar para PDF:", error);
        throw new Error("Falha ao gerar o PDF. Verifique o console.");
    }
};

/**
 * Chama o backend para gerar um LaTeX (.tex) e dispara o download.
 */
export const exportAnalysisToLatex = async (analysisId: string, payload: ExportRequestDto): Promise<void> => {
    try {
        const response = await api.post(
            `/Export/latex/${analysisId}`,
            payload,
            { responseType: 'blob' }
        );

        const blob = new Blob([response.data], { type: 'application/x-tex' }); 
        const filename = getFilenameFromHeader(response.headers, `analise_${analysisId}.tex`);
        triggerDownload(blob, filename);

    } catch (error) {
        console.error("Erro ao exportar para LaTeX:", error);
        throw new Error("Falha ao gerar o arquivo .tex. Verifique o console.");
    }
};