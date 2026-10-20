import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Container } from 'react-bootstrap';
import { AuthProvider } from './auth/AuthContext';
import ProtectedRoute from './auth/ProtectedRoute';
import AdminRoute from './auth/AdminRoute';
import LoginPage from './auth/LoginPage';
import RegisterPage from './auth/RegisterPage';
import NavMenu from './shared/NavMenu';
import PrivacyPage from './shared/PrivacyPage';
import NotFoundPage from './shared/NotFoundPage';
import HomePage from './home/HomePage';
import RoomListPage from './rooms/RoomListPage';
import RoomCreatePage from './rooms/RoomCreatePage';
import RoomUpdatePage from './rooms/RoomUpdatePage';
import RoomDetailsPage from './rooms/RoomDetailsPage';
import BuildingListPage from './buildings/BuildingListPage';
import BuildingCreatePage from './buildings/BuildingCreatePage';
import BuildingUpdatePage from './buildings/BuildingUpdatePage';
import BookingListPage from './bookings/BookingListPage';
import BookingCreatePage from './bookings/BookingCreatePage';
import BookingUpdatePage from './bookings/BookingUpdatePage';
import BookingDetailsPage from './bookings/BookingDetailsPage';

const queryClient = new QueryClient();

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <div className="d-flex flex-column min-vh-100">
            <NavMenu />
            <main className="flex-grow-1">
              <Container className="py-4">
                <Routes>
                  <Route path="/" element={<HomePage />} />
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />
                  <Route path="/privacy" element={<PrivacyPage />} />
                  <Route path="/rooms" element={<RoomListPage />} />
                  <Route path="/rooms/:roomId" element={<RoomDetailsPage />} />
                  <Route path="/buildings" element={<BuildingListPage />} />
                  <Route path="/bookings/:bookingId" element={<BookingDetailsPage />} />

                  <Route element={<AdminRoute />}>
                    <Route path="/rooms/create" element={<RoomCreatePage />} />
                    <Route path="/rooms/update/:roomId" element={<RoomUpdatePage />} />
                    <Route path="/buildings/create" element={<BuildingCreatePage />} />
                    <Route path="/buildings/update/:buildingId" element={<BuildingUpdatePage />} />
                  </Route>

                  <Route element={<ProtectedRoute />}>
                    <Route path="/bookings" element={<BookingListPage />} />
                    <Route path="/bookings/create" element={<BookingCreatePage />} />
                    <Route path="/bookings/update/:bookingId" element={<BookingUpdatePage />} />
                  </Route>
                  <Route path="*" element={<NotFoundPage />} />
                </Routes>
              </Container>
            </main>
            <footer className="srb-footer">
              <Container className="py-3 d-flex flex-wrap justify-content-between gap-2">
                <span>StudyRoomBooker, a student project for ITPE3200 · <Link to="/privacy">Privacy</Link></span>
                <span>Rooms can be booked from 07:00 to 20:00 in 15-minute slots.</span>
              </Container>
            </footer>
          </div>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}