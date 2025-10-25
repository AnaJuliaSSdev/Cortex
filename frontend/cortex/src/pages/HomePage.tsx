import React, { useState } from 'react';
import CreateAnalysisModal from '../components/CreateAnalysisModal';
import Logo from '../components/Logo';

// Componente simples para o layout da página, inspirado nas referências
const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: 'var(--background-light)' }}>
        <header style={{
            padding: '1rem 2rem', borderBottom: '1px solid var(--background-medium)',
            backgroundColor: 'var(--background-light)', color: 'var(--text-dark)',
            display: 'flex', alignItems: 'center', gap: '0.5rem'
        }}>
			     {/* Um logo simples do CORTEX */}
			<div style={{margin :"0",}}>
				<Logo />
			</div>
        </header>
        <main style={{ flex: 1, padding: '2rem' }}>
            {children}
        </main>
    </div>
);


export default function HomePage() {
    // Estado para controlar se o modal está aberto ou fechado
    const [isModalOpen, setIsModalOpen] = useState(false);
    
    // Por enquanto, não temos análises para listar
    const analyses: any[] = []; // No futuro, isso virá de uma chamada de API

    return (
        <MainLayout>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
                <h2 style={{ fontSize: '1.75rem', color: 'var(--text-dark)', margin: 0 }}>Suas Análises</h2>
                <button 
                    onClick={() => setIsModalOpen(true)}
                    style={{
                        padding: '0.75rem 1.5rem', background: 'var(--primary)', color: 'white',
                        border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 500
                    }}
                >
                    + Criar Nova Análise
                </button>
            </div>

            {/* Conteúdo Principal: Mostra o estado vazio se não houver análises */}
            {analyses.length === 0 ? (
                <div style={{
                    textAlign: 'center', padding: '4rem', border: '2px dashed var(--background-medium)',
                    borderRadius: '8px', backgroundColor: '#FDFDFC'
                }}>
                    <h3 style={{ color: 'var(--text-medium)', fontWeight: 500 }}>Sua biblioteca está vazia</h3>
                    <p style={{ color: 'var(--text-light)' }}>Crie sua primeira análise para começar a fazer upload de arquivos e extrair insights.</p>
                </div>
            ) : (
                <div>
                    {/* Aqui é onde você vai mapear e listar as análises no futuro */}
                </div>
            )}
            
            {/* O Modal é renderizado aqui, mas só é visível se isModalOpen for true */}
            <CreateAnalysisModal 
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)} 
            />
        </MainLayout>
    );
}