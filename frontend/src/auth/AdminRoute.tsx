import { Navigate, Outlet, Link, useLocation } from 'react-router-dom';
import { useAuth } from './useAuth';

export default function AdminRoute() {
  const { token, isAdmin, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return null;
  }

  if (!token) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  if (!isAdmin) {
    return (
      <div className="form-shell form-shell-narrow">
        <header className="page-header">
          <h1 className="page-title">No access</h1>
          <p className="page-subtitle">Only administrators can manage rooms and buildings.</p>
        </header>
        <div className="form-actions justify-content-center">
          <Link to="/" className="btn btn-primary">Find a room</Link>
        </div>
      </div>
    );
  }

  return <Outlet />;
}
