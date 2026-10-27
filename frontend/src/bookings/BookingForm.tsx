import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Form, Button, Alert, Card, Row, Col } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Booking } from '../types/booking';
import type { Room } from '../types/room';
import * as BookingService from './BookingService';
import { useAuth } from '../auth/useAuth';
import WeekSchedule from '../rooms/WeekSchedule';
import * as RoomService from '../rooms/RoomService';
import { addDays, availableTimes, moveEndTime, startOfWeek, toDateString, todayString } from '../shared/dateTime';
import DatePicker from '../shared/DatePicker';

interface BookingFormProps {
  initialValues?: Booking;
  defaultRoomId?: string;
  defaultDate?: string;
  defaultFromTime?: string;
  defaultToTime?: string;
  onSubmit: (booking: Booking) => void;
  isSubmitting: boolean;
  submitError: string | null;
  submitLabel: string;
}

interface BookingFormErrors {
  roomId?: string;
  subject?: string;
  date?: string;
  fromTime?: string;
  toTime?: string;
}

export default function BookingForm({
  initialValues,
  defaultRoomId,
  defaultDate,
  defaultFromTime,
  defaultToTime,
  onSubmit,
  isSubmitting,
  submitError,
  submitLabel,
}: BookingFormProps) {
  const [roomId, setRoomId] = useState<string>(initialValues ? String(initialValues.roomId) : (defaultRoomId ?? ''));
  const [subject, setSubject] = useState<string>(initialValues?.subject ?? '');
  const [date, setDate] = useState<string>(initialValues ? initialValues.startTime.substring(0, 10) : (defaultDate || todayString()));
  const [fromTime, setFromTime] = useState<string>(initialValues ? initialValues.startTime.substring(11, 16) : (defaultFromTime ?? ''));
  const [toTime, setToTime] = useState<string>(initialValues ? initialValues.endTime.substring(11, 16) : (defaultToTime ?? ''));
  const [errors, setErrors] = useState<BookingFormErrors>({});
  const [weekStart, setWeekStart] = useState<Date>(() => startOfWeek(new Date(`${date}T00:00:00`)));
  const { user } = useAuth();

  const { data: rooms = [] } = useQuery<Room[], Error>({
    queryKey: ['rooms', '', '', ''],
    queryFn: () => RoomService.fetchRooms({ buildingId: '', minCapacity: '' }),
  });

  const { data: bookings = [], error: bookingsError } = useQuery<Booking[], Error>({
    queryKey: ['bookings', user?.sub ?? ''],
    queryFn: BookingService.fetchBookings,
  });

  // Moves the end time when the start time changes.
  const handleFromTimeChange = (newFrom: string) => {
    setToTime(moveEndTime(fromTime, toTime, newFrom));
    setFromTime(newFrom);
  };

  const handleDateChange = (newDate: string) => {
    setDate(newDate);
    setWeekStart(startOfWeek(new Date(`${newDate}T00:00:00`)));
  };

  const weekEnd = toDateString(addDays(weekStart, 7));
  // Bookings for the chosen room and week. When editing, the booking itself is left out.
  const roomBookings = bookings.filter(b =>
    String(b.roomId) === roomId &&
    b.id !== initialValues?.id &&
    b.startTime.substring(0, 10) >= toDateString(weekStart) &&
    b.startTime.substring(0, 10) < weekEnd);
  const selection = fromTime && toTime && toTime > fromTime ? { date, from: fromTime, to: toTime } : undefined;
  // Bookings you made or joined get their own colour in the schedule.
  const isMine = (booking: Booking) => !!user && (booking.bookedBy === user.sub || !!booking.isJoined);

  // Checks the form before it is sent. The API checks everything again.
  const validate = (): BookingFormErrors => {
    const newErrors: BookingFormErrors = {};
    if (!roomId) {
      newErrors.roomId = 'Please select a room.';
    }
    if (!subject.trim()) {
      newErrors.subject = 'Subject is required.';
    } else if (subject.length > 100) {
      newErrors.subject = 'Subject can be at most 100 characters.';
    }
    if (!date) {
      newErrors.date = 'Please select a date.';
    }
    if (!fromTime) {
      newErrors.fromTime = 'Please select a start time.';
    }
    if (!toTime) {
      newErrors.toTime = 'Please select an end time.';
    } else if (fromTime && toTime <= fromTime) {
      newErrors.toTime = 'End time must be after start time.';
    }
    return newErrors;
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const newErrors = validate();
    setErrors(newErrors);
    if (Object.keys(newErrors).length > 0) {
      return;
    }
    onSubmit({
      id: initialValues?.id ?? 0,
      roomId: Number(roomId),
      subject: subject.trim(),
      startTime: `${date}T${fromTime}:00`,
      endTime: `${date}T${toTime}:00`,
      // The API sets BookedBy from the token, so it is not part of the form.
      bookedBy: initialValues?.bookedBy ?? '',
    });
  };

  return (
    <>
      <Card>
        <Card.Body>
          {submitError && <Alert variant="danger">{submitError}</Alert>}

          <Form noValidate onSubmit={handleSubmit}>
            <Form.Group className="mb-3" controlId="roomId">
              <Form.Label className="required">Room</Form.Label>
              <Form.Select
                value={roomId}
                onChange={e => setRoomId(e.target.value)}
                isInvalid={!!errors.roomId}
              >
                <option value="">Select a room</option>
                {rooms.map(r => (
                  <option key={r.id} value={r.id}>Room {r.roomNumber}, {r.buildingName} ({r.capacity} seats)</option>
                ))}
              </Form.Select>
              <Form.Control.Feedback type="invalid">{errors.roomId}</Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3" controlId="subject">
              <Form.Label className="required">Subject</Form.Label>
              <Form.Control
                value={subject}
                onChange={e => setSubject(e.target.value)}
                isInvalid={!!errors.subject}
              />
              <Form.Control.Feedback type="invalid">{errors.subject}</Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3" controlId="date">
              <Form.Label>Date</Form.Label>
              <DatePicker
                id="date"
                value={date}
                onChange={handleDateChange}
                minDate={initialValues ? undefined : todayString()}
                isInvalid={!!errors.date}
              />
              <Form.Control.Feedback type="invalid">{errors.date}</Form.Control.Feedback>
            </Form.Group>

            <Row>
              <Col>
                <Form.Group controlId="fromTime">
                  <Form.Label>Start time</Form.Label>
                  <Form.Select
                    value={fromTime}
                    onChange={e => handleFromTimeChange(e.target.value)}
                    isInvalid={!!errors.fromTime}
                  >
                    <option value="">Select</option>
                    {availableTimes(date, fromTime).map(t => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </Form.Select>
                  <Form.Control.Feedback type="invalid">{errors.fromTime}</Form.Control.Feedback>
                </Form.Group>
              </Col>
              <Col>
                <Form.Group controlId="toTime">
                  <Form.Label>End time</Form.Label>
                  <Form.Select
                    value={toTime}
                    onChange={e => setToTime(e.target.value)}
                    isInvalid={!!errors.toTime}
                  >
                    <option value="">Select</option>
                    {availableTimes(date, toTime).map(t => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </Form.Select>
                  <Form.Control.Feedback type="invalid">{errors.toTime}</Form.Control.Feedback>
                </Form.Group>
              </Col>
            </Row>


            <div className="form-actions">
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Saving...' : submitLabel}
              </Button>
              <Link to="/bookings" className="btn btn-quiet">Cancel</Link>
            </div>
          </Form>
        </Card.Body>
      </Card>

      <div className="booking-schedule">
        {bookingsError && (
          <Alert variant="danger">Could not load the schedule: {bookingsError.message}</Alert>
        )}
        {!bookingsError && roomId && (
          <WeekSchedule
            bookings={roomBookings}
            weekStart={weekStart}
            onPreviousWeek={() => setWeekStart(addDays(weekStart, -7))}
            onNextWeek={() => setWeekStart(addDays(weekStart, 7))}
            onThisWeek={() => setWeekStart(startOfWeek(new Date()))}
            isMine={isMine}
            selection={selection}
          />
        )}
        {!bookingsError && !roomId && (
          <p className="empty-state">Choose a room to see when it is free.</p>
        )}
      </div>
    </>
  );
}