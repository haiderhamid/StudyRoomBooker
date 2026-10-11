import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Table, Button, Spinner, Alert } from 'react-bootstrap';
import type { Building } from '../types/building';
import * as BuildingService from './BuildingService';

export default function BuildingListPage() {
  const queryClient = useQueryClient();

  const { data: buildings = [], isLoading, error } = useQuery<Building[], Error>({
    queryKey: ['buildings'],
    queryFn: BuildingService.fetchBuildings,
  });

  const deleteMutation = useMutation<void, Error, number>({
    mutationFn: BuildingService.deleteBuilding,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
    },
  });

  const handleDelete = (building: Building) => {
    if (window.confirm(`Delete building ${building.name}?`)) {
      deleteMutation.mutate(building.id);
    }
  };

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="mb-0">Buildings</h1>
        <Link to="/buildings/create" className="btn btn-primary">Create building</Link>
      </div>

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

      {error && <Alert variant="danger">Could not load buildings: {error.message}</Alert>}

      {!isLoading && !error && buildings.length === 0 && (
        <Alert variant="info">No buildings yet.</Alert>
      )}

      {buildings.length > 0 && (
        <Table striped hover responsive className="shadow-sm">
          <thead>
            <tr>
              <th>Name</th>
              <th>Address</th>
              <th>Rooms</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {buildings.map(building => (
              <tr key={building.id}>
                <td>{building.name}</td>
                <td>{building.address}</td>
                <td>{building.roomCount === 1 ? '1 room' : `${building.roomCount} rooms`}</td>
                <td className="text-end">
                  <Link to={`/buildings/update/${building.id}`} className="btn btn-sm btn-outline-primary me-2">
                    Edit
                  </Link>
                  <Button size="sm" variant="outline-danger" onClick={() => handleDelete(building)}>
                    Delete
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      )}
    </>
  );
}