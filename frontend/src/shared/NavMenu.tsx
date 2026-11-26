import { useState } from 'react';
import { Navbar, Nav, Container } from 'react-bootstrap';
import { Link, NavLink, useLocation } from 'react-router-dom';
import AuthSection from '../auth/AuthSection';
import { useAuth } from '../auth/useAuth';

export default function NavMenu() {
  const { user, isAdmin } = useAuth();
  const location = useLocation();
  // Controlled, so the mobile menu closes when the page changes
  const [expanded, setExpanded] = useState<boolean>(false);
  const [menuLocation, setMenuLocation] = useState<string>(location.key);
  if (location.key !== menuLocation) {
    setMenuLocation(location.key);
    setExpanded(false);
  }

  return (
    <Navbar expand="md" sticky="top" className="srb-navbar" expanded={expanded} onToggle={setExpanded}>
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
