import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { ApiError } from "../api";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(email, password);
      navigate("/playlists");
    } catch (err) {
      setError(err instanceof ApiError ? "Invalid email or password." : "Could not reach the server.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="page page--centered">
      <form className="card" onSubmit={handleSubmit}>
        <h1>PlaylistChaser</h1>
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
        </label>
        <label>
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy}>
          {busy ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </div>
  );
}
