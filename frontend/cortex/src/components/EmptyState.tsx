export // Componente de Estado Vazio
const EmptyState: React.FC = () => (
    <div style={{
        textAlign: 'center', padding: '4rem', border: '2px dashed var(--background-medium)',
        borderRadius: '8px', backgroundColor: '#FDFDFC'
    }}>
        <h3 style={{ color: 'var(--text-medium)', fontWeight: 500 }}>Sua biblioteca está vazia</h3>
        <p style={{ color: 'var(--text-light)' }}>Crie sua primeira análise para começar a fazer upload de arquivos e extrair insights.</p>
    </div>
);