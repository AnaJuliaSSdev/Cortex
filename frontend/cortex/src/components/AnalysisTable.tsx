import React from 'react';
import { useNavigate } from 'react-router-dom';
import styles from './css/AnalysisTable.module.css'; // Vamos criar este CSS
import type { AnalysisDto } from '../interfaces/dto/AnalysisDto';
import { AnalysisStatus } from '../interfaces/enum/AnalysisStatus';

interface AnalysisTableProps {
    analyses: AnalysisDto[];
}

// Componente "filho" para o Status, para darmos cor a ele
const StatusBadge: React.FC<{ status: AnalysisStatus }> = ({ status }) => {
    let text = 'Desconhecido';
    let className = '';

    switch (status) {
        case AnalysisStatus.Draft:
            text = 'Rascunho';
            className = styles.statusDraft;
            break;
        case AnalysisStatus.Running:
            text = 'Em Progresso';
            className = styles.statusProgress;
            break;
        case AnalysisStatus.Completed:
            text = 'Concluído';
            className = styles.statusCompleted;
            break;
        case AnalysisStatus.Failed:
            text = 'Erro';
            className = styles.statusError;
            break;
    }

    return <span className={`${styles.statusBadge} ${className}`}>{text}</span>;
};

export default function AnalysisTable({ analyses }: AnalysisTableProps) {
    const navigate = useNavigate();

    // Navega para a página de detalhes da análise
    const handleRowClick = (id: number) => {
        navigate(`/analysis/${id}`);
    };

    // Formata a data para o padrão pt-BR
    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        });
    };

    return (
        <div className={styles.tableContainer}>
            {/* Cabeçalho da Tabela */}
            <header className={styles.tableHeader}>
                <div className={styles.colTitle}>Título</div>
                <div className={styles.colStatus}>Status</div>
                <div className={styles.colCount}>Documentos</div>
                <div className={styles.colDate}>Criado em</div>
            </header>

            {/* Linhas da Tabela */}
            <ul className={styles.tableList}>
                {analyses.map((analysis) => (
                    <li 
                        key={analysis.id} 
                        className={styles.tableRow}
                        onClick={() => handleRowClick(analysis.id)}
                    >
                        <div className={styles.colTitle}>{analysis.title}</div>
                        <div className={styles.colStatus}>
                            <StatusBadge status={analysis.status} />
                        </div>
                        <div className={styles.colCount}>{analysis.documentsCount}</div>
                        <div className={styles.colDate}>{formatDate(analysis.createdAt)}</div>
                    </li>
                ))}
            </ul>
        </div>
    );
}