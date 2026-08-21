namespace CinemaTicketingAssignment.Models;

public class SeatSelectionViewModel
{
    public Showtime Showtime { get; set; } = null!;
    public List<string> BookedSeats { get; set; } = [];
    public List<string> LockedSeats { get; set; } = [];
    public List<string> MyHeldSeats { get; set; } = [];
    public int Columns { get; set; } = 10;
    public int Rows { get; set; }
}
