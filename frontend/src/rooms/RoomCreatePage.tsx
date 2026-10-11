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
    <>
      <h1 className="mb-4">Create Room</h1>
      <RoomForm
        onSubmit={room => mutation.mutate(room)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Create"
      />
    </>
  );
}