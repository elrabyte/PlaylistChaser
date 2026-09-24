import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { LoginPage } from "./pages/LoginPage";
import { PlaylistsPage } from "./pages/PlaylistsPage";
import { PlaylistDetailPage } from "./pages/PlaylistDetailPage";

function RequireAuth({ children }: { children: React.ReactElement }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/playlists"
        element={
          <RequireAuth>
            <PlaylistsPage />
          </RequireAuth>
        }
      />
      <Route
        path="/playlists/:id"
        element={
          <RequireAuth>
            <PlaylistDetailPage />
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/playlists" replace />} />
    </Routes>
  );
}
