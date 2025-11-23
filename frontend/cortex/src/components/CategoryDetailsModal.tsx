import { useMemo } from 'react';
import styles from './css/CategoryDetailsModal.module.css';
import type { Category } from '../interfaces/dto/AnalysisResult';
import CloseIcon from '@mui/icons-material/Close';
import TextSnippetIcon from '@mui/icons-material/TextSnippet';
import LabelIcon from '@mui/icons-material/Label';
import FindInPageIcon from '@mui/icons-material/FindInPage';
import type { Index } from '../interfaces/Index';
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { getFileNameFromUri, getReferencePageLabel } from '../utils/documentUtils';

interface CategoryDetailsModalProps {
    isOpen: boolean;
    onClose: () => void;
    category: Category | null;
    allDocuments: UploadedDocument[];
}

export default function CategoryDetailsModal({ isOpen, onClose, category, allDocuments}: CategoryDetailsModalProps) {
    if (!isOpen || !category) return null;

    // 1. Extrair índices únicos presentes nesta categoria para o sumário
    const uniqueIndices = useMemo(() => {
        const indicesMap = new Map<number, Index>();
        category.registerUnits.forEach(unit => {
            unit.foundIndices.forEach(index => {
                if (!indicesMap.has(index.id)) {
                    indicesMap.set(index.id, index);
                }
            });
        });
        return Array.from(indicesMap.values());
    }, [category]);

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={e => e.stopPropagation()}>
                
                {/* Header Fixo */}
                <div className={styles.header}>
                    <div>
                        <h2 className={styles.title}>{category.name}</h2>
                        <span className={styles.subtitle}>
                            {category.frequency} ocorrências totais
                        </span>
                    </div>
                    <button className={styles.closeButton} onClick={onClose}>
                        <CloseIcon />
                    </button>
                </div>

                {/* Conteúdo Rolável */}
                <div className={styles.content}>
                    
                    {/* Seção 1: Definição */}
                    <section className={styles.section}>
                        <h3 className={styles.sectionTitle}>Definição da Categoria</h3>
                        <p className={styles.definition}>{category.definition}</p>
                    </section>

                    {/* Seção 2: Índices Utilizados (Sumário) */}
                    <section className={styles.section}>
                        <h3 className={styles.sectionTitle}>
                            <LabelIcon sx={{ fontSize: 20, verticalAlign: 'middle', marginRight: 1 }}/>
                            Índices Presentes ({uniqueIndices.length})
                        </h3>
                        <div className={styles.indicesGrid}>
                            {uniqueIndices.map(index => (
                                <div key={index.id} className={styles.indexCard}>
                                    <div className={styles.indexHeader}>
                                        <span className={styles.indexName}>{index.name}</span>
                                        <span className={styles.indicatorTag}>{index.indicator.name}</span>
                                    </div>
                                    {index.description && (
                                        <p className={styles.indexDescription}>{index.description}</p>
                                    )}
                                </div>
                            ))}
                        </div>
                    </section>

                    {/* Seção 3: Unidades de Registro (Dados Brutos) */}
                    <section className={styles.section}>
                        <h3 className={styles.sectionTitle}>
                            <TextSnippetIcon sx={{ fontSize: 20, verticalAlign: 'middle', marginRight: 1 }}/>
                            Unidades de Registro ({category.registerUnits.length})
                        </h3>
                        <div className={styles.unitsList}>
                            {category.registerUnits.map((unit, idx) => (
                                <div key={unit.id || idx} className={styles.unitItem}>
                                    <blockquote className={styles.unitText}>
                                        "{unit.text}"
                                    </blockquote>
                                    
                                    <div className={styles.unitMeta}>
                                        <div className={styles.unitSource}>
                                            <FindInPageIcon sx={{ fontSize: 16, marginRight: 0.5 }}/>
                                            <span title={getFileNameFromUri(unit.sourceDocumentUri, allDocuments)}>
                                                {getFileNameFromUri(unit.sourceDocumentUri, allDocuments)} 
                                                {' '}(p. {getReferencePageLabel(unit.sourceDocumentUri, unit.page, allDocuments)})
                                            </span>
                                        </div>
                                        
                                        {/* Tags dos índices encontrados NESTA unidade */}
                                        <div className={styles.unitIndices}>
                                            {unit.foundIndices.map(idx => (
                                                <span key={idx.id} className={styles.unitIndexTag} title={idx.indicator.name}>
                                                    #{idx.name}
                                                </span>
                                            ))}
                                        </div>
                                    </div>

                                    {unit.justification && (
                                        <div className={styles.justification}>
                                            <strong>Justificativa: </strong> {unit.justification}
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </section>

                </div>
            </div>
        </div>
    );
}