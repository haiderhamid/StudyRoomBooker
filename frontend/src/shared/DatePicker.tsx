import { useState } from 'react';
import { Overlay, Popover } from 'react-bootstrap';
import { toDateString } from './dateTime';

interface DatePickerProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  minDate?: string;
  isInvalid?: boolean;
}

const WEEKDAYS = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function buildMonth(year: number, month: number): Date[] {
  const firstDay = new Date(year, month, 1);
  const offset = (firstDay.getDay() + 6) % 7;
  return Array.from({ length: 42 }, (_, index) => new Date(year, month, 1 - offset + index));
}

export default function DatePicker({ id, value, onChange, minDate, isInvalid }: DatePickerProps) {
  const [anchor, setAnchor] = useState<HTMLButtonElement | null>(null);
  const [show, setShow] = useState<boolean>(false);
  const [viewYear, setViewYear] = useState<number>(new Date().getFullYear());
  const [viewMonth, setViewMonth] = useState<number>(new Date().getMonth());
  const today = toDateString(new Date());

  const open = () => {
    const shown = value ? parseDate(value) : new Date();
    setViewYear(shown.getFullYear());
    setViewMonth(shown.getMonth());
    setShow(true);
  };

  const changeMonth = (step: number) => {
    const next = new Date(viewYear, viewMonth + step, 1);
    setViewYear(next.getFullYear());
    setViewMonth(next.getMonth());
  };

  const pick = (day: Date) => {
    onChange(toDateString(day));
    setShow(false);
    anchor?.focus();
  };

  const label = value
    ? parseDate(value).toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric' })
    : 'Choose a date';

  const monthLabel = new Date(viewYear, viewMonth, 1).toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });

  return (
    <>
      <button
        id={id}
        ref={setAnchor}
        type="button"
        className={`form-select date-trigger${isInvalid ? ' is-invalid' : ''}`}
        aria-haspopup="dialog"
        aria-expanded={show}
        onClick={open}
      >
        {label}
      </button>
      <Overlay target={anchor} show={show} placement="bottom-start" rootClose onHide={() => setShow(false)}>
        <Popover className="calendar-popover">
          <Popover.Body>
            <div className="calendar-header">
              <button type="button" className="calendar-nav" aria-label="Previous month" onClick={() => changeMonth(-1)}>
                ‹
              </button>
              <span className="calendar-month">{monthLabel}</span>
              <button type="button" className="calendar-nav" aria-label="Next month" onClick={() => changeMonth(1)}>
                ›
              </button>
            </div>
            <div className="calendar-grid">
              {WEEKDAYS.map(weekday => (
                <span key={weekday} className="calendar-weekday" aria-hidden="true">{weekday}</span>
              ))}
              {buildMonth(viewYear, viewMonth).map(day => {
                const dayValue = toDateString(day);
                const classes = [
                  'calendar-day',
                  day.getMonth() !== viewMonth ? 'is-outside' : '',
                  dayValue === today ? 'is-today' : '',
                  dayValue === value ? 'is-selected' : '',
                ].filter(Boolean).join(' ');
                return (
                  <button
                    key={dayValue}
                    type="button"
                    className={classes}
                    disabled={!!minDate && dayValue < minDate}
                    aria-pressed={dayValue === value}
                    aria-label={day.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
                    onClick={() => pick(day)}
                  >
                    {day.getDate()}
                  </button>
                );
              })}
            </div>
            <div className="calendar-footer">
              <button type="button" className="btn btn-link btn-sm" onClick={() => pick(new Date())}>
                Today
              </button>
            </div>
          </Popover.Body>
        </Popover>
      </Overlay>
    </>
  );
}
