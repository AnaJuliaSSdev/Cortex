import axios from 'axios';

const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL
});

api.interceptors.request.use(
    (config) => {
        // Pega o token do localStorage (onde o auth.login() deve salvar)
        const token = localStorage.getItem('authToken'); 
        
        // Se o token existir, adiciona ao cabeçalho de Authorization
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        
        return config; // Retorna a configuração modificada para o Axios continuar
    },
    (error) => {
        // Caso ocorra um erro ao configurar a requisição
        return Promise.reject(error);
    }
);

api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            localStorage.removeItem('authToken');
        }
        return Promise.reject(error);
    }
);

export default api;