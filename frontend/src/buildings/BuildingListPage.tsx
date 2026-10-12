import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Spinner, Alert } from 'react-bootstrap';
import type { Building } from '../types/building';
import * as BuildingService from './BuildingService';
import { useAuth } from '../auth/AuthContext';

export default function BuildingListPage() {
  const queryClient = useQueryClient();
  const { isAdmin } = useAuth();

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
      <header className="page-header">
        <div>
          <h1 className="page-title">Buildings</h1>
          <p className="page-subtitle">The campus buildings that have study rooms.</p>
        </div>
        {isAdmin && (
          <Link to="/buildings/create" className="btn btn-primary">Add building</Link>
        )}
      </header>

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

      {error && <Alert variant="danger">Could not load buildings: {error.message}</Alert>}

      {!isLoading && !error && buildings.length === 0 && (
        <p className="empty-state">No buildings yet.</p>
      )}

      <ul className="item-rows">
        {buildings.map(building => (
          <li key={building.id} className="item-row">
            <div className="item-main">
              <span className="item-title">{building.name}</span>
              <span className="item-meta">{building.address}</span>
            </div>
            <span className="room-tag">
              {building.roomCount === 1 ? '1 room' : `${building.roomCount} rooms`}
            </span>
            {isAdmin && (
              <div className="item-actions">
                <Link to={`/buildings/update/${building.id}`} className="btn btn-sm btn-quiet">Edit</Link>
                <button type="button" className="btn btn-sm btn-quiet btn-quiet-danger" onClick={() => handleDelete(building)}>
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
