import type { Booking } from '../types/booking';

const DAY_START = 7 * 60;
const DAY_END = 20 * 60;
const DAY_LENGTH = DAY_END - DAY_START;
const HOUR_MARKS = [7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];

interface AvailabilityBarProps {
  bookings: Booking[];
  selectedFrom?: string;
  selectedTo?: string;
}

function timeToMinutes(time: string): number {
  return Number(time.substring(0, 2)) * 60 + Number(time.substring(3, 5));
}

// Where a time is on the 07-20 bar, in percent.
function toPercent(minutes: number): number {
  return ((minutes - DAY_START) / DAY_LENGTH) * 100;
}

export default function AvailabilityBar({ bookings, selectedFrom, selectedTo }: AvailabilityBarProps) {
  const description = bookings.length === 0
    ? 'Free all day.'
    : `Booked ${bookings.map(b => `${b.startTime.substring(11, 16)} to ${b.endTime.substring(11, 16)}`).join(', ')}.`;

  return (
    <div className="availability">
      <div className="availability-track">
        {bookings.map(booking => {
          const start = Math.max(timeToMinutes(booking.startTime.substring(11, 16)), DAY_START);
          const end = Math.min(timeToMinutes(booking.endTime.substring(11, 16)), DAY_END);
          return (
            <span
              key={booking.id}
              className="availability-booked"
              style={{ left: `${toPercent(start)}%`, width: `${toPercent(end) - toPercent(start)}%` }}
              title={`Booked ${booking.startTime.substring(11, 16)}–${booking.endTime.substring(11, 16)}`}
            />
          );
        })}
        {selectedFrom && selectedTo && (
          <span
            className="availability-selected"
            style={{
              left: `${toPercent(timeToMinutes(selectedFrom))}%`,
              width: `${toPercent(timeToMinutes(selectedTo)) - toPercent(timeToMinutes(selectedFrom))}%`,
            }}
          />
        )}
      </div>
      <div className="availability-scale" aria-hidden="true">
        {HOUR_MARKS.map(hour => (
          <span key={hour} style={{ left: `${toPercent(hour * 60)}%` }}>
            {String(hour).padStart(2, '0')}
          </span>
        ))}
      </div>
      <p className="visually-hidden">{description}</p>
    </div>
  );
}