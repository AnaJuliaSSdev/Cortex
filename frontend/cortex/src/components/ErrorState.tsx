export const ErrorState: React.FC<{ message: string }> = ({ message }) => (
     <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--primary)', border: '1px solid var(--primary)', borderRadius: '8px', backgroundColor: '#fef4f2' }}>
        <h3 style={{ margin: 0 }}>Ocorreu um Erro</h3>
        <p>{message}</p>
    </div>
);