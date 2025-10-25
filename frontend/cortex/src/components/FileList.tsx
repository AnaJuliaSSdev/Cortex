import React from 'react';
import styles from './css/FileList.module.css';
import type { UploadedDocument } from '../interfaces/dto/UploadedDocument';

interface FileListProps {
    files: UploadedDocument[];
}

// Função auxiliar para formatar o tamanho do arquivo
const formatBytes = (bytes: number, decimals = 2) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

const FileList: React.FC<FileListProps> = ({ files }) => {
    if (files.length === 0) {
        return <p className={styles.emptyMessage}>Nenhum arquivo enviado ainda.</p>;
    }

    return (
        <ul className={styles.fileList}>
            {files.map((file) => (
                <li key={file.id} className={styles.fileItem}>
                    <span className={styles.fileName}>{file.fileName}</span>
                    <span className={styles.fileSize}>{formatBytes(file.fileSize)}</span>
                    {/* Você pode adicionar um botão de 'excluir' aqui no futuro */}
                </li>
            ))}
        </ul>
    );
};

export default FileList;