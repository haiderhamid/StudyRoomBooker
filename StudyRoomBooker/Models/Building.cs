using System.ComponentModel.DataAnnotations;

public class Building
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an address.")]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public virtual List<Room>? Rooms { get; set; }

    // "Pilestredet 35" -> "P35". Not stored in the database.
    public string ShortName
    {
        get
        {
            var shortName = "";
            // Name can be null while an empty form is validated
            foreach (var word in (Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                shortName += char.IsDigit(word[0]) ? word : char.ToUpper(word[0]).ToString();
            }
            return shortName;
        }
    }
}