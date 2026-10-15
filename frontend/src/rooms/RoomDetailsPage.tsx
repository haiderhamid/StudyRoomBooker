import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Spinner, Alert } from 'react-bootstrap';
import type { Room } from '../types/room';
import type { Booking } from '../types/booking';
import * as RoomService from './RoomService';
import * as BookingService from '../bookings/BookingService';
import { useAuth } from '../auth/useAuth';
import { addDays, startOfWeek, toDateString } from '../shared/dateTime';
import RoomTags from '../shared/RoomTags';
import WeekSchedule from './WeekSchedule';

export default function RoomDetailsPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const [weekStart, setWeekStart] = useState<Date>(() => startOfWeek(new Date()));
  const { user, isAdmin } = useAuth();

  const { data: room, isLoading, error } = useQuery<Room, Error>({
    queryKey: ['room', roomId],
    queryFn: () => RoomService.fetchRoomById(roomId!),
    enabled: !!roomId,
  });

  const { data: bookings = [], isLoading: bookingsLoading, error: bookingsError } = useQuery<Booking[], Error>({
    queryKey: ['bookings', user?.sub ?? ''],
    queryFn: BookingService.fetchBookings,
  });

  if (isLoading) {
    return (
      <Spinner animation="border" role="status" className="my-4">
        <span className="visually-hidden">Loading...</span>
      </Spinner>
    );
  }

  if (error) {
    return <Alert variant="danger">Could not load room: {error.message}</Alert>;
  }

  if (!room) {
    return <Alert variant="warning">Room not found.</Alert>;
  }

  const weekEnd = toDateString(addDays(weekStart, 7));
  const weekBookings = bookings.filter(b =>
    b.roomId === room.id &&
    b.startTime.substring(0, 10) >= toDateString(weekStart) &&
    b.startTime.substring(0, 10) < weekEnd);

  const isMine = (booking: Booking) => !!user && booking.bookedBy === user.sub;
  const canManage = (booking: Booking) => isAdmin || isMine(booking);

  return (
    <>
      <Link to="/rooms" className="back-link">‹ All rooms</Link>

      <header className="page-header">
        <div>
          <h1 className="page-title">Room {room.roomNumber}</h1>
          <p className="page-subtitle">{room.buildingName}</p>
          <RoomTags room={room} />
        </div>
        <div className="d-flex gap-2">
          {isAdmin && (
            <Link to={`/rooms/update/${room.id}`} className="btn btn-outline-secondary">Edit room</Link>
          )}
          <Link to={`/bookings/create?roomId=${room.id}`} className="btn btn-primary">Book this room</Link>
        </div>
      </header>

      {bookingsLoading && (
        <Spinner animation="border" role="status" className="my-4">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
      )}

      {bookingsError && (
        <Alert variant="danger">Could not load the schedule: {bookingsError.message}</Alert>
      )}

      {!bookingsLoading && !bookingsError && (
        <WeekSchedule
          bookings={weekBookings}
          weekStart={weekStart}
          onPreviousWeek={() => setWeekStart(addDays(weekStart, -7))}
          onNextWeek={() => setWeekStart(addDays(weekStart, 7))}
          onThisWeek={() => setWeekStart(startOfWeek(new Date()))}
          isMine={isMine}
          canManage={canManage}
        />
      )}
    </>
  );
}
