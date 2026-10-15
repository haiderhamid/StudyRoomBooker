import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Form, Row, Col, Spinner, Alert } from 'react-bootstrap';
import type { Room } from '../types/room';
import type { Building } from '../types/building';
import type { Facility } from '../types/facility';
import * as RoomService from './RoomService';
import * as BuildingService from '../buildings/BuildingService';
import * as FacilityService from '../facilities/FacilityService';
import { useAuth } from '../auth/useAuth';
import RoomTags from '../shared/RoomTags';

export default function RoomListPage() {
  const [buildingId, setBuildingId] = useState<string>('');
  const [minCapacity, setMinCapacity] = useState<string>('');
  const [facilityIds, setFacilityIds] = useState<number[]>([]);
  const queryClient = useQueryClient();
  const { isAdmin } = useAuth();

  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms', buildingId, minCapacity],
    queryFn: () => RoomService.fetchRooms({ buildingId, minCapacity, facilityId: '' }),
  });

  const { data: buildings = [] } = useQuery<Building[], Error>({
    queryKey: ['buildings'],
    queryFn: BuildingService.fetchBuildings,
  });

  const { data: facilities = [] } = useQuery<Facility[], Error>({
    queryKey: ['facilities'],
    queryFn: FacilityService.fetchFacilities,
  });

  const deleteMutation = useMutation<void, Error, number>({
    mutationFn: RoomService.deleteRoom,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
    },
  });

  const matchingRooms = rooms.filter(room =>
    facilityIds.every(id => room.facilityIds.includes(id)));

  const isFiltered = buildingId !== '' || minCapacity !== '' || facilityIds.length > 0;

  const handleDelete = (room: Room) => {
    if (window.confirm(`Delete room ${room.roomNumber}?`)) {
      deleteMutation.mutate(room.id);
    }
  };

  const toggleFacility = (id: number) => {
    setFacilityIds(previous =>
      previous.includes(id) ? previous.filter(f => f !== id) : [...previous, id]);
  };

  const resetFilters = () => {
    setBuildingId('');
    setMinCapacity('');
    setFacilityIds([]);
  };

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Rooms</h1>
          <p className="page-subtitle">Every study room on campus, with seats and equipment.</p>
        </div>
        {isAdmin && (
          <Link to="/rooms/create" className="btn btn-primary">Add room</Link>
        )}
      </header>

      <Form className="search-panel" onSubmit={e => e.preventDefault()}>
        <Row className="g-3">
          <Col xs={7} md={4}>
            <Form.Group controlId="buildingFilter">
              <Form.Label>Building</Form.Label>
              <Form.Select value={buildingId} onChange={e => setBuildingId(e.target.value)}>
                <option value="">All buildings</option>
                {buildings.map(b => (
                  <option key={b.id} value={b.id}>{b.name}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col xs={5} md={2}>
            <Form.Group controlId="capacityFilter">
              <Form.Label>Group size</Form.Label>
              <Form.Control
                type="number"
                min={1}
                placeholder="Any"
                value={minCapacity}
                onChange={e => setMinCapacity(e.target.value)}
              />
            </Form.Group>
          </Col>
        </Row>

        <div className="search-footer">
          <div className="chip-group" role="group" aria-label="Equipment">
            <span className="chip-group-label">Equipment</span>
            {facilities.map(f => {
              const isActive = facilityIds.includes(f.id);
              return (
                <button
                  key={f.id}
                  type="button"
                  className={`chip${isActive ? ' is-active' : ''}`}
                  aria-pressed={isActive}
                  onClick={() => toggleFacility(f.id)}
                >
                  {f.name}
                </button>
              );
            })}
          </div>
          {isFiltered && (
            <button type="button" className="reset-link" onClick={resetFilters}>
              Reset filters
            </button>
          )}
        </div>
      </Form>

      {deleteMutation.error && (
        <Alert variant="danger" dismissible onClose={() => deleteMutation.reset()}>
          {deleteMutation.error.message}
        </Alert>
      )}

      {isLoading && (
        <Spinner animation="border" role="status" className="my-4">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
      )}

      {error && <Alert variant="danger">Could not load rooms: {error.message}</Alert>}

      {!isLoading && !error && (
        <p className="list-count">
          {matchingRooms.length === 1 ? '1 room' : `${matchingRooms.length} rooms`}
        </p>
      )}

      {!isLoading && !error && matchingRooms.length === 0 && (
        <p className="empty-state">No rooms match your search. Try fewer filters.</p>
      )}

      <ul className="room-grid">
        {matchingRooms.map(room => (
          <li key={room.id} className="room-card">
            <Link to={`/rooms/${room.id}`} className="room-number stretched-link" aria-label={`Room ${room.roomNumber}`}>{room.roomNumber}</Link>
            <span className="item-meta">{room.buildingName}</span>
            <RoomTags room={room} />
            {isAdmin && (
              <div className="item-actions">
                <Link to={`/rooms/update/${room.id}`} className="btn btn-sm btn-quiet">Edit</Link>
                <button type="button" className="btn btn-sm btn-quiet btn-quiet-danger" onClick={() => handleDelete(room)}>
                  Delete
                </button>
              </div>
            )}
          </li>
        ))}
      </ul>
    </>
  );
}
