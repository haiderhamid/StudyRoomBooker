export default function PrivacyPage() {
  return (
    <div className="form-shell">
      <header className="page-header">
        <h1 className="page-title">Privacy</h1>
        <p className="page-subtitle">How StudyRoomBooker handles your data.</p>
      </header>

      <div className="card">
        <div className="card-body">
          <h2 className="day-heading">What we store</h2>
          <p>When you create an account we store your email address and a hashed password. When you book a room we store the room, the time, the subject and who made the booking.</p>
          <h2 className="day-heading">Who can see it</h2>
          <p>Everyone can see when a room is booked and the subject of the booking. Your email address is only shown to you and to administrators.</p>
          <h2 className="day-heading">Your choices</h2>
          <p className="mb-0">You can change or delete your own bookings at any time from My bookings.</p>
        </div>
      </div>
    </div>
  );
}
