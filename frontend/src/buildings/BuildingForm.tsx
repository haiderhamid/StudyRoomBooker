import { useState } from 'react';
import type { FormEvent } from 'react';
import { Form, Button, Alert, Card } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Building } from '../types/building';

interface BuildingFormProps {
  initialValues?: Building;
  onSubmit: (building: Building) => void;
  isSubmitting: boolean;
  submitError: string | null;
  submitLabel: string;
}

interface BuildingFormErrors {
  name?: string;
  address?: string;
}

export default function BuildingForm({ initialValues, onSubmit, isSubmitting, submitError, submitLabel }: BuildingFormProps) {
  const [name, setName] = useState<string>(initialValues?.name ?? '');
  const [address, setAddress] = useState<string>(initialValues?.address ?? '');
  const [errors, setErrors] = useState<BuildingFormErrors>({});

  const validate = (): BuildingFormErrors => {
    const newErrors: BuildingFormErrors = {};
    if (!name.trim()) {
      newErrors.name = 'Please enter a name.';
    } else if (name.length > 100) {
      newErrors.name = 'Name can be at most 100 characters.';
    }
    if (!address.trim()) {
      newErrors.address = 'Please enter an address.';
    } else if (address.length > 200) {
      newErrors.address = 'Address can be at most 200 characters.';
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
      name: name.trim(),
      address: address.trim(),
      roomCount: initialValues?.roomCount ?? 0,
    });
  };

  return (
    <Card>
      <Card.Body>
        {submitError && <Alert variant="danger">{submitError}</Alert>}

        <Form noValidate onSubmit={handleSubmit}>
          <Form.Group className="mb-3" controlId="name">
            <Form.Label>Name</Form.Label>
            <Form.Control
              value={name}
              onChange={e => setName(e.target.value)}
              isInvalid={!!errors.name}
            />
            <Form.Control.Feedback type="invalid">{errors.name}</Form.Control.Feedback>
          </Form.Group>

          <Form.Group controlId="address">
            <Form.Label>Address</Form.Label>
            <Form.Control
              value={address}
              onChange={e => setAddress(e.target.value)}
              isInvalid={!!errors.address}
            />
            <Form.Control.Feedback type="invalid">{errors.address}</Form.Control.Feedback>
          </Form.Group>

          <div className="form-actions">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : submitLabel}
            </Button>
            <Link to="/buildings" className="btn btn-quiet">Cancel</Link>
          </div>
        </Form>
      </Card.Body>
    </Card>
  );
}