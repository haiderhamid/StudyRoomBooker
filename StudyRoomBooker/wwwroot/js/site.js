const timeToMinutes = time => Number(time.substring(0, 2)) * 60 + Number(time.substring(3, 5));

// For every form that has a start and an end time.
document.querySelectorAll('form').forEach(form => {
    const fromSelect = form.elements['FromTime'] || form.elements['from'];
    const toSelect = form.elements['ToTime'] || form.elements['to'];
    if (!fromSelect || !toSelect) {
        return;
    }

    const dateInput = form.elements['Date'] || form.elements['date'];
    const now = new Date();
    const todayValue = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    const currentTime = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

    // Hides times that have already passed today.
    const hidePassedTimes = () => {
        const isToday = dateInput && dateInput.value === todayValue;
        [fromSelect, toSelect].forEach(select => {
            Array.from(select.options).forEach(option => {
                const hasPassed = isToday && option.value !== '' && option.value.substring(0, 5) <= currentTime && !option.selected;
                option.hidden = hasPassed;
                option.disabled = hasPassed;
            });
        });
    };

    hidePassedTimes();
    if (dateInput) {
        dateInput.addEventListener('change', hidePassedTimes);
    }

    let previousFrom = fromSelect.value;
    // When the start time changes, the end time moves too, so the booking keeps the same length.
    fromSelect.addEventListener('change', () => {
        const newFrom = fromSelect.value;
        if (newFrom) {
            let duration = 60;
            if (previousFrom && toSelect.value && toSelect.value > previousFrom) {
                duration = timeToMinutes(toSelect.value) - timeToMinutes(previousFrom);
            }
            const end = Math.min(timeToMinutes(newFrom) + duration, 20 * 60);
            const endTime = `${String(Math.floor(end / 60)).padStart(2, '0')}:${String(end % 60).padStart(2, '0')}`;
            const option = Array.from(toSelect.options).find(o => o.value.startsWith(endTime));
            if (option) {
                toSelect.value = option.value;
            }
        }
        previousFrom = newFrom;
    });
});

// Search forms: search again as soon as a field changes. Empty fields are left out of the URL.
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

// Booking form: shows the calendar for the chosen room and updates it when something changes.
const bookingSchedule = document.getElementById('booking-schedule');
const bookingForm = document.querySelector('form[data-booking-form]');

if (bookingSchedule && bookingForm) {
    // Loads the calendar for the chosen room and week from RoomController.Schedule.
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
    // The arrows in the calendar load another week without leaving the form.
    bookingSchedule.addEventListener('click', event => {
        const link = event.target.closest('[data-week]');
        if (link) {
            event.preventDefault();
            loadSchedule(link.dataset.week);
        }
    });
    loadSchedule();
}
