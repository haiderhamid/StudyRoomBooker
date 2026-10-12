document.querySelectorAll('form[data-auto-submit]').forEach(form => {
    form.addEventListener('change', () => {
        const params = new URLSearchParams();
        new FormData(form).forEach((value, key) => {
            if (value !== '') {
                params.append(key, value);
            }
        });
        const query = params.toString();
        window.location.href = form.action + (query ? '?' + query : '');
    });
});

const bookingSchedule = document.getElementById('booking-schedule');
const bookingForm = document.querySelector('form[data-booking-form]');

if (bookingSchedule && bookingForm) {
    const loadSchedule = week => {
        const roomId = bookingForm.elements['Booking.RoomId'].value;
        if (!roomId) {
            bookingSchedule.innerHTML = '<p class="empty-state">Choose a room to see when it is free.</p>';
            return;
        }

        const params = new URLSearchParams({
            id: roomId,
            date: bookingForm.elements['Date'].value,
            from: bookingForm.elements['FromTime'].value,
            to: bookingForm.elements['ToTime'].value
        });
        if (week) {
            params.set('week', week);
        }
        if (bookingSchedule.dataset.exclude) {
            params.set('excludeBookingId', bookingSchedule.dataset.exclude);
        }

        fetch(`${bookingSchedule.dataset.url}?${params.toString()}`)
            .then(response => response.ok ? response.text() : Promise.reject(response.status))
            .then(html => { bookingSchedule.innerHTML = html; })
            .catch(() => {
                bookingSchedule.innerHTML = '<p class="empty-state">Could not load the schedule.</p>';
            });
    };

    bookingForm.addEventListener('change', () => loadSchedule());
    bookingSchedule.addEventListener('click', event => {
        const link = event.target.closest('[data-week]');
        if (link) {
            event.preventDefault();
            loadSchedule(link.dataset.week);
        }
    });
    loadSchedule();
}
