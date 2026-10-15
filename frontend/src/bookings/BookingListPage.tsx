import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Spinner, Alert } from 'react-bootstrap';
import type { Booking } from '../types/booking';
import * as BookingService from './BookingService';
import { useAuth } from '../auth/useAuth';
import { todayString, toDateString } from '../shared/dateTime';

type View = 'upcoming' | 'past';

function formatDayHeading(date: string): string {
  const today = todayString();
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);

  if (date === today) return 'Today';
  if (date === toDateString(tomorrow)) return 'Tomorrow';

  return new Date(`${date}T00:00:00`).toLocaleDateString('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });
}

export default function BookingListPage() {
  const [view, setView] = useState<View>('upcoming');
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const { data: bookings = [], isLoading, error } = useQuery<Booking[], Error>({
    queryKey: ['bookings', 'mine', user?.sub ?? ''],
    queryFn: BookingService.fetchMyBookings,
  });

  const deleteMutation = useMutation<void, Error, number>({
    mutationFn: BookingService.deleteBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings'] });
    },
  });

  const handleDelete = (booking: Booking) => {
    if (window.confirm(`Delete the booking "${booking.subject}"?`)) {
      deleteMutation.mutate(booking.id);
    }
  };

  const now = new Date();
  const hasStarted = (booking: Booking) => new Date(booking.startTime) <= now;
  const hasEnded = (booking: Booking) => new Date(booking.endTime) <= now;
  const visibleBookings = bookings
    .filter(b => (view === 'upcoming' ? !hasEnded(b) : hasEnded(b)))
    .sort((a, b) => (view === 'upcoming'
      ? a.startTime.localeCompare(b.startTime)
      : b.startTime.localeCompare(a.startTime)));

  const dates: string[] = [];
  for (const booking of visibleBookings) {
    const date = booking.startTime.substring(0, 10);
    if (!dates.includes(date)) {
      dates.push(date);
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">My bookings</h1>
          <p className="page-subtitle">The study rooms you have booked.</p>
        </div>
        <Link to="/" className="btn btn-primary">Book a room</Link>
      </header>

      <div className="segmented" role="group" aria-label="Show bookings">
        <button
          type="button"
          className={view === 'upcoming' ? 'is-active' : ''}
          aria-pressed={view === 'upcoming'}
          onClick={() => setView('upcoming')}
        >
          Upcoming
        </button>
        <button
          type="button"
          className={view === 'past' ? 'is-active' : ''}
          aria-pressed={view === 'past'}
          onClick={() => setView('past')}
        >
          Past
        </button>
      </div>

      {deleteMutation.error && (
        <Alert variant="danger" dismissible onClose={() => deleteMutation.reset()}>
          {deleteMutation.error.message}
        </Alert>
      )}

      {isLoading && (
        <Spinner animation="border" role="status" className="my-4">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
      )}

      {error && <Alert variant="danger">Could not load your bookings: {error.message}</Alert>}

      {!isLoading && !error && visibleBookings.length === 0 && (
        <div className="empty-state">
          <p>{view === 'upcoming' ? 'You have no upcoming bookings.' : 'You have no past bookings.'}</p>
          {view === 'upcoming' && <Link to="/" className="btn btn-outline-secondary">Find a room</Link>}
        </div>
      )}

      {dates.map(date => (
        <section key={date} className="day-group">
          <h2 className="day-heading">{formatDayHeading(date)}</h2>
          <ul className="item-rows">
            {visibleBookings.filter(b => b.startTime.substring(0, 10) === date).map(booking => (
              <li key={booking.id} className="item-row">
                <span className="booking-time">
                  {booking.startTime.substring(11, 16)}–{booking.endTime.substring(11, 16)}
                </span>
                <div className="item-main">
                  <span className="item-title">{booking.subject}</span>
                  <span className="item-meta">
                    <Link to={`/rooms/${booking.roomId}`}>Room {booking.roomNumber}</Link> · {booking.buildingName}
                  </span>
                </div>
                {!hasStarted(booking) && (
                  <div className="item-actions">
                    <Link to={`/bookings/update/${booking.id}`} className="btn btn-sm btn-quiet">Edit</Link>
                    <button type="button" className="btn btn-sm btn-quiet btn-quiet-danger" onClick={() => handleDelete(booking)}>
                      Delete
                    </button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </section>
      ))}
    </>
  );
}
