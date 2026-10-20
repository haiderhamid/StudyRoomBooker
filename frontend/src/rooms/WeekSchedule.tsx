import { Link } from 'react-router-dom';
import type { Booking } from '../types/booking';
import { addDays, isoWeekNumber, toDateString, todayString } from '../shared/dateTime';

const DAY_START = 7;
const DAY_END = 20;
const HOURS = [7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];

export interface ScheduleSelection {
  date: string;
  from: string;
  to: string;
}

interface WeekScheduleProps {
  bookings: Booking[];
  weekStart: Date;
  onPreviousWeek: () => void;
  onNextWeek: () => void;
  onThisWeek: () => void;
  isMine: (booking: Booking) => boolean;
  selection?: ScheduleSelection;
}

// Where a time is in the day column, in percent.
function toPercent(time: string): number {
  const minutes = Number(time.substring(0, 2)) * 60 + Number(time.substring(3, 5));
  const clamped = Math.min(Math.max(minutes, DAY_START * 60), DAY_END * 60);
  return ((clamped - DAY_START * 60) / ((DAY_END - DAY_START) * 60)) * 100;
}

export default function WeekSchedule({
  bookings,
  weekStart,
  onPreviousWeek,
  onNextWeek,
  onThisWeek,
  isMine,
  selection,
}: WeekScheduleProps) {
  const days: Date[] = [];
  for (let i = 0; i < 7; i++) {
    days.push(addDays(weekStart, i));
  }
  const today = todayString();
  // True if the chosen time overlaps a booking. Shows a red frame.
  const selectionIsBooked = !!selection && bookings.some(b =>
    b.startTime.substring(0, 10) === selection.date &&
    b.startTime.substring(11, 16) < selection.to &&
    b.endTime.substring(11, 16) > selection.from);

  return (
    <section className="schedule" aria-label="Weekly schedule">
      <div className="schedule-toolbar">
        <div className="schedule-nav">
          <button type="button" className="schedule-arrow" aria-label="Previous week" onClick={onPreviousWeek}>
            ‹
          </button>
          <h2 className="schedule-title">Week {isoWeekNumber(weekStart)}</h2>
          <button type="button" className="schedule-arrow" aria-label="Next week" onClick={onNextWeek}>
            ›
          </button>
        </div>
        <div className="d-flex flex-wrap align-items-center gap-3">
          <span className="legend">
            <span className="legend-swatch" aria-hidden="true"></span>
            Booked
          </span>
          <span className="legend">
            <span className="legend-swatch legend-swatch-mine" aria-hidden="true"></span>
            Yours
          </span>
          {selection && (
            <span className="legend">
              <span className="legend-swatch legend-swatch-selected" aria-hidden="true"></span>
              Your time
            </span>
          )}
          <button type="button" className="btn btn-sm btn-quiet" onClick={onThisWeek}>This week</button>
        </div>
      </div>

      {selectionIsBooked && (
        <p className="schedule-warning" role="status">The room is already booked in the time you have chosen.</p>
      )}

      <div className="schedule-scroll">
        <div className="schedule-grid">
          <div className="schedule-corner" aria-hidden="true"></div>
          {days.map(day => {
            const isToday = toDateString(day) === today;
            return (
              <div key={toDateString(day)} className={`schedule-day-head${isToday ? ' is-today' : ''}`}>
                <span>{day.toLocaleDateString('en-GB', { weekday: 'short' })}</span>
                <strong>{day.toLocaleDateString('en-GB', { day: 'numeric', month: 'numeric' })}</strong>
              </div>
            );
          })}

          <div className="schedule-times" aria-hidden="true">
            {HOURS.map(hour => (
              <span key={hour} style={{ top: `${((hour - DAY_START) / (DAY_END - DAY_START)) * 100}%` }}>
                {String(hour).padStart(2, '0')}:00
              </span>
            ))}
          </div>

          {days.map(day => {
            const date = toDateString(day);
            const dayBookings = bookings.filter(b => b.startTime.substring(0, 10) === date);
            return (
              <div key={date} className={`schedule-day${date === today ? ' is-today' : ''}`}>
                {dayBookings.map(booking => {
                  const from = booking.startTime.substring(11, 16);
                  const to = booking.endTime.substring(11, 16);
                  const top = toPercent(from);
                  const style = { top: `${top}%`, height: `${toPercent(to) - top}%` };
                  const className = `schedule-booking${isMine(booking) ? ' is-mine' : ''}`;
                  // Every booking links to its own page, where you can join it.
                  return (
                    <Link
                      key={booking.id}
                      to={`/bookings/${booking.id}`}
                      className={className}
                      style={style}
                      aria-label={`${booking.subject}, ${from} to ${to}, ${booking.seatsTaken} of ${booking.roomCapacity} seats taken`}
                    >
                      <strong>{booking.subject}</strong>
                      <span>{from}–{to}</span>
                    </Link>
                  );
                })}
                {selection && selection.date === date && (
                  <div
                    className={`schedule-selected${selectionIsBooked ? ' is-conflict' : ''}`}
                    style={{
                      top: `${toPercent(selection.from)}%`,
                      height: `${toPercent(selection.to) - toPercent(selection.from)}%`,
                    }}
                    aria-hidden="true"
                  >
                    <span>Your time</span>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
