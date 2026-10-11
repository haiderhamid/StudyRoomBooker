import { Navbar, Nav, Container } from 'react-bootstrap';
import { Link } from 'react-router-dom';

export default function NavMenu() {
  return (
    <Navbar bg="white" expand="sm" className="border-bottom shadow-sm">
      <Container fluid>
        <Navbar.Brand as={Link} to="/">StudyRoomBooker</Navbar.Brand>
        <Navbar.Toggle aria-controls="main-nav" />
        <Navbar.Collapse id="main-nav">
          <Nav className="me-auto">
            <Nav.Link as={Link} to="/">Home</Nav.Link>
            <Nav.Link as={Link} to="/rooms">Rooms</Nav.Link>
            <Nav.Link as={Link} to="/bookings">Bookings</Nav.Link>
            <Nav.Link as={Link} to="/buildings">Buildings</Nav.Link>
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
}