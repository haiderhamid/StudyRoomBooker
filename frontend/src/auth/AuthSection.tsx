import { Nav, NavDropdown } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from './AuthContext';

export default function AuthSection() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  if (user) {
    return (
      <Nav>
        <NavDropdown title={user.sub} id="user-dropdown" align="end">
          <NavDropdown.Item onClick={handleLogout}>Log out</NavDropdown.Item>
        </NavDropdown>
      </Nav>
    );
  }

  return (
    <div className="d-flex gap-2 mt-2 mt-md-0">
      <Link to="/login" className="btn btn-outline-secondary btn-sm">Log in</Link>
      <Link to="/register" className="btn btn-primary btn-sm">Register</Link>
    </div>
  );
}
