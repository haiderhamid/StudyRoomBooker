import type { Room } from '../types/room';

interface RoomTagsProps {
  room: Room;
}

export default function RoomTags({ room }: RoomTagsProps) {
  return (
    <div className="room-tags">
      <span className="room-tag room-tag-capacity">
        <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
          <circle cx="8" cy="5" r="3" fill="currentColor" />
          <path d="M2 14c0-3.3 2.7-5 6-5s6 1.7 6 5" fill="currentColor" />
        </svg>
        {room.capacity} {room.capacity === 1 ? 'seat' : 'seats'}
      </span>
      {room.facilityNames.map(name => (
        <span key={name} className="room-tag">{name}</span>
      ))}
    </div>
  );
}
