import type { Index } from "../interfaces/Index";
import type { PreAnalysisStage } from "../interfaces/PreAnalysisStage";
import styles from './css/PreAnalysisResults.module.css';

interface PreAnalysisResultsProps {
    preAnalysisResult: PreAnalysisStage;
}

// Um componente "filho" para renderizar cada item da lista
const IndexItem: React.FC<{ index: Index }> = ({ index }) => {
    return (
        <li className={styles.indexItem}>
            <div className={styles.indexContent}>
                <span className={styles.indicatorName}>{index.indicator.name}</span>
                <h3 className={styles.indexName}>{index.name}</h3>
                {index.description && <p className={styles.indexDescription}>{index.description}</p>}
                
                {/* Informação sobre as referências (para o futuro) */}
                <span className={styles.referenceInfo}>
                    {index.references.length} {index.references.length === 1 ? 'referência' : 'referências'} encontradas
                </span>
            </div>
            
            {/* Ações (para os próximos passos que você mencionou) */}
            <div className={styles.indexActions}>
                <button title="Editar" className={styles.actionButton}>✏️</button>
                <button title="Excluir" className={styles.actionButton}>🗑️</button>
            </div>
        </li>
    );
};


const PreAnalysisResults: React.FC<PreAnalysisResultsProps> = ({ preAnalysisResult }) => {
    
    // Agrupar índices por indicador (opcional, mas bom para visualização)
    // Vamos fazer simples por enquanto: uma lista reta.
    
    const { indexes } = preAnalysisResult;

    return (
        <section className={styles.resultsContainer}>
            <div className={styles.header}>
                <h2 className={styles.title}>Resultados da Pré-Análise</h2>
                <button className={styles.primaryButton}>+ Adicionar Novo Índice</button>
            </div>
            
            {indexes.length > 0 ? (
                <ul className={styles.indexList}>
                    {indexes.map((index) => (
                        <IndexItem key={index.id} index={index} />
                    ))}
                </ul>
            ) : (
                <p>Nenhum índice foi extraído automaticamente. Você pode adicioná-los manualmente.</p>
            )}
            
            <footer className={styles.footer}>
                <button className={styles.primaryButton}>Confirmar Índices e Avançar</button>
            </footer>
        </section>
    );
};

export default PreAnalysisResults;