import type { Booking } from '../types/booking';
import { API_URL, getAuthHeaders, handleResponse, handleEmptyResponse } from '../shared/apiHelpers';

// Sends the token too, so the API can show who booked on your own bookings.
export async function fetchBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/bookinglist`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking[]>(response);
}

// Only the bookings of the logged-in user.
export async function fetchMyBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/mybookings`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking[]>(response);
}

export async function fetchBookingById(bookingId: string): Promise<Booking> {
  const response = await fetch(`${API_URL}/api/bookingapi/${bookingId}`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking>(response);
}

export async function createBooking(booking: Booking): Promise<Booking> {
  const response = await fetch(`${API_URL}/api/bookingapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(booking),
  });
  return handleResponse<Booking>(response);
}

export async function updateBooking(bookingId: number, booking: Booking): Promise<Booking> {
  const response = await fetch(`${API_URL}/api/bookingapi/update/${bookingId}`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(booking),
  });
  return handleResponse<Booking>(response);
}

export async function deleteBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingapi/delete/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  return handleEmptyResponse(response);
}
// Join someone else's booking as the logged-in user.
export async function joinBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingapi/join/${bookingId}`, {
    method: 'POST',
    headers: getAuthHeaders(),
  });
  return handleEmptyResponse(response);
}

export async function leaveBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingapi/leave/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  return handleEmptyResponse(response);
}
