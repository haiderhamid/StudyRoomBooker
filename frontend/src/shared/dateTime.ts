export const timeOptions: string[] = [];
for (let minutes = 7 * 60; minutes <= 20 * 60; minutes += 15) {
  const hours = String(Math.floor(minutes / 60)).padStart(2, '0');
  const mins = String(minutes % 60).padStart(2, '0');
  timeOptions.push(`${hours}:${mins}`);
}

export function toDateString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function todayString(): string {
  return toDateString(new Date());
}

// Monday is the first day of the week
export function startOfWeek(date: Date): Date {
  const monday = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const daysSinceMonday = (monday.getDay() + 6) % 7;
  monday.setDate(monday.getDate() - daysSinceMonday);
  return monday;
}

// Same rule as the MVC app: after 19:45 nothing can be booked today, so the first day is tomorrow.
export function firstBookableDate(): string {
  const now = new Date();
  const lastStart = 19 * 60 + 45;
  if (now.getHours() * 60 + now.getMinutes() >= lastStart) {
    return toDateString(addDays(now, 1));
  }
  return todayString();
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

// Week number like in Norwegian calendars (ISO 8601)
export function isoWeekNumber(date: Date): number {
  const target = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNumber = target.getUTCDay() || 7;
  target.setUTCDate(target.getUTCDate() + 4 - dayNumber);
  const yearStart = new Date(Date.UTC(target.getUTCFullYear(), 0, 1));
  return Math.ceil(((target.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
}

function timeToMinutes(time: string): number {
  return Number(time.substring(0, 2)) * 60 + Number(time.substring(3, 5));
}

function minutesToTime(minutes: number): string {
  const hours = String(Math.floor(minutes / 60)).padStart(2, '0');
  const mins = String(minutes % 60).padStart(2, '0');
  return `${hours}:${mins}`;
}

// Same length as before, but never later than 20:00
export function moveEndTime(oldFrom: string, oldTo: string, newFrom: string): string {
  if (!newFrom) {
    return oldTo;
  }
  let duration = 60;
  if (oldFrom && oldTo && oldTo > oldFrom) {
    duration = timeToMinutes(oldTo) - timeToMinutes(oldFrom);
  }
  const end = Math.min(timeToMinutes(newFrom) + duration, 20 * 60);
  return minutesToTime(end);
}

// Today: only times that have not passed
export function availableTimes(date: string, selectedTime: string): string[] {
  if (date !== todayString()) {
    return timeOptions;
  }
  const now = new Date();
  const currentTime = minutesToTime(now.getHours() * 60 + now.getMinutes());
  return timeOptions.filter(t => t > currentTime || t === selectedTime);
}
