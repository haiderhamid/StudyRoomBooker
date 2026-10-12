import { Navbar, Nav, Container } from 'react-bootstrap';
import { Link, NavLink } from 'react-router-dom';
import AuthSection from '../auth/AuthSection';
import { useAuth } from '../auth/AuthContext';

export default function NavMenu() {
  const { user, isAdmin } = useAuth();

  return (
    <Navbar expand="md" sticky="top" className="srb-navbar">
      <Container>
        <Navbar.Brand as={Link} to="/">StudyRoomBooker</Navbar.Brand>
        <Navbar.Toggle aria-controls="main-nav" />
        <Navbar.Collapse id="main-nav">
          <Nav className="me-auto">
            <Nav.Link as={NavLink} to="/" end>Find a room</Nav.Link>
            <Nav.Link as={NavLink} to="/rooms">Rooms</Nav.Link>
            {user && <Nav.Link as={NavLink} to="/bookings">My bookings</Nav.Link>}
            {isAdmin && <Nav.Link as={NavLink} to="/buildings">Buildings</Nav.Link>}
          </Nav>
          <AuthSection />
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
}
