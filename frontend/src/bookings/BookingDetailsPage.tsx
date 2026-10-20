import type { ReactNode } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Spinner, Alert } from 'react-bootstrap';
import type { Booking } from '../types/booking';
import * as BookingService from './BookingService';
import { useAuth } from '../auth/useAuth';

export default function BookingDetailsPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { user, isAdmin } = useAuth();

  const { data: booking, isLoading, error } = useQuery<Booking, Error>({
    queryKey: ['booking', bookingId, user?.sub ?? ''],
    queryFn: () => BookingService.fetchBookingById(bookingId!),
    enabled: !!bookingId,
  });

  // Join and leave work the same way, so they share what happens afterwards.
  const onSuccess = () => {
    queryClient.invalidateQueries({ queryKey: ['booking'] });
    queryClient.invalidateQueries({ queryKey: ['bookings'] });
  };
  const joinMutation = useMutation<void, Error, number>({ mutationFn: BookingService.joinBooking, onSuccess });
  const leaveMutation = useMutation<void, Error, number>({ mutationFn: BookingService.leaveBooking, onSuccess });

  if (isLoading) {
    return (
      <Spinner animation="border" role="status" className="my-4">
        <span className="visually-hidden">Loading...</span>
      </Spinner>
    );
  }

  if (error) {
    return <Alert variant="danger">Could not load booking: {error.message}</Alert>;
  }

  if (!booking) {
    return <Alert variant="warning">Booking not found.</Alert>;
  }

  const now = new Date();
  const hasStarted = new Date(booking.startTime) <= now;
  const hasEnded = new Date(booking.endTime) <= now;
  const isOwner = !!user && booking.bookedBy === user.sub;
  const canManage = isAdmin || isOwner;
  const isFull = (booking.seatsTaken ?? 1) >= (booking.roomCapacity ?? 0);
  const actionError = joinMutation.error ?? leaveMutation.error;

  const date = new Date(booking.startTime).toLocaleDateString('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });

  // Picks the right button for the user: edit your own, leave if joined, or join if there is room.
  let actions: ReactNode;
  if (canManage && (isAdmin || !hasStarted) && !hasEnded) {
    actions = <Link to={`/bookings/update/${booking.id}`} className="btn btn-primary">Edit</Link>;
  } else if (hasEnded) {
    actions = <p className="notice">This booking has ended.</p>;
  } else if (isOwner) {
    actions = <p className="notice">This booking has started and can't be changed.</p>;
  } else if (booking.isJoined) {
    actions = (
      <button type="button" className="btn btn-outline-secondary" disabled={leaveMutation.isPending}
        onClick={() => leaveMutation.mutate(booking.id)}>
        Leave
      </button>
    );
  } else if (!user) {
    actions = (
      <Link to="/login" state={{ from: location.pathname }} className="btn btn-primary">Log in to join</Link>
    );
  } else if (isFull) {
    actions = <p className="notice">This booking is full.</p>;
  } else {
    actions = (
      <button type="button" className="btn btn-primary" disabled={joinMutation.isPending}
        onClick={() => joinMutation.mutate(booking.id)}>
        Join
      </button>
    );
  }

  return (
    <div className="form-shell form-shell-narrow">
      <header className="page-header">
        <h1 className="page-title">{booking.subject}</h1>
        <p className="page-subtitle">{booking.seatsTaken} of {booking.roomCapacity} seats taken</p>
      </header>

      {actionError && <Alert variant="danger">{actionError.message}</Alert>}
      {joinMutation.isSuccess && booking.isJoined && <Alert variant="success">You have joined this booking.</Alert>}

      <div className="card">
        <div className="card-body">
          <dl className="detail-list">
            <dt>Room</dt>
            <dd><Link to={`/rooms/${booking.roomId}`}>Room {booking.roomNumber}</Link>, {booking.buildingName}</dd>
            <dt>Date</dt>
            <dd>{date}</dd>
            <dt>Time</dt>
            <dd>{booking.startTime.substring(11, 16)}–{booking.endTime.substring(11, 16)}</dd>
            {/* The API only sends names to the owner and admins. */}
            {canManage && (
              <>
                <dt>Booked by</dt>
                <dd>{booking.bookedBy}</dd>
                <dt>Joined</dt>
                <dd>{booking.participants?.length ? booking.participants.join(', ') : 'Nobody yet'}</dd>
              </>
            )}
          </dl>

          <div className="form-actions">
            {actions}
            <Link to={`/rooms/${booking.roomId}`} className="btn btn-quiet">Back to room</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
