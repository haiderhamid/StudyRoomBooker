import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Spinner, Alert } from 'react-bootstrap';
import BookingForm from './BookingForm';
import type { Booking } from '../types/booking';
import * as BookingService from './BookingService';
import { useAuth } from '../auth/useAuth';

export default function BookingUpdatePage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user, isAdmin } = useAuth();

  const { data: booking, isLoading, error } = useQuery<Booking, Error>({
    queryKey: ['booking', bookingId],
    queryFn: () => BookingService.fetchBookingById(bookingId!),
    enabled: !!bookingId,
  });

  const mutation = useMutation<Booking, Error, Booking>({
    mutationFn: (updatedBooking: Booking) => BookingService.updateBooking(updatedBooking.id, updatedBooking),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings'] });
      queryClient.invalidateQueries({ queryKey: ['booking', bookingId] });
      navigate('/bookings');
    },
  });

  if (isLoading) {
    return (
      <Spinner animation="border" role="status">
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

  // Only the owner or an admin can edit, and not after the booking has started.
  if (!isAdmin && booking.bookedBy !== user?.sub) {
    return <Alert variant="warning">You can only edit your own bookings.</Alert>;
  }

  if (!isAdmin && new Date(booking.startTime) <= new Date()) {
    return <Alert variant="warning">This booking has already started and cannot be changed.</Alert>;
  }

  return (
    <div className="booking-page">
      <header className="page-header">
        <h1 className="page-title">Edit booking</h1>
        <p className="page-subtitle">Change the room, day or time for this booking.</p>
      </header>
      <BookingForm
        initialValues={booking}
        onSubmit={updatedBooking => mutation.mutate(updatedBooking)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Save changes"
      />
    </div>
  );
}