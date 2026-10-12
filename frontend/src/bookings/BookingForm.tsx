import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Form, Button, Alert, Card, Row, Col } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Booking } from '../types/booking';
import type { Room } from '../types/room';
import * as RoomService from '../rooms/RoomService';
import { timeOptions, todayString } from '../shared/dateTime';
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
  bookedBy?: string;
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
  const [bookedBy, setBookedBy] = useState<string>(initialValues?.bookedBy ?? '');
  const [errors, setErrors] = useState<BookingFormErrors>({});

  const { data: rooms = [] } = useQuery<Room[], Error>({
    queryKey: ['rooms', '', '', ''],
    queryFn: () => RoomService.fetchRooms({ buildingId: '', minCapacity: '', facilityId: '' }),
  });

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
    if (!bookedBy.trim()) {
      newErrors.bookedBy = 'Please enter who booked the room.';
    } else if (bookedBy.length > 100) {
      newErrors.bookedBy = 'Name can be at most 100 characters.';
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
      bookedBy: bookedBy.trim(),
    });
  };

  return (
    <Card>
      <Card.Body>
        {submitError && <Alert variant="danger">{submitError}</Alert>}

        <Form noValidate onSubmit={handleSubmit}>
          <Form.Group className="mb-3" controlId="roomId">
            <Form.Label>Room</Form.Label>
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
            <Form.Label>Subject</Form.Label>
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
              onChange={setDate}
              minDate={initialValues ? undefined : todayString()}
              isInvalid={!!errors.date}
            />
            <Form.Control.Feedback type="invalid">{errors.date}</Form.Control.Feedback>
          </Form.Group>

          <Row>
            <Col>
              <Form.Group className="mb-3" controlId="fromTime">
                <Form.Label>Start time</Form.Label>
                <Form.Select
                  value={fromTime}
                  onChange={e => setFromTime(e.target.value)}
                  isInvalid={!!errors.fromTime}
                >
                  <option value="">Select</option>
                  {timeOptions.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </Form.Select>
                <Form.Control.Feedback type="invalid">{errors.fromTime}</Form.Control.Feedback>
              </Form.Group>
            </Col>
            <Col>
              <Form.Group className="mb-3" controlId="toTime">
                <Form.Label>End time</Form.Label>
                <Form.Select
                  value={toTime}
                  onChange={e => setToTime(e.target.value)}
                  isInvalid={!!errors.toTime}
                >
                  <option value="">Select</option>
                  {timeOptions.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </Form.Select>
                <Form.Control.Feedback type="invalid">{errors.toTime}</Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Form.Group controlId="bookedBy">
            <Form.Label>Booked by</Form.Label>
            <Form.Control
              value={bookedBy}
              onChange={e => setBookedBy(e.target.value)}
              isInvalid={!!errors.bookedBy}
            />
            <Form.Control.Feedback type="invalid">{errors.bookedBy}</Form.Control.Feedback>
          </Form.Group>

          <div className="form-actions">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : submitLabel}
            </Button>
            <Link to="/bookings" className="btn btn-quiet">Cancel</Link>
          </div>
        </Form>
      </Card.Body>
    </Card>
  );
}