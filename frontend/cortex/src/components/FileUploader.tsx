import React, { useState, useCallback } from 'react';
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
}

// Limites (como você mencionou, um pouco menos de 50MB)
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
    onUploadSuccess
}) => {
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const onDrop = useCallback(async (acceptedFiles: File[]) => {
        setError(null);
        if (acceptedFiles.length === 0) {
            return;
        }

        setIsUploading(true);
        // Faz o upload dos arquivos um por um, como o backend espera
        for (const file of acceptedFiles) {
            try {
                const uploadedDoc = await uploadDocument(analysisId, file, purpose);
                onUploadSuccess(uploadedDoc); // Notifica o componente pai sobre o sucesso
            } catch (err) {
                setError(`Erro ao enviar o arquivo: ${file.name}. Tente novamente.`);
                // Para o loop se um arquivo falhar
                break; 
            }
        }
        setIsUploading(false);
    }, [analysisId, purpose, onUploadSuccess]);

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: ACCEPTED_FILES,
        maxSize: MAX_FILE_SIZE,
        onDropRejected: (fileRejections) => {
            const firstError = fileRejections[0].errors[0];
            if (firstError.code === 'file-too-large') {
                setError('Arquivo muito grande. O limite é de 48MB.');
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
                {...getRootProps()} 
                className={`${styles.dropzone} ${isDragActive ? styles.dragActive : ''}`}
            >
                <input {...getInputProps()} />
                {isUploading ? (
                    <p>Enviando arquivos...</p> // Você pode adicionar um Spinner aqui
                ) : (
                    <>
                        <p className={styles.uploaderDescription}>{description}</p>
                        <p className={styles.fileTypes}>Arquivos suportados: .pdf, .txt (Máx 48MB)</p>
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