import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5062/api',
});

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