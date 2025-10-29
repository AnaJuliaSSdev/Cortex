import  { useMemo, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import styles from './css/ExplorationResults.module.css'
import type { ExplorationOfMaterialStage } from '../interfaces/dto/AnalysisResult';

interface ExplorationResultsProps {
    explorationStage: ExplorationOfMaterialStage;
}

type ViewMode = 'chart' | 'table' | 'cards';

// Tipo de dado que o gráfico espera
interface ChartData {
    categoryName: string;
    [indexName: string]: any; // Chaves dinâmicas para cada índice (ex: "VISUERROS": 5)
}

// Cores para as barras do gráfico (baseadas na sua paleta)
// const CHART_COLORS = [
//     '#B35848', // --primary
//     '#4A4644', // --text-medium
//     '#8E8883', // --text-light
//     '#5a8d87', // Um verde/azul complementar
//     '#c7a97f', // Um bege/dourado
// ];

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
    const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

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
    

    // // 1. Lógica para processar os dados
    // const { chartData, allIndexNames } = useMemo(() => {
    //     const data: ChartData[] = [];
    //     const indexNameSet = new Set<string>();
    //     console.log(explorationStage);

    //     // Para cada Categoria...
    //     for (const category of explorationStage.categories) {
    //         const categoryData: ChartData = { categoryName: category.name };
    //         console.log(explorationStage.categories);
    //         // Contar os índices...
    //         const indexCounts: { [key: string]: number } = {};
            
    //         // Iterando pelas RegisterUnits e depois pelos FoundIndices
    //         category.registerUnits.forEach(unit => {
    //             unit.foundIndices.forEach(index => {
    //                 indexNameSet.add(index.name); // Adiciona ao Set global de índices
    //                 indexCounts[index.name] = (indexCounts[index.name] || 0) + 1;
    //             });
    //         });
            
    //         data.push({ ...categoryData, ...indexCounts });
    //     }
        
    //     const allIndexNames = Array.from(indexNameSet);
    //     return { chartData: data, allIndexNames };
    // }, [explorationStage]);

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

    // return (
    //     <section className={styles.container}>
    //         <div className={styles.header}>
    //             <h2 className={styles.title}>Resultados da Exploração</h2>
    //             {/* Botões para o futuro (ex: exportar) */}
    //         </div>

    //         <p className={styles.description}>
    //             Abaixo está a contagem de cada <strong>Índice</strong> encontrado,
    //             agrupado pela <strong>Categoria</strong> em que foi identificado.
    //         </p>

    //         {/* 2. O Gráfico Interativo */}
    //         <div className={styles.chartWrapper}>
    //             <ResponsiveContainer width="100%" height={400}>
    //                 <BarChart
    //                     data={chartData}
    //                     margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
    //                 >
    //                     <CartesianGrid strokeDasharray="3 3" stroke="#EFEBE6" />
    //                     <XAxis dataKey="categoryName" stroke="#4A4644" />
    //                     <YAxis allowDecimals={false} stroke="#4A4644" />
    //                     <Tooltip
    //                         contentStyle={{ 
    //                             backgroundColor: 'var(--background-light, #FBFBF8)', 
    //                             border: '1px solid var(--background-medium, #EFEBE6)',
    //                             borderRadius: '8px'
    //                         }}
    //                         cursor={{ fill: '#fef4f2' /* Cor de hover */ }}
    //                     />
    //                     <Legend />
                        
    //                     {/* 3. Cria uma <Bar> para cada Índice encontrado */}
    //                     {allIndexNames.map((indexName, i) => (
    //                         <Bar 
    //                             key={indexName} 
    //                             dataKey={indexName} 
    //                             fill={CHART_COLORS[i % CHART_COLORS.length]} 
    //                         />
    //                     ))}
    //                 </BarChart>
    //             </ResponsiveContainer>
    //         </div>
    //     </section>
    // );

     return (
        <div style={{
            maxWidth: '1400px',
            margin: '0 auto',
            padding: '2rem',
            fontFamily: 'system-ui, -apple-system, sans-serif'
        }}>
            {/* Header com controles */}
            <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '2rem',
                flexWrap: 'wrap',
                gap: '1rem'
            }}>
                <div>
                    <h2 style={{
                        fontSize: '2rem',
                        fontWeight: 'bold',
                        color: '#4A4644',
                        margin: 0
                    }}>
                        Resultados da Exploração
                    </h2>
                    <p style={{ color: '#8E8883', margin: '0.5rem 0 0 0' }}>
                        {processedData.categories.length} categorias • {processedData.allIndices.length} índices únicos
                    </p>
                </div>

                {/* Botões de visualização */}
                <div style={{
                    display: 'flex',
                    gap: '0.5rem',
                    background: '#f5f5f5',
                    padding: '0.25rem',
                    borderRadius: '8px'
                }}>
                    <button
                        onClick={() => setViewMode('chart')}
                        style={{
                            padding: '0.5rem 1rem',
                            border: 'none',
                            borderRadius: '6px',
                            cursor: 'pointer',
                            background: viewMode === 'chart' ? '#B35848' : 'transparent',
                            color: viewMode === 'chart' ? 'white' : '#4A4644',
                            fontWeight: viewMode === 'chart' ? 'bold' : 'normal'
                        }}
                    >
                        📊 Gráfico
                    </button>
                    <button
                        onClick={() => setViewMode('table')}
                        style={{
                            padding: '0.5rem 1rem',
                            border: 'none',
                            borderRadius: '6px',
                            cursor: 'pointer',
                            background: viewMode === 'table' ? '#B35848' : 'transparent',
                            color: viewMode === 'table' ? 'white' : '#4A4644',
                            fontWeight: viewMode === 'table' ? 'bold' : 'normal'
                        }}
                    >
                        📋 Tabela
                    </button>
                    <button
                        onClick={() => setViewMode('cards')}
                        style={{
                            padding: '0.5rem 1rem',
                            border: 'none',
                            borderRadius: '6px',
                            cursor: 'pointer',
                            background: viewMode === 'cards' ? '#B35848' : 'transparent',
                            color: viewMode === 'cards' ? 'white' : '#4A4644',
                            fontWeight: viewMode === 'cards' ? 'bold' : 'normal'
                        }}
                    >
                        🎴 Cards
                    </button>
                </div>
            </div>

            {/* Visualização de Gráfico */}
            {viewMode === 'chart' && (
                <div style={{
                    background: 'white',
                    padding: '2rem',
                    borderRadius: '12px',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
                }}>
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

            {/* Visualização de Tabela */}
            {viewMode === 'table' && (
                <div style={{
                    background: 'white',
                    borderRadius: '12px',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
                    overflow: 'hidden'
                }}>
                    <div style={{ overflowX: 'auto' }}>
                        <table style={{
                            width: '100%',
                            borderCollapse: 'collapse',
                            fontSize: '0.95rem'
                        }}>
                            <thead>
                                <tr style={{ background: '#f8f8f8' }}>
                                    <th style={{
                                        padding: '1rem',
                                        textAlign: 'left',
                                        borderBottom: '2px solid #e0e0e0',
                                        fontWeight: 'bold',
                                        color: '#4A4644'
                                    }}>
                                        Categoria
                                    </th>
                                    <th style={{
                                        padding: '1rem',
                                        textAlign: 'center',
                                        borderBottom: '2px solid #e0e0e0',
                                        fontWeight: 'bold',
                                        color: '#4A4644'
                                    }}>
                                        Unidades
                                    </th>
                                    {allIndexNames.map(indexName => (
                                        <th
                                            key={indexName}
                                            style={{
                                                padding: '1rem',
                                                textAlign: 'center',
                                                borderBottom: '2px solid #e0e0e0',
                                                fontWeight: 'bold',
                                                color: '#4A4644',
                                                minWidth: '100px'
                                            }}
                                        >
                                            {indexName}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {processedData.categories.map((cat, idx) => (
                                    <tr
                                        key={idx}
                                        style={{
                                            background: idx % 2 === 0 ? 'white' : '#fafafa',
                                            transition: 'background 0.2s'
                                        }}
                                        onMouseEnter={(e) => e.currentTarget.style.background = '#f0f0f0'}
                                        onMouseLeave={(e) => e.currentTarget.style.background = idx % 2 === 0 ? 'white' : '#fafafa'}
                                    >
                                        <td style={{
                                            padding: '1rem',
                                            borderBottom: '1px solid #e0e0e0',
                                            fontWeight: '500'
                                        }}>
                                            {cat.categoryName}
                                        </td>
                                        <td style={{
                                            padding: '1rem',
                                            textAlign: 'center',
                                            borderBottom: '1px solid #e0e0e0',
                                            color: '#8E8883'
                                        }}>
                                            {cat.totalUnits}
                                        </td>
                                        {allIndexNames.map(indexName => {
                                            const indexData = cat.indices.find(i => i.name === indexName);
                                            const count = indexData?.count || 0;
                                            return (
                                                <td
                                                    key={indexName}
                                                    style={{
                                                        padding: '1rem',
                                                        textAlign: 'center',
                                                        borderBottom: '1px solid #e0e0e0',
                                                        fontWeight: count > 0 ? 'bold' : 'normal',
                                                        color: count > 0 ? '#B35848' : '#d0d0d0'
                                                    }}
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
                <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
                    gap: '1.5rem'
                }}>
                    {processedData.categories.map((cat, idx) => (
                        <div
                            key={idx}
                            style={{
                                background: 'white',
                                borderRadius: '12px',
                                padding: '1.5rem',
                                boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
                                transition: 'transform 0.2s, box-shadow 0.2s',
                                cursor: 'pointer'
                            }}
                            onMouseEnter={(e) => {
                                e.currentTarget.style.transform = 'translateY(-4px)';
                                e.currentTarget.style.boxShadow = '0 4px 16px rgba(0,0,0,0.15)';
                            }}
                            onMouseLeave={(e) => {
                                e.currentTarget.style.transform = 'translateY(0)';
                                e.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)';
                            }}
                        >
                            <h3 style={{
                                margin: '0 0 1rem 0',
                                color: '#4A4644',
                                fontSize: '1.25rem',
                                borderBottom: '2px solid #B35848',
                                paddingBottom: '0.5rem'
                            }}>
                                {cat.categoryName}
                            </h3>
                            
                            <div style={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: '0.5rem',
                                marginBottom: '1rem',
                                color: '#8E8883',
                                fontSize: '0.9rem'
                            }}>
                                <span>📄 {cat.totalUnits} unidades de registro</span>
                            </div>

                            <div style={{
                                display: 'flex',
                                flexDirection: 'column',
                                gap: '0.75rem'
                            }}>
                                {cat.indices.map((index, i) => (
                                    <div
                                        key={i}
                                        style={{
                                            display: 'flex',
                                            justifyContent: 'space-between',
                                            alignItems: 'center',
                                            padding: '0.75rem',
                                            background: '#f8f8f8',
                                            borderRadius: '8px',
                                            borderLeft: `4px solid ${index.color}`
                                        }}
                                    >
                                        <span style={{
                                            fontWeight: '500',
                                            color: '#4A4644',
                                            flex: 1
                                        }}>
                                            {index.name}
                                        </span>
                                        <span style={{
                                            fontWeight: 'bold',
                                            color: index.color,
                                            fontSize: '1.25rem',
                                            minWidth: '40px',
                                            textAlign: 'right'
                                        }}>
                                            {index.count}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Resumo estatístico */}
            <div style={{
                marginTop: '2rem',
                padding: '1.5rem',
                background: '#f8f8f8',
                borderRadius: '12px',
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                gap: '1rem'
            }}>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#B35848' }}>
                        {processedData.categories.length}
                    </div>
                    <div style={{ color: '#8E8883', fontSize: '0.9rem' }}>Categorias</div>
                </div>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#5a8d87' }}>
                        {processedData.allIndices.length}
                    </div>
                    <div style={{ color: '#8E8883', fontSize: '0.9rem' }}>Índices Únicos</div>
                </div>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#4A4644' }}>
                        {processedData.categories.reduce((sum, cat) => sum + cat.totalUnits, 0)}
                    </div>
                    <div style={{ color: '#8E8883', fontSize: '0.9rem' }}>Unidades de Registro</div>
                </div>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#c7a97f' }}>
                        {processedData.allIndices.reduce((sum, [_, count]) => sum + count, 0)}
                    </div>
                    <div style={{ color: '#8E8883', fontSize: '0.9rem' }}>Total de Ocorrências</div>
                </div>
            </div>
        </div>
    );
}