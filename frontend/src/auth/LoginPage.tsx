import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { Form, Button, Alert, Card, Spinner } from 'react-bootstrap';
import { useAuth } from './useAuth';
import { NotRegisteredError } from './AuthService';

export default function LoginPage() {
  const [username, setUsername] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [notRegistered, setNotRegistered] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  // The page the user tried to open before logging in.
  const returnTo = location.state?.from ?? '/';

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setNotRegistered(false);
    setIsSubmitting(true);
    try {
      await login({ username, password });
      navigate(returnTo, { replace: true });
    } catch (err) {
      setNotRegistered(err instanceof NotRegisteredError);
      setError(err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="form-shell form-shell-narrow">
      <header className="page-header">
        <h1 className="page-title">Log in</h1>
        <p className="page-subtitle">Log in to book and manage rooms.</p>
      </header>
      <Card>
        <Card.Body>
          {error && (
            <Alert variant="danger">
              {error}
              {notRegistered && (
                <>
                  {' '}
                  <Link to="/register" state={{ email: username.includes('@') ? username : '' }} className="alert-link">
                    Create an account
                  </Link>
                </>
              )}
            </Alert>
          )}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="username">
              <Form.Label>Username or email</Form.Label>
              <Form.Control
                value={username}
                onChange={e => setUsername(e.target.value)}
                disabled={isSubmitting}
                required
              />
            </Form.Group>

            <Form.Group controlId="password">
              <Form.Label>Password</Form.Label>
              <Form.Control
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                disabled={isSubmitting}
                required
              />
            </Form.Group>

            <div className="form-actions">
              <Button type="submit" className="w-100" disabled={isSubmitting}>
                {isSubmitting ? <Spinner animation="border" size="sm" /> : 'Log in'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>
      <p className="form-switch-link">
        Don't have an account? <Link to="/register">Create one</Link>
      </p>
    </div>
  );
}