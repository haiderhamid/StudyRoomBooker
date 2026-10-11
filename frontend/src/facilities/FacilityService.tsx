import type { Facility } from '../types/facility';
import { API_URL, handleResponse } from '../shared/apiHelpers';

export async function fetchFacilities(): Promise<Facility[]> {
  const response = await fetch(`${API_URL}/api/facilityapi/facilitylist`);
  return handleResponse<Facility[]>(response);
}