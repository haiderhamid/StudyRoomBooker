export interface Booking {
  id: number;
  roomId: number;
  roomNumber?: string | null;
  buildingName?: string | null;
  subject: string;
  startTime: string;
  endTime: string;
  bookedBy: string;
  seatsTaken?: number;
  roomCapacity?: number;
  isJoined?: boolean;
  participants?: string[];
}