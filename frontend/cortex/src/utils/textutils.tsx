import styles from '../components/css/DocumentViewer.module.css';

export const removeAccents = (str: string): string => {
    return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

// Normaliza removendo acentos, convertendo para minúsculo, e colapsando whitespace
export const normalize = (str: string): string =>  {
    return removeAccents(str)
        .toLowerCase()
        .replace(/[\r\n]+/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

export const highlightTxt = (content: string, search: string | null): React.ReactNode =>  {
    if (!search || !content) return content;

    // Estratégia 1: Match exato (case insensitive)
    let idx = content.toLowerCase().indexOf(search.toLowerCase());
    if (idx !== -1) {
        return renderHighlight(content, idx, idx + search.length);
    }

    // Estratégia 2: Sem acentos
    const contentNorm = removeAccents(content.toLowerCase());
    const searchNorm = removeAccents(search.toLowerCase());
    idx = contentNorm.indexOf(searchNorm);
    if (idx !== -1) {
        return renderHighlight(content, idx, idx + search.length);
    }

    // Estratégia 3: Ignorando quebras de linha (normaliza whitespace)
    // Cria mapeamento: posição normalizada -> posição original
    const mapping: number[] = [];
    let normalized = '';

    for (let i = 0; i < content.length; i++) {
        const char = content[i];
        if (/\s/.test(char)) {
            // Se o último char normalizado não é espaço, adiciona um
            if (normalized.length > 0 && normalized[normalized.length - 1] !== ' ') {
                normalized += ' ';
                mapping.push(i);
            }
            // Pula whitespaces adicionais
        } else {
            normalized += removeAccents(char.toLowerCase());
            mapping.push(i);
        }
    }

    const searchClean = normalize(search);
    idx = normalized.indexOf(searchClean);

    if (idx !== -1) {
        // Mapeia de volta para posição original
        const startOrig = mapping[idx] ?? 0;
        // Encontra o fim: avança no original até cobrir todo o match
        let endIdx = idx + searchClean.length - 1;
        const endOrig = (mapping[endIdx] ?? content.length - 1) + 1;

        return renderHighlight(content, startOrig, endOrig);
    }

    // Estratégia 4: Primeiras palavras
    const words = search.split(/\s+/).filter(w => w.length > 2);
    if (words.length >= 2) {
        const partial = normalize(words.slice(0, 3).join(' '));
        const partialIdx = normalized.indexOf(partial);
        if (partialIdx !== -1) {
            const startOrig = mapping[partialIdx] ?? 0;
            // Estima o fim baseado no tamanho original
            const endOrig = Math.min(startOrig + search.length + 20, content.length);
            return renderHighlight(content, startOrig, endOrig);
        }
    }

    return content;
}


export const renderHighlight = (content: string, start: number, end: number): React.ReactNode => {
    return (
        <>
            {content.substring(0, start)}
            <mark className={styles.highlight}>{content.substring(start, end)}</mark>
            {content.substring(end)}
        </>
    );
}