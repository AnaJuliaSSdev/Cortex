import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute";
import LoginPage from "./pages/LoginPage.tsx";
import HomePage from "./pages/HomePage.tsx";
import RegisterPage from "./pages/RegisterPage.tsx";
import AnalysisPage from "./pages/AnalysisPage.tsx";
import AppLayout from "./components/AppLayout.tsx";
function App() {
     return (
         <BrowserRouter>
             <Routes>
                 <Route path="/login" element={<LoginPage />} />
                 <Route path="/register" element={<RegisterPage />} />
                 <Route
                     element={
                         <ProtectedRoute>
                             <AppLayout /> 
                         </ProtectedRoute>
                     }
                 >
                     <Route path="/" element={<HomePage />} />
                     <Route path="/analysis/:id" element={<AnalysisPage />} />
                 </Route>
                 
                 <Route path="*" element={<Navigate to="/" replace />} />
             </Routes>
         </BrowserRouter>
     );
}

export default App;
