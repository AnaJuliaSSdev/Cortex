import { createRoot } from 'react-dom/client'
import App from './App.tsx'
import { AuthProvider } from '../src/contexts/AuthContext.tsx'
import React from 'react'

createRoot(document.getElementById('root')!).render(
 <React.StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </React.StrictMode>
)
