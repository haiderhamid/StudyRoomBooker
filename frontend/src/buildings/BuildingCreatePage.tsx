import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import BuildingForm from './BuildingForm';
import type { Building } from '../types/building';
import * as BuildingService from './BuildingService';

export default function BuildingCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const mutation = useMutation<Building, Error, Building>({
    mutationFn: BuildingService.createBuilding,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      navigate('/buildings');
    },
  });

  return (
    <>
      <h1 className="mb-4">Create Building</h1>
      <BuildingForm
        onSubmit={building => mutation.mutate(building)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Create"
      />
    </>
  );
}