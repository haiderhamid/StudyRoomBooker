import { useState, useEffect } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Form, Button, Alert, Card, Spinner } from 'react-bootstrap';
import type { RegisterDto } from '../types/auth';
import * as AuthService from './AuthService';

export default function RegisterPage() {
  const [formData, setFormData] = useState<RegisterDto>({ username: '', email: '', password: '' });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (success) {
      const timer = setTimeout(() => navigate('/login'), 2000);
      return () => clearTimeout(timer);
    }
  }, [success, navigate]);

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await AuthService.register(formData);
      setSuccess('Registration successful! Redirecting to login...');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const isDisabled = isSubmitting || !!success;

  return (
    <div className="form-shell form-shell-narrow">
      <header className="page-header">
        <h1 className="page-title">Create account</h1>
        <p className="page-subtitle">One account to book study rooms.</p>
      </header>
      <Card>
        <Card.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          {success && <Alert variant="success">{success}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="username">
              <Form.Label>Username</Form.Label>
              <Form.Control
                name="username"
                value={formData.username}
                onChange={handleChange}
                disabled={isDisabled}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3" controlId="email">
              <Form.Label>Email</Form.Label>
              <Form.Control
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                disabled={isDisabled}
                required
              />
            </Form.Group>

            <Form.Group controlId="password">
              <Form.Label>Password</Form.Label>
              <Form.Control
                type="password"
                name="password"
                value={formData.password}
                onChange={handleChange}
                disabled={isDisabled}
                required
              />
              <Form.Text muted>
                At least 6 characters, with uppercase, lowercase, a number and a symbol.
              </Form.Text>
            </Form.Group>

            <div className="form-actions">
              <Button type="submit" className="w-100" disabled={isDisabled}>
                {isSubmitting ? <Spinner animation="border" size="sm" /> : 'Create account'}
              </Button>
            </div>
          </Form>
        </Card.Body>
      </Card>
      <p className="form-switch-link">
        Already have an account? <Link to="/login">Log in</Link>
      </p>
    </div>
  );
}