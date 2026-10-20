using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

// A student who has joined someone else's booking.
// The index stops the same user from joining the same booking twice.
[Index(nameof(BookingId), nameof(UserName), IsUnique = true)]
public class BookingParticipant
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    public virtual Booking? Booking { get; set; }
}
