import { useMemo, useRef, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import html2canvas from 'html2canvas';
import styles from './css/ExplorationResults.module.css'
import type { Category, ExplorationOfMaterialStage } from '../interfaces/dto/AnalysisResult';
import AutoGraphIcon from '@mui/icons-material/AutoGraph';
import SpaceDashboardIcon from '@mui/icons-material/SpaceDashboard';
import DescriptionIcon from '@mui/icons-material/Description';
import TextSnippetIcon from '@mui/icons-material/TextSnippet';
import CategoryDetailsModal from './CategoryDetailsModal';
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import { exportAnalysisToPdf } from '../services/exportService';
import type { AlertType } from './Alert';
import Alert from './Alert';
import DownloadingIcon from '@mui/icons-material/Downloading';
import ArrowDropDownIcon from '@mui/icons-material/ArrowDropDown';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';

interface ExplorationResultsProps {
    explorationStage: ExplorationOfMaterialStage;
    analysisDocuments: UploadedDocument[];
    referenceDocuments: UploadedDocument[];
    analysisId: string;
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

export default function ExplorationResults({ explorationStage, analysisDocuments,
    referenceDocuments, analysisId }: ExplorationResultsProps) {

    const [viewMode, setViewMode] = useState<ViewMode>('chart');
    const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);

    const [isExporting, setIsExporting] = useState(false);
    const [exportAlert, setExportAlert] = useState<{ message: string; type: AlertType } | null>(null);
    const [isExportMenuOpen, setIsExportMenuOpen] = useState(false);
    const chartRef = useRef<HTMLDivElement>(null);

    const handleCategoryClick = (categoryName: string) => {
        const fullCategory = explorationStage.categories.find(c => c.name === categoryName);
        if (fullCategory) {
            setSelectedCategory(fullCategory);
        }
    };

    const handleExportPdf = async () => {
        setIsExporting(true);
        setExportAlert(null);
        setIsExportMenuOpen(false);

        setViewMode('chart');
        await new Promise(resolve => setTimeout(resolve, 100));

        const options = {
            backgroundColor: '#ffffff',
            scale: window.devicePixelRatio || 2,
            useCORS: true,
            allowTaint: true,
            logging: false,
            width: chartRef.current!.scrollWidth,
            height: chartRef.current!.scrollHeight
            } as any;


        let chartImageBase64: string | null = null;

        // Capturar o gráfico como imagem
        if (chartRef.current) {
            try {
                // Aguardar renderização completa
                await new Promise(resolve => setTimeout(resolve, 500));
                
                // Usar html2canvas que funciona melhor com Recharts
               const canvas = await html2canvas(chartRef.current as HTMLElement, options);
                // Converter canvas para Base64
                const dataUrl = canvas.toDataURL('image/png');
                
                // Remover o prefixo "data:image/png;base64,"
                chartImageBase64 = dataUrl.split(',')[1];
                
                console.log('Gráfico capturado com sucesso! Tamanho:', chartImageBase64.length, 'chars');

            } catch (err) {
                console.error("Falha ao capturar o gráfico:", err);
                setExportAlert({ 
                    message: "Falha ao capturar a imagem do gráfico. O PDF será gerado sem o gráfico.", 
                    type: "warning" 
                });
                chartImageBase64 = null;
            }
        }

        // Chamar o serviço de exportação
        try {
            await exportAnalysisToPdf(analysisId, {
                chartImageBase64: chartImageBase64 || undefined,
                options: {
                    includeCharts: true,
                    includeTables: true,
                    includeRegisterUnits: true
                }
            });
            
            setExportAlert({ 
                message: "Seu PDF foi baixado com sucesso!", 
                type: "success" 
            });
        } catch (err) {
            console.error("Erro na exportação:", err);
            setExportAlert({ 
                message: "Falha ao gerar o PDF. Verifique sua conexão e tente novamente.", 
                type: "error" 
            });
        } finally {
            setIsExporting(false);
        }
    };

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
                        <AutoGraphIcon /> Gráfico
                    </button>
                    <button
                        onClick={() => setViewMode('table')}
                        className={`${styles.viewButton} ${viewMode === 'table' ? styles.activeButton : ''}`}
                    >
                        <SpaceDashboardIcon /> Tabela
                    </button>
                    <button
                        onClick={() => setViewMode('cards')}
                        className={`${styles.viewButton} ${viewMode === 'cards' ? styles.activeButton : ''}`}
                    >
                        <DescriptionIcon /> Cards
                    </button>
                </div>

                <div className={styles.exportContainer}>
                    <button
                        className={styles.exportButton}
                        onClick={() => setIsExportMenuOpen(!isExportMenuOpen)}
                        disabled={isExporting}
                    >
                        <DownloadingIcon />
                        {isExporting ? 'Exportando...' : 'Exportar Relatório'}
                        <ArrowDropDownIcon />
                    </button>

                    {isExportMenuOpen && (
                        <div className={styles.exportMenu}>
                            <button onClick={handleExportPdf}>
                                <PictureAsPdfIcon /> Exportar como PDF
                            </button>
                        </div>
                    )}
                </div>
            </div>

            {exportAlert && (
                <Alert
                    message={exportAlert.message}
                    type={exportAlert.type}
                    onClose={() => setExportAlert(null)}
                />
            )}

            {/* Visualização de Gráfico */}
            {viewMode === 'chart' && (
                <div className={styles.chartContainer}>
                     <div ref={chartRef} style={{ backgroundColor: '#fff', padding: '1rem' }}>
                    <ResponsiveContainer width="100%" minHeight={600}>
                        <BarChart
                            data={chartData}
                            margin={{ top: 20, right: 30, left: 20, bottom: 150 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="#EFEBE6" />
                            <XAxis
                                dataKey="category"
                                angle={-45}
                                textAnchor="end"
                                height={150}
                                interval={0}
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
                                wrapperStyle={{ paddingBottom: '20px' }}
                                layout="horizontal"
                                verticalAlign="top"
                            />
                            {allIndexNames.map((indexName, i) => (
                                <Bar
                                    key={indexName}
                                    dataKey={indexName}
                                    fill={CHART_COLORS[i % CHART_COLORS.length]}
                                    name={indexName}
                                    barSize={20}
                                />
                            ))}
                        </BarChart>
                    </ResponsiveContainer>
                </div>
            </div>
            )}

             {/* Visualização de Tabela */}
            {viewMode === 'table' && (
                <div className={styles.tableWrapper}>
                    <table className={styles.tableFixed}>
                        <thead>
                            <tr>
                                <th className={styles.thCategory} rowSpan={2}>
                                    Categoria
                                </th>
                                <th className={styles.thUnits} rowSpan={2}>
                                    Unidades
                                </th>
                                <th className={styles.thIndicesGroup} colSpan={allIndexNames.length}>
                                    Índices Encontrados
                                </th>
                            </tr>
                            <tr className={styles.indexNamesRow}>
                                {allIndexNames.map(indexName => (
                                    <th
                                        key={indexName}
                                        className={styles.thIndexName}
                                        title={indexName}
                                    >
                                        <div className={styles.indexNameText}>
                                            {indexName.length > 40 ? indexName.substring(0, 35) + '...' : indexName}
                                        </div>
                                    </th>
                                ))}
                            </tr>
                        </thead>
                        <tbody>
                            {processedData.categories.map((cat, idx) => (
                                <tr key={idx} className={styles.dataRow}>
                                    <td className={styles.tdCategory}>
                                        {cat.categoryName}
                                    </td>
                                    <td className={styles.tdUnits}>
                                        {cat.totalUnits}
                                    </td>
                                    {allIndexNames.map(indexName => {
                                        const indexData = cat.indices.find(i => i.name === indexName);
                                        const count = indexData?.count || 0;
                                        return (
                                            <td
                                                key={indexName}
                                                className={count > 0 ? styles.tdIndexValue : styles.tdIndexEmpty}
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
            )}

            {/* Visualização de Cards */}
            {viewMode === 'cards' && (
                <div className={styles.cardsScrollContainer}>
                    <div className={styles.cardsGrid}>
                        {processedData.categories.map((cat, idx) => (
                            <div
                                onClick={() => handleCategoryClick(cat.categoryName)}
                                key={idx}
                                className={styles.card}>
                                <h3 className={styles.cardTitle}>
                                    {cat.categoryName}
                                </h3>
                                <div className={styles.cardMeta}>
                                    <TextSnippetIcon /> {cat.totalUnits} unidades de registro
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

            {/* Resumo estatístico */}
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
            
            <CategoryDetailsModal
                isOpen={!!selectedCategory}
                onClose={() => setSelectedCategory(null)}
                category={selectedCategory}
                allDocuments={[...analysisDocuments, ...referenceDocuments]}
            />
        </div>
    );
}