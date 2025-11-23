import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import type { AlertColor } from '@mui/material/Alert';

export interface ToastProps {
    open: boolean;
    message: string;
    type: AlertColor; // 'success' | 'error' | 'warning' | 'info'
    onClose: () => void;
    duration?: number;
    position?: {
        vertical: 'top' | 'bottom';
        horizontal: 'left' | 'center' | 'right';
    };
}

export default function Toast({
    open,
    message,
    type,
    onClose,
    duration = 5000,
    position = { vertical: 'top', horizontal: 'center' }
}: ToastProps) {
    return (
        <Snackbar
            open={open}
            autoHideDuration={duration}
            onClose={onClose}
            anchorOrigin={position}
            sx={{ marginTop: '60px' }} // Ajuste se tiver navbar fixa
        >
            <Alert
                onClose={onClose}
                severity={type}
                variant="filled"
                sx={{ 
                    width: '100%',
                    fontFamily: 'var(--font-principal, Roboto), sans-serif',
                    fontSize: '0.95rem',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.15)'
                }}
            >
                {message}
            </Alert>
        </Snackbar>
    );
}