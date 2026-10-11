import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Spinner, Alert } from 'react-bootstrap';
import RoomForm from './RoomForm';
import type { Room } from '../types/room';
import * as RoomService from './RoomService';

export default function RoomUpdatePage() {
  const { roomId } = useParams<{ roomId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: room, isLoading, error } = useQuery<Room, Error>({
    queryKey: ['room', roomId],
    queryFn: () => RoomService.fetchRoomById(roomId!),
    enabled: !!roomId,
  });

  const mutation = useMutation<Room, Error, Room>({
    mutationFn: (updatedRoom: Room) => RoomService.updateRoom(updatedRoom.id, updatedRoom),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      queryClient.invalidateQueries({ queryKey: ['room', roomId] });
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      navigate('/rooms');
    },
  });

  if (isLoading) {
    return (
      <Spinner animation="border" role="status">
        <span className="visually-hidden">Loading...</span>
      </Spinner>
    );
  }

  if (error) {
    return <Alert variant="danger">Could not load room: {error.message}</Alert>;
  }

  if (!room) {
    return <Alert variant="warning">Room not found.</Alert>;
  }

  return (
    <>
      <h1 className="mb-4">Edit Room</h1>
      <RoomForm
        initialValues={room}
        onSubmit={updatedRoom => mutation.mutate(updatedRoom)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Save changes"
      />
    </>
  );
}