import  { useMemo } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import styles from './css/ExplorationResults.module.css'
import type { ExplorationOfMaterialStage } from '../interfaces/dto/AnalysisResult';

interface ExplorationResultsProps {
    explorationStage: ExplorationOfMaterialStage;
}

// Tipo de dado que o gráfico espera
interface ChartData {
    categoryName: string;
    [indexName: string]: any; // Chaves dinâmicas para cada índice (ex: "VISUERROS": 5)
}

// Cores para as barras do gráfico (baseadas na sua paleta)
const CHART_COLORS = [
    '#B35848', // --primary
    '#4A4644', // --text-medium
    '#8E8883', // --text-light
    '#5a8d87', // Um verde/azul complementar
    '#c7a97f', // Um bege/dourado
];

export default function ExplorationResults({ explorationStage }: ExplorationResultsProps) {

    // 1. Lógica para processar os dados
    const { chartData, allIndexNames } = useMemo(() => {
        const data: ChartData[] = [];
        const indexNameSet = new Set<string>();
        console.log(explorationStage);

        // Para cada Categoria...
        for (const category of explorationStage.categories) {
            const categoryData: ChartData = { categoryName: category.name };
            console.log(explorationStage.categories);
            // Contar os índices...
            const indexCounts: { [key: string]: number } = {};
            
            // Iterando pelas RegisterUnits e depois pelos FoundIndices
            category.registerUnits.forEach(unit => {
                unit.foundIndices.forEach(index => {
                    indexNameSet.add(index.name); // Adiciona ao Set global de índices
                    indexCounts[index.name] = (indexCounts[index.name] || 0) + 1;
                });
            });
            
            data.push({ ...categoryData, ...indexCounts });
        }
        
        const allIndexNames = Array.from(indexNameSet);
        return { chartData: data, allIndexNames };
    }, [explorationStage]);

    return (
        <section className={styles.container}>
            <div className={styles.header}>
                <h2 className={styles.title}>Resultados da Exploração</h2>
                {/* Botões para o futuro (ex: exportar) */}
            </div>

            <p className={styles.description}>
                Abaixo está a contagem de cada <strong>Índice</strong> encontrado,
                agrupado pela <strong>Categoria</strong> em que foi identificado.
            </p>

            {/* 2. O Gráfico Interativo */}
            <div className={styles.chartWrapper}>
                <ResponsiveContainer width="100%" height={400}>
                    <BarChart
                        data={chartData}
                        margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
                    >
                        <CartesianGrid strokeDasharray="3 3" stroke="#EFEBE6" />
                        <XAxis dataKey="categoryName" stroke="#4A4644" />
                        <YAxis allowDecimals={false} stroke="#4A4644" />
                        <Tooltip
                            contentStyle={{ 
                                backgroundColor: 'var(--background-light, #FBFBF8)', 
                                border: '1px solid var(--background-medium, #EFEBE6)',
                                borderRadius: '8px'
                            }}
                            cursor={{ fill: '#fef4f2' /* Cor de hover */ }}
                        />
                        <Legend />
                        
                        {/* 3. Cria uma <Bar> para cada Índice encontrado */}
                        {allIndexNames.map((indexName, i) => (
                            <Bar 
                                key={indexName} 
                                dataKey={indexName} 
                                fill={CHART_COLORS[i % CHART_COLORS.length]} 
                            />
                        ))}
                    </BarChart>
                </ResponsiveContainer>
            </div>
        </section>
    );
}
// import { useMemo } from 'react';
// import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

// interface Index {
//     id: number;
//     name: string;
//     description?: string;
// }

// interface RegisterUnit {
//     id: number;
//     text: string;
//     sourceDocumentUri: string;
//     page?: string;
//     line?: string;
//     justification?: string;
//     categoryId: number;
//     foundIndices: Index[];
// }

// interface Category {
//     id: number;
//     name: string;
//     definition: string;
//     frequency: number;
//     explorationOfMaterialStageId: number;
//     registerUnits: RegisterUnit[];
// }

// interface ExplorationOfMaterialStage {
//     id: number;
//     categories: Category[];
// }

// interface ExplorationResultsProps {
//     explorationStage: ExplorationOfMaterialStage;
// }

// // Tipo de dado que o gráfico espera
// interface ChartData {
//     categoryName: string;
//     [indexName: string]: any;
// }

// // Cores para as barras do gráfico
// const CHART_COLORS = [
//     '#B35848', // --primary
//     '#4A4644', // --text-medium
//     '#8E8883', // --text-light
//     '#5a8d87', // Verde/azul complementar
//     '#c7a97f', // Bege/dourado
//     '#d4a5a5', // Rosa claro
//     '#7b9e89', // Verde musgo
// ];

// export default function ExplorationResults({ explorationStage }: ExplorationResultsProps) {
    
//     console.log('=== DEBUG EXPLORATIONRESULTS ===');
//     console.log('explorationStage recebido:', explorationStage);
//     console.log('Número de categorias:', explorationStage?.categories?.length);

//     // Processar os dados
//     const { chartData, allIndexNames } = useMemo(() => {
//         console.log('--- INICIANDO PROCESSAMENTO ---');
        
//         if (!explorationStage || !explorationStage.categories) {
//             console.error('❌ explorationStage ou categories está undefined');
//             return { chartData: [], allIndexNames: [] };
//         }

//         const data: ChartData[] = [];
//         const indexNameSet = new Set<string>();

//         // Para cada Categoria...
//         for (const category of explorationStage.categories) {
//             console.log(`\n📁 Processando categoria: "${category.name}"`);
//             console.log(`   - ID: ${category.id}`);
//             console.log(`   - RegisterUnits: ${category.registerUnits?.length || 0}`);
            
//             const categoryData: ChartData = { categoryName: category.name };
//             const indexCounts: { [key: string]: number } = {};
            
//             // Verificar se registerUnits existe
//             if (!category.registerUnits || category.registerUnits.length === 0) {
//                 console.warn(`   ⚠️ Categoria "${category.name}" não tem registerUnits!`);
//                 data.push(categoryData);
//                 continue;
//             }
            
//             // Iterando pelas RegisterUnits
//             category.registerUnits.forEach((unit, unitIndex) => {
//                 console.log(`   📄 RegisterUnit ${unitIndex + 1}:`, {
//                     id: unit.id,
//                     text: unit.text?.substring(0, 50) + '...',
//                     foundIndices: unit.foundIndices?.length || 0
//                 });
                
//                 // Verificar se foundIndices existe
//                 if (!unit.foundIndices || unit.foundIndices.length === 0) {
//                     console.warn(`      ⚠️ RegisterUnit ${unit.id} não tem foundIndices!`);
//                     return;
//                 }
                
//                 // Iterando pelos FoundIndices
//                 unit.foundIndices.forEach((index, indexIdx) => {
//                     console.log(`      🏷️ Index ${indexIdx + 1}: "${index.name}" (ID: ${index.id})`);
//                     indexNameSet.add(index.name);
//                     indexCounts[index.name] = (indexCounts[index.name] || 0) + 1;
//                 });
//             });
            
//             console.log(`   ✅ Contagem final para "${category.name}":`, indexCounts);
//             data.push({ ...categoryData, ...indexCounts });
//         }
        
//         const allIndexNames = Array.from(indexNameSet);
        
//         console.log('\n=== RESULTADO FINAL ===');
//         console.log('chartData:', data);
//         console.log('allIndexNames:', allIndexNames);
//         console.log('========================\n');
        
//         return { chartData: data, allIndexNames };
//     }, [explorationStage]);

//     return (
//         <section style={{ 
//             padding: '2rem',
//             maxWidth: '1200px',
//             margin: '0 auto'
//         }}>
//             <div style={{ marginBottom: '2rem' }}>
//                 <h2 style={{ 
//                     fontSize: '1.75rem',
//                     fontWeight: 'bold',
//                     color: '#4A4644',
//                     marginBottom: '0.5rem'
//                 }}>
//                     Resultados da Exploração
//                 </h2>
//             </div>

//             <p style={{ 
//                 marginBottom: '2rem',
//                 color: '#8E8883',
//                 fontSize: '1rem'
//             }}>
//                 Abaixo está a contagem de cada <strong>Índice</strong> encontrado,
//                 agrupado pela <strong>Categoria</strong> em que foi identificado.
//             </p>

//             {/* Debug Info */}
//             <div style={{
//                 background: '#f0f0f0',
//                 padding: '1rem',
//                 borderRadius: '8px',
//                 marginBottom: '2rem',
//                 fontFamily: 'monospace',
//                 fontSize: '0.85rem'
//             }}>
//                 <strong>🔍 Debug Info:</strong><br/>
//                 Categorias: {explorationStage?.categories?.length || 0}<br/>
//                 Índices únicos encontrados: {allIndexNames.length}<br/>
//                 Dados do gráfico: {chartData.length} pontos<br/>
//                 {allIndexNames.length === 0 && (
//                     <span style={{ color: 'red' }}>
//                         ⚠️ Nenhum índice foi encontrado! Verifique o console.
//                     </span>
//                 )}
//             </div>

//             {/* Gráfico */}
//             <div style={{
//                 background: 'white',
//                 padding: '2rem',
//                 borderRadius: '12px',
//                 boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
//             }}>
//                 {chartData.length === 0 || allIndexNames.length === 0 ? (
//                     <div style={{ 
//                         textAlign: 'center', 
//                         padding: '3rem',
//                         color: '#8E8883'
//                     }}>
//                         <p style={{ fontSize: '1.2rem', marginBottom: '1rem' }}>
//                             📊 Nenhum dado disponível para visualização
//                         </p>
//                         <p style={{ fontSize: '0.9rem' }}>
//                             Verifique o console para mais detalhes sobre os dados recebidos.
//                         </p>
//                     </div>
//                 ) : (
//                     <ResponsiveContainer width="100%" height={400}>
//                         <BarChart
//                             data={chartData}
//                             margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
//                         >
//                             <CartesianGrid strokeDasharray="3 3" stroke="#EFEBE6" />
//                             <XAxis 
//                                 dataKey="categoryName" 
//                                 stroke="#4A4644"
//                                 style={{ fontSize: '0.875rem' }}
//                             />
//                             <YAxis 
//                                 allowDecimals={false} 
//                                 stroke="#4A4644"
//                                 style={{ fontSize: '0.875rem' }}
//                             />
//                             <Tooltip
//                                 contentStyle={{ 
//                                     backgroundColor: '#FBFBF8', 
//                                     border: '1px solid #EFEBE6',
//                                     borderRadius: '8px',
//                                     padding: '0.75rem'
//                                 }}
//                                 cursor={{ fill: 'rgba(179, 88, 72, 0.1)' }}
//                             />
//                             <Legend 
//                                 wrapperStyle={{ paddingTop: '20px' }}
//                             />
                            
//                             {allIndexNames.map((indexName, i) => (
//                                 <Bar 
//                                     key={indexName} 
//                                     dataKey={indexName} 
//                                     fill={CHART_COLORS[i % CHART_COLORS.length]}
//                                     name={indexName}
//                                 />
//                             ))}
//                         </BarChart>
//                     </ResponsiveContainer>
//                 )}
//             </div>
//         </section>
//     );
// }