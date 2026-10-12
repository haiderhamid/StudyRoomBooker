import type { Booking } from '../types/booking';
import { API_URL, getAuthHeaders, handleResponse, handleEmptyResponse } from '../shared/apiHelpers';

export async function fetchBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/bookinglist`);
  return handleResponse<Booking[]>(response);
}

export async function fetchBookingById(bookingId: string): Promise<Booking> {
  const response = await fetch(`${API_URL}/api/bookingapi/${bookingId}`);
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