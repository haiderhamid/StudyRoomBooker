import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="form-shell form-shell-narrow">
      <header className="page-header">
        <h1 className="page-title">Page not found</h1>
        <p className="page-subtitle">The page you are looking for does not exist.</p>
      </header>
      <div className="form-actions justify-content-center">
        <Link to="/" className="btn btn-primary">Find a room</Link>
      </div>
    </div>
  );
}
