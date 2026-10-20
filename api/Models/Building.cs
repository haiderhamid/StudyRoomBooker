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

    // Short name for the building, for example "Pilestredet 35" becomes "P35". It is not saved in the database.
    public string ShortName
    {
        get
        {
            var shortName = "";
            foreach (var word in Name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                shortName += char.IsDigit(word[0]) ? word : char.ToUpper(word[0]).ToString();
            }
            return shortName;
        }
    }
}