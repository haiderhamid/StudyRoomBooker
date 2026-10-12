import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import BookingForm from './BookingForm';
import type { Booking } from '../types/booking';
import * as BookingService from './BookingService';

export default function BookingCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();

  const mutation = useMutation<Booking, Error, Booking>({
    mutationFn: BookingService.createBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings'] });
      navigate('/bookings');
    },
  });

  return (
    <div className="booking-page">
      <header className="page-header">
        <h1 className="page-title">Book a room</h1>
        <p className="page-subtitle">Choose a room, a day and a time.</p>
      </header>
      <BookingForm
        defaultRoomId={searchParams.get('roomId') ?? undefined}
        defaultDate={searchParams.get('date') ?? undefined}
        defaultFromTime={searchParams.get('from') ?? undefined}
        defaultToTime={searchParams.get('to') ?? undefined}
        onSubmit={booking => mutation.mutate(booking)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Book room"
      />
    </div>
  );
}