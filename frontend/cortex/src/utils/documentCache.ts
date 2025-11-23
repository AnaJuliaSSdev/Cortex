interface CacheEntry {
    type: 'pdf' | 'text';
    url?: string;
    content?: string;
    timestamp: number;
}

const MAX_CACHE = 10;
// O objeto cache vive no escopo do módulo, então ele persiste
// enquanto a aplicação (SPA) estiver rodando, mesmo trocando de componentes.
const cache: Record<string, CacheEntry> = {};

export const cacheSet = (id: string, data: Omit<CacheEntry, 'timestamp'>) => {
    const keys = Object.keys(cache);
    
    // Limpeza LRU (Least Recently Used) se atingir o limite
    if (keys.length >= MAX_CACHE) {
        const oldest = keys.reduce((a, b) => cache[a].timestamp < cache[b].timestamp ? a : b);
        
        // Importante: Revogar a URL do Blob para evitar vazamento de memória no navegador
        if (cache[oldest]?.url) {
            URL.revokeObjectURL(cache[oldest].url!);
        }
        
        delete cache[oldest];
    }
    
    cache[id] = { ...data, timestamp: Date.now() };
};

export const cacheGet = (id: string): CacheEntry | null => {
    if (cache[id]) {
        // Atualiza o timestamp para indicar que foi usado recentemente
        cache[id].timestamp = Date.now();
        return cache[id];
    }
    return null;
};