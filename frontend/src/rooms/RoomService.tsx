import type { Room } from '../types/room';
import { API_URL, getAuthHeaders, handleResponse, handleEmptyResponse } from '../shared/apiHelpers';

export interface RoomFilters {
  buildingId: string;
  minCapacity: string;
  facilityId: string;
}

export async function fetchRooms(filters: RoomFilters): Promise<Room[]> {
  const params = new URLSearchParams();
  if (filters.buildingId) params.append('buildingId', filters.buildingId);
  if (filters.minCapacity) params.append('minCapacity', filters.minCapacity);
  if (filters.facilityId) params.append('facilityId', filters.facilityId);

  const response = await fetch(`${API_URL}/api/roomapi/roomlist?${params.toString()}`);
  return handleResponse<Room[]>(response);
}

export async function fetchRoomById(roomId: string): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/${roomId}`);
  return handleResponse<Room>(response);
}

export async function createRoom(room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function updateRoom(roomId: number, room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/update/${roomId}`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function deleteRoom(roomId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/roomapi/delete/${roomId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  return handleEmptyResponse(response);
}