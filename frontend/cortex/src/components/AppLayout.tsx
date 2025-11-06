import { Outlet } from 'react-router-dom';
import Header from './Header';
import styles from './css/AppLayout.module.css';

export default function AppLayout() {
    return (
        <div className={styles.appContainer}>
            <Header />
            <main className={styles.mainContent}>
                <Outlet />
            </main>
        </div>
    );
}