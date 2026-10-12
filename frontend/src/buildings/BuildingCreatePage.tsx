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
    <div className="form-shell">
      <header className="page-header">
        <h1 className="page-title">Add a building</h1>
        <p className="page-subtitle">Add a campus building that has study rooms.</p>
      </header>
      <BuildingForm
        onSubmit={building => mutation.mutate(building)}
        isSubmitting={mutation.isPending}
        submitError={mutation.error?.message ?? null}
        submitLabel="Add building"
      />
    </div>
  );
}