import { Link } from 'react-router-dom';

export default function HomePage() {
  return (
    <div className="text-center mt-5">
      <h1 className="display-5">Study Room Booker</h1>
      <p className="lead">Find and book a study room at OsloMet.</p>
      <Link to="/rooms" className="btn btn-primary me-2">Find a room</Link>
      <Link to="/bookings" className="btn btn-outline-secondary">See bookings</Link>
    </div>
  );
}