import axios from 'axios';

/**
 * Define a estrutura do "dicionário" de erros que cada
 * página vai fornecer.
 */
export interface ApiErrorMap {
    // Para erros específicos do backend, ex: "invalid_credentials"
    byErrorCode?: {
        [key: string]: string;
    };
    // Para status codes genéricos, ex: 401, 500
    byStatusCode?: {
        [key: number]: string;
    };
    // A mensagem padrão se nenhuma outra for encontrada
    default: string;
}

/**
 * Processa um erro de API e o traduz usando um mapa fornecido.
 * @param error O erro (geralmente do 'catch')
 * @param map O mapa de tradução de erros da página
 * @returns Uma string amigável para o usuário
 */
export const handleApiError = (error: unknown, map: ApiErrorMap): string => {
    
    // Verifica se é um erro do Axios com uma resposta do servidor
    if (axios.isAxiosError(error) && error.response) {
        const status = error.response.status;
        const data = error.response.data;

        // 1. Tenta encontrar pelo código de erro específico do backend
        // (Ex: "invalid_password", "user_not_found")
        const backendErrorCode = data?.message || data?.error;
        if (backendErrorCode && map.byErrorCode?.[backendErrorCode]) {
            return map.byErrorCode[backendErrorCode];
        }

        // 2. Se não encontrar, tenta encontrar pelo Status Code (Ex: 401)
        if (map.byStatusCode?.[status]) {
            return map.byStatusCode[status];
        }
    }

    // 3. Se for qualquer outro tipo de erro (rede, JS) ou um
    // erro de API não mapeado, usa a mensagem padrão.
    return map.default;
};