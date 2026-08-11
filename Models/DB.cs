using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Models;

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    // --- ADD THESE TWO LINES TO FIX THE ERROR ---
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    // --------------------------------------------

    public DbSet<Movie> Movies { get; set; }
    public DbSet<CinemaHall> CinemaHalls { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Concession> Concessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Admin user (for testing the CRUD/admin pages) -----
        // Login at /Account/Login with these credentials.
        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                Id = "ADM001",
                Name = "System Admin",
                Email = "admin@cinema.com",
                Hash = "Admin@123"
            }
        );

        // ----- Movies -----
        modelBuilder.Entity<Movie>().HasData(
            new Movie
            {
                Id = "MOV001",
                Title = "The Dark Knight",
                Synopsis = "Batman faces the Joker in a battle for Gotham's soul.",
                Genre = "Action",
                Language = "English",
                AgeClassification = "18",
                ReleaseDate = new DateTime(2008, 7, 18),
                Status = MovieStatus.NowShowing,
                PosterImagePath = null
            },
            new Movie
            {
                Id = "MOV002",
                Title = "Inception",
                Synopsis = "A thief who steals secrets through dream-sharing takes on one last job.",
                Genre = "Sci-Fi",
                Language = "English",
                AgeClassification = "PG13",
                ReleaseDate = new DateTime(2011, 7, 8),
                Status = MovieStatus.NowShowing,
                PosterImagePath = null
            },
            new Movie
            {
                Id = "MOV003",
                Title = "Spiderman Brand New Day",
                Synopsis = "Spiderman returns for a brand new adventure in New York City.",
                Genre = "Action",
                Language = "English",
                AgeClassification = "PG13",
                ReleaseDate = new DateTime(2026, 7, 31),
                Status = MovieStatus.ComingSoon,
                PosterImagePath = null
            },
            new Movie
            {
                Id = "MOV004",
                Title = "The Odyssey",
                Synopsis = "An epic retelling of Homer's legendary voyage.",
                Genre = "Adventure",
                Language = "English",
                AgeClassification = "PG13",
                ReleaseDate = new DateTime(2026, 7, 17),
                Status = MovieStatus.ComingSoon,
                PosterImagePath = null
            },
            new Movie
            {
                Id = "MOV005",
                Title = "Interstellar",
                Synopsis = "A team of explorers travel through a wormhole in space to save humanity.",
                Genre = "Sci-Fi",
                Language = "English",
                AgeClassification = "PG13",
                ReleaseDate = new DateTime(2014, 11, 7),
                Status = MovieStatus.NowShowing,
                PosterImagePath = null
            }
        );

        // ----- Cinema Halls -----
        modelBuilder.Entity<CinemaHall>().HasData(
            new CinemaHall { Id = "HALL001", HallName = "Deluxe", HallType = "Standard Deluxe", SeatCapacity = 120 },
            new CinemaHall { Id = "HALL002", HallName = "4DX", HallType = "Motion & Effects", SeatCapacity = 80 },
            new CinemaHall { Id = "HALL003", HallName = "Dolby D-BOX", HallType = "Dolby Atmos + D-BOX Seats", SeatCapacity = 100 },
            new CinemaHall { Id = "HALL004", HallName = "Samsung Onyx", HallType = "LED Cinema Screen", SeatCapacity = 150 },
            new CinemaHall { Id = "HALL005", HallName = "70MM IMAX", HallType = "IMAX", SeatCapacity = 200 }
        );

        // ----- Showtimes (10:45 AM & 2:10 PM slots, spread across halls) -----
        var showDate = new DateTime(2026, 8, 1);
        var slotMorning = showDate.AddHours(10).AddMinutes(45);   // 10:45 AM
        var slotAfternoon = showDate.AddHours(14).AddMinutes(10); // 2:10 PM

        modelBuilder.Entity<Showtime>().HasData(
            new Showtime { Id = "SHOW001", MovieId = "MOV001", CinemaHallId = "HALL001", StartDateTime = slotMorning, EndDateTime = slotMorning.AddMinutes(152), TicketPrice = 25.00m },
            new Showtime { Id = "SHOW002", MovieId = "MOV001", CinemaHallId = "HALL002", StartDateTime = slotAfternoon, EndDateTime = slotAfternoon.AddMinutes(152), TicketPrice = 35.00m },

            new Showtime { Id = "SHOW003", MovieId = "MOV002", CinemaHallId = "HALL003", StartDateTime = slotMorning, EndDateTime = slotMorning.AddMinutes(148), TicketPrice = 30.00m },
            new Showtime { Id = "SHOW004", MovieId = "MOV002", CinemaHallId = "HALL004", StartDateTime = slotAfternoon, EndDateTime = slotAfternoon.AddMinutes(148), TicketPrice = 40.00m },

            new Showtime { Id = "SHOW005", MovieId = "MOV003", CinemaHallId = "HALL005", StartDateTime = slotMorning, EndDateTime = slotMorning.AddMinutes(130), TicketPrice = 45.00m },
            new Showtime { Id = "SHOW006", MovieId = "MOV003", CinemaHallId = "HALL001", StartDateTime = slotAfternoon, EndDateTime = slotAfternoon.AddMinutes(130), TicketPrice = 25.00m },

            new Showtime { Id = "SHOW007", MovieId = "MOV004", CinemaHallId = "HALL002", StartDateTime = slotMorning, EndDateTime = slotMorning.AddMinutes(140), TicketPrice = 35.00m },
            new Showtime { Id = "SHOW008", MovieId = "MOV004", CinemaHallId = "HALL003", StartDateTime = slotAfternoon, EndDateTime = slotAfternoon.AddMinutes(140), TicketPrice = 30.00m },

            new Showtime { Id = "SHOW009", MovieId = "MOV005", CinemaHallId = "HALL004", StartDateTime = slotMorning, EndDateTime = slotMorning.AddMinutes(169), TicketPrice = 40.00m },
            new Showtime { Id = "SHOW010", MovieId = "MOV005", CinemaHallId = "HALL005", StartDateTime = slotAfternoon, EndDateTime = slotAfternoon.AddMinutes(169), TicketPrice = 45.00m }
        );
    }
}