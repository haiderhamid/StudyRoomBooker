export const timeOptions: string[] = [];
for (let minutes = 7 * 60; minutes <= 22 * 60; minutes += 15) {
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