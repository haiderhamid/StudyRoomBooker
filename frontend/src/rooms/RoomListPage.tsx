import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Form, Row, Col, Button, Card, Badge, Spinner, Alert } from 'react-bootstrap';
import type { Room } from '../types/room';
import type { Building } from '../types/building';
import type { Facility } from '../types/facility';
import * as RoomService from './RoomService';
import * as BuildingService from '../buildings/BuildingService';
import * as FacilityService from '../facilities/FacilityService';

export default function RoomListPage() {
  const [buildingId, setBuildingId] = useState<string>('');
  const [minCapacity, setMinCapacity] = useState<string>('');
  const [facilityId, setFacilityId] = useState<string>('');
  const queryClient = useQueryClient();

  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms', buildingId, minCapacity, facilityId],
    queryFn: () => RoomService.fetchRooms({ buildingId, minCapacity, facilityId }),
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

  const handleDelete = (room: Room) => {
    if (window.confirm(`Delete room ${room.roomNumber}?`)) {
      deleteMutation.mutate(room.id);
    }
  };

  const clearFilters = () => {
    setBuildingId('');
    setMinCapacity('');
    setFacilityId('');
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="mb-0">Rooms</h1>
        <Link to="/rooms/create" className="btn btn-primary">Create room</Link>
      </div>

      <Form className="mb-4">
        <Row className="g-3 align-items-end">
          <Col md={4}>
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
          <Col md={3}>
            <Form.Group controlId="capacityFilter">
              <Form.Label>Minimum capacity</Form.Label>
              <Form.Control
                type="number"
                min={1}
                value={minCapacity}
                onChange={e => setMinCapacity(e.target.value)}
              />
            </Form.Group>
          </Col>
          <Col md={3}>
            <Form.Group controlId="facilityFilter">
              <Form.Label>Facility</Form.Label>
              <Form.Select value={facilityId} onChange={e => setFacilityId(e.target.value)}>
                <option value="">Any facility</option>
                {facilities.map(f => (
                  <option key={f.id} value={f.id}>{f.name}</option>
                ))}
              </Form.Select>
            </Form.Group>
          </Col>
          <Col md={2}>
            <Button variant="outline-secondary" className="w-100" onClick={clearFilters}>
              Clear
            </Button>
          </Col>
        </Row>
      </Form>

      {deleteMutation.error && (
        <Alert variant="danger" dismissible onClose={() => deleteMutation.reset()}>
          {deleteMutation.error.message}
        </Alert>
      )}

      {isLoading && (
        <Spinner animation="border" role="status">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
      )}

      {error && <Alert variant="danger">Could not load rooms: {error.message}</Alert>}

      {!isLoading && !error && rooms.length === 0 && (
        <Alert variant="info">No rooms match your search.</Alert>
      )}

      <Row xs={1} md={2} lg={3} className="g-4">
        {rooms.map(room => (
          <Col key={room.id}>
            <Card className="h-100 shadow-sm">
              <Card.Body>
                <Card.Title>Room {room.roomNumber}</Card.Title>
                <Card.Subtitle className="mb-2 text-muted">{room.buildingName}</Card.Subtitle>
                <Card.Text>
                  Capacity: {room.capacity} {room.capacity === 1 ? 'person' : 'people'}
                </Card.Text>
                {room.facilityNames.map(name => (
                  <Badge key={name} bg="secondary" className="me-1">{name}</Badge>
                ))}
              </Card.Body>
              <Card.Footer className="bg-white border-0">
                <Link to={`/rooms/update/${room.id}`} className="btn btn-sm btn-outline-primary me-2">
                  Edit
                </Link>
                <Button size="sm" variant="outline-danger" onClick={() => handleDelete(room)}>
                  Delete
                </Button>
              </Card.Footer>
            </Card>
          </Col>
        ))}
      </Row>
    </>
  );
}