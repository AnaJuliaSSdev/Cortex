// Corresponde ao seu ExportOptions no backend
export interface ExportOptions {
    includeCharts: boolean;
    includeTables: boolean;
    includeRegisterUnits: boolean;
}

// Corresponde ao seu ExportRequestDto no backend
export interface ExportRequestDto {
    chartImageBase64?: string;
    options?: ExportOptions;
}