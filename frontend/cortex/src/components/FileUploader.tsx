import React, { useState, useCallback, useMemo } from 'react';
import { useDropzone } from 'react-dropzone';
import styles from './css/FileUploader.module.css'; // Vamos criar este arquivo de estilo
import type { DocumentPurpose } from '../interfaces/enum/DocumentPurpose';
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';
import {uploadDocument} from '../services/documentService';

// Definindo as props do componente
interface FileUploaderProps {
    analysisId: string;
    purpose: DocumentPurpose;
    title: string;
    description: string;
    onUploadSuccess: (document: UploadedDocument) => void;
    currentTotalSize: number; // Tamanho (em bytes) dos arquivos já enviados
    maxTotalSize: number;     // Limite total (em bytes)
}

// Função auxiliar para formatar bytes
export const formatBytes = (bytes: number, decimals = 1) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

// Limites 
const MAX_FILE_SIZE = 48 * 1024 * 1024; // 48 MB
const ACCEPTED_FILES = {
    'application/pdf': ['.pdf'],
    'text/plain': ['.txt'],
};

const FileUploader: React.FC<FileUploaderProps> = ({
    analysisId,
    purpose,
    title,
    description,
    onUploadSuccess,
    currentTotalSize,
    maxTotalSize
}) => {
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const onDrop = useCallback(async (acceptedFiles: File[]) => {
        setError(null);
        if (acceptedFiles.length === 0) return;

        // Faz uma cópia mutável do tamanho atual
        let newTotalSize = currentTotalSize;

        // Calcula o tamanho total *deste lote*
        const batchSize = acceptedFiles.reduce((sum, file) => sum + file.size, 0);

        // Verificação Rápida: Se o lote inteiro de uma vez estoura o limite
        if (newTotalSize + batchSize > maxTotalSize) {
             setError(`Limite de ${formatBytes(maxTotalSize)} excedido. Você só pode adicionar mais ${formatBytes(maxTotalSize - newTotalSize)}.`);
             return; // Rejeita o lote inteiro
        }

        setIsUploading(true);
        // Faz o upload dos arquivos um por um, como o backend espera
        for (const file of acceptedFiles) {
            // Verificação dupla, caso o lote tenha vários arquivos
            // (Embora a verificação acima já deva pegar isso, é uma segurança extra)
            if (newTotalSize + file.size > maxTotalSize) {
                setError(`Limite de ${formatBytes(maxTotalSize)} atingido. O arquivo ${file.name} não foi enviado.`);
                break; // Para o loop
            }

            try {
                const uploadedDoc = await uploadDocument(analysisId, file, purpose);
                onUploadSuccess(uploadedDoc); // Notifica o pai
                // Atualiza o total para a próxima iteração do loop
                newTotalSize += uploadedDoc.fileSize; 
            } catch (err) {
                setError(`Erro ao enviar o arquivo: ${file.name}. Tente novamente.`);
                break; 
            }
        }
        setIsUploading(false);
    }, [analysisId, purpose, onUploadSuccess, currentTotalSize, maxTotalSize]);

    const storageInfo = useMemo(() => {
        return `${formatBytes(currentTotalSize)} de ${formatBytes(maxTotalSize)} usados`;
    }, [currentTotalSize, maxTotalSize]);

    // Verifica se o uploader deve ser desabilitado
    const isStorageFull = currentTotalSize >= maxTotalSize;

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: ACCEPTED_FILES,
        maxSize: MAX_FILE_SIZE,
        onDropRejected: (fileRejections) => {
            const firstError = fileRejections[0].errors[0];
            if (firstError.code === 'file-too-large') {
                setError(`Arquivo muito grande. O limite é de ${formatBytes(MAX_FILE_SIZE)}.`);
            } else if (firstError.code === 'file-invalid-type') {
                setError('Tipo de arquivo inválido. Apenas .pdf e .txt são aceitos.');
            } else {
                setError('Arquivo inválido. Verifique o tipo e o tamanho.');
            }
        },
    });

    return (
        <div className={styles.uploaderContainer}>
            <h3 className={styles.uploaderTitle}>{title}</h3>
            <div 
                {...getRootProps({ 
                    // Desabilita o dropzone se o armazenamento estiver cheio
                    disabled: isStorageFull 
                })}
                className={`${styles.dropzone} ${isDragActive ? styles.dragActive : ''}`}
            >
                <input {...getInputProps({ disabled: isStorageFull })} />
                
                {isUploading ? (
                    <p>Enviando arquivos...</p>
                ) : isStorageFull ? (
                    // MENSAGEM SE ESTIVER CHEIO
                    <>
                        <p className={styles.uploaderDescription}>Limite de armazenamento atingido.</p>
                        <p className={styles.storageInfo}>{storageInfo}</p>
                    </>
                ) : (
                    //USO ATUAL DE ARMAZENAMENTO
                    <>
                        <p className={styles.uploaderDescription}>{description}</p>
                        <p className={styles.fileTypes}>Arquivos suportados: .pdf, .txt (Máx {formatBytes(MAX_FILE_SIZE)})</p>
                        <p className={styles.storageInfo}>{storageInfo}</p>
                        <button type="button" className={styles.uploadButton}>
                            Selecionar Arquivos
                        </button>
                    </>
                )}
            </div>
            {error && <p className={styles.errorMessage}>{error}</p>}
        </div>
    );
};

export default FileUploader;