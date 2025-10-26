import { useState, useEffect } from 'react';
import styles from './css/LoadingSpinner.module.css'; // Vamos criar este novo CSS

const defaultLoadingMessages = [
    'Carregando...',
    'Só um momento, por favor...',
    'Processando sua solicitação...',
];

interface LoadingSpinnerProps {
    messages?: string[];
}

// usamos a desestruturação com valor padrão.
export default function LoadingSpinner({ messages = defaultLoadingMessages }: LoadingSpinnerProps) {    // Estado para o índice da mensagem atual
    const [messageIndex, setMessageIndex] = useState(0);

    // Estado para os "pulsos" interativos
    const [pulses, setPulses] = useState<{ id: number }[]>([]);

    // Efeito para ciclar as mensagens a cada 5.5 segundos
    useEffect(() => {
        const interval = setInterval(() => {
            setMessageIndex((prevIndex) =>
                (prevIndex + 1) % messages.length
            );
        }, 5500); 

        return () => clearInterval(interval);
    }, [messages]); // <-- A dependência foi adicionada

    // Função para criar um novo pulso ao clicar
    const handleClick = () => {
        const newPulse = { id: Date.now() };
        // Adiciona o novo pulso
        setPulses((prevPulses) => [...prevPulses, newPulse]);

        // Remove o pulso após a animação (1 segundo)
        setTimeout(() => {
            setPulses((prev) => prev.filter((p) => p.id !== newPulse.id));
        }, 1000);
    };

    return (
        // Usamos um container de tela cheia para "bloquear" a UI
        <div className={styles.loadingContainer}>
            
            {/* O Orbe Interativo */}
            <div className={styles.orbContainer} onClick={handleClick}>
                <div className={styles.orb}></div>
                {/* Mapeia os pulsos para renderizá-los */}
                {pulses.map(pulse => (
                    <span key={pulse.id} className={styles.pulse} />
                ))}
            </div>

            {/* O Texto Dinâmico */}
            <div className={styles.textContainer}>
                {messages.map((message, index) => (
                    <p
                        key={index}
                        className={`${styles.statusText} ${index === messageIndex ? styles.visible : ''}`}
                    >
                        {message}
                    </p>
                ))}
            </div>

            {/* O Subtexto Fixo */}
            <p className={styles.subText}>
                Este processo pode levar alguns minutos, não feche esta aba.
            </p>
        </div>
    );
}