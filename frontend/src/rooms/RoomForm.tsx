import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Form, Button, Alert, Card } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Room } from '../types/room';
import type { Building } from '../types/building';
import type { Facility } from '../types/facility';
import * as BuildingService from '../buildings/BuildingService';
import * as FacilityService from '../facilities/FacilityService';

interface RoomFormProps {
  initialValues?: Room;
  onSubmit: (room: Room) => void;
  isSubmitting: boolean;
  submitError: string | null;
  submitLabel: string;
}

interface RoomFormErrors {
  roomNumber?: string;
  capacity?: string;
  buildingId?: string;
}

export default function RoomForm({ initialValues, onSubmit, isSubmitting, submitError, submitLabel }: RoomFormProps) {
  const [roomNumber, setRoomNumber] = useState<string>(initialValues?.roomNumber ?? '');
  const [capacity, setCapacity] = useState<string>(initialValues ? String(initialValues.capacity) : '');
  const [buildingId, setBuildingId] = useState<string>(initialValues ? String(initialValues.buildingId) : '');
  const [facilityIds, setFacilityIds] = useState<number[]>(initialValues?.facilityIds ?? []);
  const [errors, setErrors] = useState<RoomFormErrors>({});

  const { data: buildings = [] } = useQuery<Building[], Error>({
    queryKey: ['buildings'],
    queryFn: BuildingService.fetchBuildings,
  });

  const { data: facilities = [] } = useQuery<Facility[], Error>({
    queryKey: ['facilities'],
    queryFn: FacilityService.fetchFacilities,
  });

  const toggleFacility = (facilityId: number) => {
    setFacilityIds(previous =>
      previous.includes(facilityId)
        ? previous.filter(id => id !== facilityId)
        : [...previous, facilityId]
    );
  };

  const validate = (): RoomFormErrors => {
    const newErrors: RoomFormErrors = {};
    if (!roomNumber.trim()) {
      newErrors.roomNumber = 'Please enter a room number.';
    } else if (roomNumber.length > 20) {
      newErrors.roomNumber = 'Room number can be at most 20 characters.';
    }
    const capacityNumber = Number(capacity);
    if (!Number.isInteger(capacityNumber) || capacityNumber < 1 || capacityNumber > 100) {
      newErrors.capacity = 'Capacity must be between 1 and 100.';
    }
    if (!buildingId) {
      newErrors.buildingId = 'Please select a building.';
    }
    return newErrors;
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const newErrors = validate();
    setErrors(newErrors);
    if (Object.keys(newErrors).length > 0) {
      return;
    }
    onSubmit({
      id: initialValues?.id ?? 0,
      roomNumber: roomNumber.trim(),
      capacity: Number(capacity),
      buildingId: Number(buildingId),
      facilityIds,
      facilityNames: [],
    });
  };

  return (
    <Card>
      <Card.Body>
        {submitError && <Alert variant="danger">{submitError}</Alert>}

        <Form noValidate onSubmit={handleSubmit}>
          <Form.Group className="mb-3" controlId="roomNumber">
            <Form.Label>Room number</Form.Label>
            <Form.Control
              value={roomNumber}
              onChange={e => setRoomNumber(e.target.value)}
              isInvalid={!!errors.roomNumber}
            />
            <Form.Control.Feedback type="invalid">{errors.roomNumber}</Form.Control.Feedback>
          </Form.Group>

          <Form.Group className="mb-3" controlId="buildingId">
            <Form.Label>Building</Form.Label>
            <Form.Select
              value={buildingId}
              onChange={e => setBuildingId(e.target.value)}
              isInvalid={!!errors.buildingId}
            >
              <option value="">Select a building</option>
              {buildings.map(b => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </Form.Select>
            <Form.Control.Feedback type="invalid">{errors.buildingId}</Form.Control.Feedback>
          </Form.Group>

          <Form.Group className="mb-3" controlId="capacity">
            <Form.Label>Seats</Form.Label>
            <Form.Control
              type="number"
              min={1}
              max={100}
              value={capacity}
              onChange={e => setCapacity(e.target.value)}
              isInvalid={!!errors.capacity}
            />
            <Form.Control.Feedback type="invalid">{errors.capacity}</Form.Control.Feedback>
          </Form.Group>

          <fieldset>
            <legend className="form-label">Equipment</legend>
            <div className="chip-group">
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
          </fieldset>

          <div className="form-actions">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : submitLabel}
            </Button>
            <Link to="/rooms" className="btn btn-quiet">Cancel</Link>
          </div>
        </Form>
      </Card.Body>
    </Card>
  );
}