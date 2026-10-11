import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Container } from 'react-bootstrap';
import NavMenu from './shared/NavMenu';
import HomePage from './home/HomePage';
import RoomListPage from './rooms/RoomListPage';
import RoomCreatePage from './rooms/RoomCreatePage';
import RoomUpdatePage from './rooms/RoomUpdatePage';
import BuildingListPage from './buildings/BuildingListPage';
import BuildingCreatePage from './buildings/BuildingCreatePage';
import BuildingUpdatePage from './buildings/BuildingUpdatePage';

const queryClient = new QueryClient();

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <NavMenu />
        <Container className="mt-4">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/rooms" element={<RoomListPage />} />
            <Route path="/rooms/create" element={<RoomCreatePage />} />
            <Route path="/rooms/update/:roomId" element={<RoomUpdatePage />} />
            <Route path="/buildings" element={<BuildingListPage />} />
            <Route path="/buildings/create" element={<BuildingCreatePage />} />
            <Route path="/buildings/update/:buildingId" element={<BuildingUpdatePage />} />
          </Routes>
        </Container>
      </BrowserRouter>
    </QueryClientProvider>
  );
}