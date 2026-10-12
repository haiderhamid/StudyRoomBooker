import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import RoomForm from './RoomForm';
import type { Room } from '../types/room';
import * as RoomService from './RoomService';

export default function RoomCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const mutation = useMutation<Room, Error, Room>({
    mutationFn: RoomService.createRoom,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      navigate('/rooms');
    },
  });

  return (
    <div className="form-shell">
      <header className="page-header">
        <h1 className="page-title">Add a room</h1>
        <p className="page-subtitle">Add a new study room that students can book.</p>
      </header>
      <RoomForm
        onSubmit={room => mutation.mutate(room)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Add room"
      />
    </div>
  );
}