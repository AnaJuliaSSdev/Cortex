import type { AlertColor } from "@mui/material/Alert";

export default interface ToastState {
    open: boolean;
    message: string;
    type: AlertColor;
}

