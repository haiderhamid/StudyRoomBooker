export interface Room {
  id: number;
  roomNumber: string;
  capacity: number;
  buildingId: number;
  buildingName?: string | null;
  buildingShortName?: string | null;
  facilityIds: number[];
  facilityNames: string[];
}