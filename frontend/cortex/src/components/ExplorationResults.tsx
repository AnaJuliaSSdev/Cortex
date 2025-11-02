import { useMemo, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import styles from './css/ExplorationResults.module.css'
import type { ExplorationOfMaterialStage } from '../interfaces/dto/AnalysisResult';
import AutoGraphIcon from '@mui/icons-material/AutoGraph';
import SpaceDashboardIcon from '@mui/icons-material/SpaceDashboard';
import DescriptionIcon from '@mui/icons-material/Description';
import TextSnippetIcon from '@mui/icons-material/TextSnippet';

interface ExplorationResultsProps {
    explorationStage: ExplorationOfMaterialStage;
}

type ViewMode = 'chart' | 'table' | 'cards';

const CHART_COLORS = [
    '#B35848', '#4A4644', '#8E8883', '#5a8d87', '#c7a97f',
    '#d4a5a5', '#7b9e89', '#9b6b6b', '#6b8e9b', '#8e9b6b'
];

interface ProcessedData {
    categoryName: string;
    indices: { name: string; count: number; color: string }[];
    totalUnits: number;
}

export default function ExplorationResults({ explorationStage }: ExplorationResultsProps) {

    const [viewMode, setViewMode] = useState<ViewMode>('chart');

    const processedData = useMemo(() => {
        const data: ProcessedData[] = [];
        const allIndicesMap = new Map<string, number>();

        explorationStage.categories.forEach(category => {
            const indexCounts = new Map<string, number>();

            category.registerUnits.forEach(unit => {
                unit.foundIndices.forEach(index => {
                    indexCounts.set(index.name, (indexCounts.get(index.name) || 0) + 1);
                    allIndicesMap.set(index.name, (allIndicesMap.get(index.name) || 0) + 1);
                });
            });

            const indices = Array.from(indexCounts.entries())
                .map(([name, count], idx) => ({
                    name,
                    count,
                    color: CHART_COLORS[idx % CHART_COLORS.length]
                }))
                .sort((a, b) => b.count - a.count);

            data.push({
                categoryName: category.name,
                indices,
                totalUnits: category.registerUnits.length
            });
        });

        return { categories: data, allIndices: Array.from(allIndicesMap.entries()) };
    }, [explorationStage]);

    // Dados para o gráfico de barras agrupadas
    const chartData = useMemo(() => {
        return processedData.categories.map(cat => {
            const entry: any = { category: cat.categoryName };
            cat.indices.forEach(idx => {
                entry[idx.name] = idx.count;
            });
            return entry;
        });
    }, [processedData]);

    const allIndexNames = useMemo(() => {
        const names = new Set<string>();
        processedData.categories.forEach(cat => {
            cat.indices.forEach(idx => names.add(idx.name));
        });
        return Array.from(names);
    }, [processedData]);

    // Mapa de índices com suas descrições
    const indexDescriptions = useMemo(() => {
        const map = new Map<string, string>();
        explorationStage.categories.forEach(cat => {
            cat.registerUnits.forEach(unit => {
                unit.foundIndices.forEach(index => {
                    if (index.description && !map.has(index.name)) {
                        map.set(index.name, index.description);
                    }
                });
            });
        });
        return map;
    }, [explorationStage]);

    return (
        <div className={styles.container}>
            {/* Header com controles */}
            <div className={styles.header}>
                <div>
                    <h2 className={styles.title}>Resultados da Exploração</h2>
                    <p className={styles.summaryText}>
                        {processedData.categories.length} categorias • {processedData.allIndices.length} índices únicos
                    </p>
                </div>

                {/* Botões de visualização */}
                <div className={styles.viewControls}>
                    <button
                        onClick={() => setViewMode('chart')}
                        className={`${styles.viewButton} ${viewMode === 'chart' ? styles.activeButton : ''}`}
                    >
                        <AutoGraphIcon/> Gráfico    
                    </button>
                    <button
                        onClick={() => setViewMode('table')}
                        className={`${styles.viewButton} ${viewMode === 'table' ? styles.activeButton : ''}`}
                    >
                        <SpaceDashboardIcon/> Tabela
                    </button>
                    <button
                        onClick={() => setViewMode('cards')}
                        className={`${styles.viewButton} ${viewMode === 'cards' ? styles.activeButton : ''}`}
                    >
                        <DescriptionIcon/> Cards
                    </button>
                </div>
            </div>

            {/* Visualização de Gráfico */}
            {viewMode === 'chart' && (
                <div className={styles.chartContainer}>
                    <ResponsiveContainer width="100%" height={500}>
                        <BarChart
                            data={chartData}
                            margin={{ top: 20, right: 30, left: 20, bottom: 100 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="#EFEBE6" />
                            <XAxis
                                dataKey="category"
                                angle={-45}
                                textAnchor="end"
                                height={100}
                                stroke="#4A4644"
                                style={{ fontSize: '0.85rem' }}
                            />
                            <YAxis
                                allowDecimals={false}
                                label={{ value: 'Frequência', angle: -90, position: 'insideLeft' }}
                                stroke="#4A4644"
                            />
                            <Tooltip
                                contentStyle={{
                                    background: '#FBFBF8',
                                    border: '1px solid #EFEBE6',
                                    borderRadius: '8px',
                                    padding: '1rem'
                                }}
                            />
                            <Legend
                                wrapperStyle={{ paddingTop: '20px' }}
                                layout="horizontal"
                                verticalAlign="bottom"
                            />
                            {allIndexNames.map((indexName, i) => (
                                <Bar
                                    key={indexName}
                                    dataKey={indexName}
                                    fill={CHART_COLORS[i % CHART_COLORS.length]}
                                    name={indexName}
                                />
                            ))}
                        </BarChart>
                    </ResponsiveContainer>
                </div>
            )}

            {/* Visualização de Tabela (Refatorada) */}
            {viewMode === 'table' && (
                <div className={styles.tableContainer}>
                    {/* O CSS do scrollbar agora está no .module.css
                        e será aplicado a este contêiner */}
                    <div className={styles.tableScrollContainer}>
                        <table className={styles.table}>
                            <thead className={styles.tableHead}>
                                <tr>
                                    <th className={`${styles.tableHeader} ${styles.categoryHeader}`}>
                                        Categoria
                                    </th>
                                    <th className={`${styles.tableHeader} ${styles.unitHeader}`}>
                                        Unidades
                                    </th>
                                    <th colSpan={allIndexNames.length} className={`${styles.tableHeader} ${styles.indicesHeader}`}>
                                        Índices Encontrados
                                    </th>
                                </tr>
                                <tr>
                                    <th className={styles.stickyColumn}></th>
                                    <th></th>
                                    {allIndexNames.map(indexName => (
                                        <th
                                            key={indexName}
                                            className={styles.indexNameHeader}
                                            title={indexName}
                                        >
                                            {indexName.length > 25 ? indexName.substring(0, 22) + '...' : indexName}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody className={styles.tableBody}>
                                {processedData.categories.map((cat, idx) => (
                                    <tr key={idx} className={styles.tableRow}>
                                        <td className={`${styles.tableCell} ${styles.categoryCell}`}>
                                            {cat.categoryName}
                                        </td>
                                        <td className={`${styles.tableCell} ${styles.unitCell}`}>
                                            {cat.totalUnits}
                                        </td>
                                        {allIndexNames.map(indexName => {
                                            const indexData = cat.indices.find(i => i.name === indexName);
                                            const count = indexData?.count || 0;
                                            return (
                                                <td
                                                    key={indexName}
                                                    className={`${styles.tableCell} ${count > 0 ? styles.indexCellHasCount : styles.indexCellNoCount}`}
                                                >
                                                    {count > 0 ? count : '—'}
                                                </td>
                                            );
                                        })}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* Visualização de Cards */}
            {viewMode === 'cards' && (
                <div className={styles.cardsScrollContainer}>
                    <div className={styles.cardsGrid}>
                        {processedData.categories.map((cat, idx) => (
                            <div key={idx} className={styles.card}>
                                <h3 className={styles.cardTitle}>
                                    {cat.categoryName}
                                </h3>
                                <div className={styles.cardMeta}>
                                    <TextSnippetIcon/> {cat.totalUnits} unidades de registro
                                </div>
                                <div className={styles.cardIndexList}>
                                    {cat.indices.map((index, i) => {
                                        const description = indexDescriptions.get(index.name);
                                        return (
                                            <div
                                                key={i}
                                                className={styles.cardIndexItem}
                                                style={{ borderLeftColor: index.color }}
                                                title={description || index.name}
                                            >
                                                <div className={styles.cardIndexHeader}>
                                                    <span className={styles.cardIndexName}>
                                                        {index.name}
                                                    </span>
                                                    <span className={styles.cardIndexCount} style={{ color: index.color }}>
                                                        {index.count}
                                                    </span>
                                                </div>
                                                {description && (
                                                    <p className={styles.cardIndexDescription}>
                                                        {description.length > 120 ? description.substring(0, 117) + '...' : description}
                                                    </p>
                                                )}
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Resumo estatístico compacto */}
            <div className={styles.summaryFooter}>
                {[
                    { value: processedData.categories.length, label: 'Categorias', color: '#B35848' },
                    { value: processedData.allIndices.length, label: 'Índices Únicos', color: '#5a8d87' },
                    { value: processedData.categories.reduce((sum, cat) => sum + cat.totalUnits, 0), label: 'Unidades de Registro', color: '#4A4644' },
                    { value: processedData.allIndices.reduce((sum, [_, count]) => sum + count, 0), label: 'Total de Ocorrências', color: '#c7a97f' }
                ].map(({ value, label, color }) => (
                    <div key={label} className={styles.summaryItem}>
                        <div className={styles.summaryValue} style={{ color }}>{value}</div>
                        <div className={styles.summaryLabel}>{label}</div>
                    </div>
                ))}
            </div>
        </div>
    );
}