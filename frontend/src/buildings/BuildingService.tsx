import type { Building } from '../types/building';
import { API_URL, getAuthHeaders, handleResponse, handleEmptyResponse } from '../shared/apiHelpers';

export async function fetchBuildings(): Promise<Building[]> {
  const response = await fetch(`${API_URL}/api/buildingapi/buildinglist`);
  return handleResponse<Building[]>(response);
}

export async function fetchBuildingById(buildingId: string): Promise<Building> {
  const response = await fetch(`${API_URL}/api/buildingapi/${buildingId}`);
  return handleResponse<Building>(response);
}

export async function createBuilding(building: Building): Promise<Building> {
  const response = await fetch(`${API_URL}/api/buildingapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(building),
  });
  return handleResponse<Building>(response);
}

export async function updateBuilding(buildingId: number, building: Building): Promise<Building> {
  const response = await fetch(`${API_URL}/api/buildingapi/update/${buildingId}`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(building),
  });
  return handleResponse<Building>(response);
}

export async function deleteBuilding(buildingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/buildingapi/delete/${buildingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  return handleEmptyResponse(response);
}