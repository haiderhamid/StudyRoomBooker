import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Form, Row, Col, Spinner, Alert } from 'react-bootstrap';
import type { Room } from '../types/room';
import type { Building } from '../types/building';
import type { Booking } from '../types/booking';
import type { Facility } from '../types/facility';
import * as RoomService from '../rooms/RoomService';
import * as BuildingService from '../buildings/BuildingService';
import * as BookingService from '../bookings/BookingService';
import * as FacilityService from '../facilities/FacilityService';
import { availableTimes, moveEndTime, todayString } from '../shared/dateTime';
import DatePicker from '../shared/DatePicker';
import RoomTags from '../shared/RoomTags';
import AvailabilityBar from './AvailabilityBar';

export default function HomePage() {
  const [date, setDate] = useState<string>(todayString());
  const [fromTime, setFromTime] = useState<string>('');
  const [toTime, setToTime] = useState<string>('');
  const [buildingId, setBuildingId] = useState<string>('');
  const [minCapacity, setMinCapacity] = useState<string>('');
  const [facilityIds, setFacilityIds] = useState<number[]>([]);

  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms', buildingId, minCapacity],
    queryFn: () => RoomService.fetchRooms({ buildingId, minCapacity, facilityId: '' }),
  });

  const { data: buildings = [] } = useQuery<Building[], Error>({
    queryKey: ['buildings'],
    queryFn: BuildingService.fetchBuildings,
  });

  const { data: facilities = [] } = useQuery<Facility[], Error>({
    queryKey: ['facilities'],
    queryFn: FacilityService.fetchFacilities,
  });

  const { data: bookings = [], isLoading: bookingsLoading, error: bookingsError } = useQuery<Booking[], Error>({
    queryKey: ['bookings'],
    queryFn: BookingService.fetchBookings,
  });

  const timeIsInvalid = fromTime !== '' && toTime !== '' && toTime <= fromTime;
  const hasTime = fromTime !== '' && toTime !== '' && !timeIsInvalid;
  const bookingsOnDate = bookings.filter(b => b.startTime.substring(0, 10) === date);
  const isFiltered = date !== todayString() || fromTime !== '' || toTime !== '' ||
    buildingId !== '' || minCapacity !== '' || facilityIds.length > 0;

  // The room must have all the chosen equipment.
  const matchingRooms = rooms.filter(room =>
    facilityIds.every(id => room.facilityIds.includes(id)));

  // A room is free if no booking overlaps the chosen time.
  const isFree = (roomId: number) =>
    !bookingsOnDate.some(b =>
      b.roomId === roomId &&
      b.startTime.substring(11, 16) < toTime &&
      b.endTime.substring(11, 16) > fromTime);

  // When a time is chosen, free rooms are shown first.
  const freeRooms = matchingRooms.filter(room => isFree(room.id));
  const bookedRooms = matchingRooms.filter(room => !isFree(room.id));
  const sortedRooms = hasTime ? [...freeRooms, ...bookedRooms] : matchingRooms;

  const displayDate = new Date(`${date}T00:00:00`).toLocaleDateString('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });

  const bookingSummary = (roomId: number) => {
    const count = bookingsOnDate.filter(b => b.roomId === roomId).length;
    if (count === 0) return 'Free all day';
    return count === 1 ? '1 booking' : `${count} bookings`;
  };

  const toggleFacility = (id: number) => {
    setFacilityIds(previous =>
      previous.includes(id) ? previous.filter(f => f !== id) : [...previous, id]);
  };

  // Moves the end time when the start time changes.
  const handleFromTimeChange = (newFrom: string) => {
    setToTime(moveEndTime(fromTime, toTime, newFrom));
    setFromTime(newFrom);
  };

  const resetFilters = () => {
    setDate(todayString());
    setFromTime('');
    setToTime('');
    setBuildingId('');
    setMinCapacity('');
    setFacilityIds([]);
  };

  // The Book button sends room, date and time to the booking form.
  const bookLink = (roomId: number) => {
    const params = new URLSearchParams({ roomId: String(roomId), date });
    if (hasTime) {
      params.append('from', fromTime);
      params.append('to', toTime);
    }
    return `/bookings/create?${params.toString()}`;
  };

  return (
    <>
      <section className="hero">
        <h1>Book a study room.</h1>
        <p>Pick a day and a time to see which rooms are free.</p>
      </section>

      <Form className="search-panel" onSubmit={e => e.preventDefault()}>
        <Row className="g-3">
          <Col xs={12} md={3}>
            <Form.Group controlId="searchDate">
              <Form.Label>Date</Form.Label>
              <DatePicker id="searchDate" value={date} onChange={setDate} minDate={todayString()} />
            </Form.Group>
          </Col>
          <Col xs={6} md={2}>
            <Form.Group controlId="searchFrom">
              <Form.Label>From</Form.Label>
              <Form.Select value={fromTime} onChange={e => handleFromTimeChange(e.target.value)}>
                <option value="">Any time</option>
                {availableTimes(date, fromTime).map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={6} md={2}>
            <Form.Group controlId="searchTo">
              <Form.Label>To</Form.Label>
              <Form.Select value={toTime} onChange={e => setToTime(e.target.value)} isInvalid={timeIsInvalid}>
                <option value="">Any time</option>
                {availableTimes(date, toTime).map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </Form.Select>
              <Form.Control.Feedback type="invalid">Must be after the start time.</Form.Control.Feedback>
            </Form.Group>
          </Col>
          <Col xs={7} md={3}>
            <Form.Group controlId="searchBuilding">
              <Form.Label>Building</Form.Label>
              <Form.Select value={buildingId} onChange={e => setBuildingId(e.target.value)}>
                <option value="">All buildings</option>
                {buildings.map(b => (
                  <option key={b.id} value={b.id}>{b.name}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={5} md={2}>
            <Form.Group controlId="searchCapacity">
              <Form.Label>Group size</Form.Label>
              <Form.Control
                type="number"
                min={1}
                placeholder="Any"
                value={minCapacity}
                onChange={e => setMinCapacity(e.target.value)}
              />
            </Form.Group>
          </Col>
        </Row>

        <div className="search-footer">
          <div className="chip-group" role="group" aria-label="Equipment">
            <span className="chip-group-label">Equipment</span>
            {facilities.map(f => {
              const isActive = facilityIds.includes(f.id);
              return (
                <button
                  key={f.id}
                  type="button"
                  className={`chip${isActive ? ' is-active' : ''}`}
                  aria-pressed={isActive}
                  onClick={() => toggleFacility(f.id)}
                >
                  {f.name}
                </button>
              );
            })}
          </div>
          {isFiltered && (
            <button type="button" className="reset-link" onClick={resetFilters}>
              Reset filters
            </button>
          )}
        </div>
      </Form>

      <div className="results-header">
        <h2>
          {displayDate}{hasTime && `, ${fromTime}–${toTime}`}
        </h2>
        <div className="d-flex gap-3">
          <span className="legend">
            <span className="legend-swatch legend-swatch-free" aria-hidden="true"></span>
            Free
          </span>
          <span className="legend">
            <span className="legend-swatch" aria-hidden="true"></span>
            Booked
          </span>
          {hasTime && (
            <span className="legend">
              <span className="legend-swatch legend-swatch-selected" aria-hidden="true"></span>
              Your time
            </span>
          )}
        </div>
      </div>

      {(isLoading || bookingsLoading) && (
        <Spinner animation="border" role="status" className="my-4">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
      )}

      {error && <Alert variant="danger">Could not load rooms: {error.message}</Alert>}

      {bookingsError && (
        <Alert variant="danger">Could not load bookings, so availability cannot be shown: {bookingsError.message}</Alert>
      )}

      {!isLoading && !error && matchingRooms.length === 0 && (
        <Alert variant="light">No rooms match your search. Try fewer filters.</Alert>
      )}

      {!bookingsLoading && !bookingsError && (
        <ul className="room-rows">
          {sortedRooms.map(room => (
            <li key={room.id} className="room-row">
              <div>
                <Link to={`/rooms/${room.id}`} className="room-number room-link" aria-label={`Room ${room.roomNumber}, ${room.buildingName}`}><span className="room-prefix">{room.buildingShortName}</span>{room.roomNumber}</Link>
                <span className="room-meta">{room.buildingName}</span>
                <RoomTags room={room} />
              </div>
              <AvailabilityBar
                bookings={bookingsOnDate.filter(b => b.roomId === room.id)}
                selectedFrom={hasTime ? fromTime : undefined}
                selectedTo={hasTime ? toTime : undefined}
              />
              <span className="status-cell">
                {hasTime
                  ? (isFree(room.id)
                    ? <span className="status status-free">Available</span>
                    : <span className="status status-busy">Booked</span>)
                  : <span className="status-summary">{bookingSummary(room.id)}</span>}
              </span>
              <Link to={bookLink(room.id)} className="btn btn-sm btn-primary">Book</Link>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
