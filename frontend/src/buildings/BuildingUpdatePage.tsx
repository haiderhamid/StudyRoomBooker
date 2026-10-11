import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Spinner, Alert } from 'react-bootstrap';
import BuildingForm from './BuildingForm';
import type { Building } from '../types/building';
import * as BuildingService from './BuildingService';

export default function BuildingUpdatePage() {
  const { buildingId } = useParams<{ buildingId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: building, isLoading, error } = useQuery<Building, Error>({
    queryKey: ['building', buildingId],
    queryFn: () => BuildingService.fetchBuildingById(buildingId!),
    enabled: !!buildingId,
  });

  const mutation = useMutation<Building, Error, Building>({
    mutationFn: (updatedBuilding: Building) => BuildingService.updateBuilding(updatedBuilding.id, updatedBuilding),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      queryClient.invalidateQueries({ queryKey: ['building', buildingId] });
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      navigate('/buildings');
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
    return <Alert variant="danger">Could not load building: {error.message}</Alert>;
  }

  if (!building) {
    return <Alert variant="warning">Building not found.</Alert>;
  }

  return (
    <>
      <h1 className="mb-4">Edit Building</h1>
      <BuildingForm
        initialValues={building}
        onSubmit={updatedBuilding => mutation.mutate(updatedBuilding)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Save changes"
      />
    </>
  );
}