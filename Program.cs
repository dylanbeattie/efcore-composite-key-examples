using Microsoft.EntityFrameworkCore;

var db = new DemoDbContext("Server=localhost;Database=demo_db;User Id=demo_user;Password=demo_pass;MultipleActiveResultSets=true;TrustServerCertificate=True");
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

var venues = db.Venues
	.Include(v => v.Shows).ThenInclude(show => show.HeadlineArtist)
	.Include(v => v.Shows).ThenInclude(show => show.SupportSlots).ThenInclude(slot => slot.Artist);

foreach (var venue in venues) {
	Console.WriteLine(venue.Name);
	foreach (var show in venue.Shows) {
		Console.WriteLine($"{show.Date}: {show.HeadlineArtist.Name}");
		if (show.SupportSlots.Any()) {
			Console.Write("  plus special guests: ");
			var supportNames = show.SupportSlots
				.OrderBy(slot => slot.SlotNumber)
				.Select(slot => slot.Artist.Name).ToArray();

			Console.WriteLine(String.Join(", ", supportNames));
		}
	}
	Console.WriteLine("====================================================");
}

public class Venue {
	public int Id { get; set; }
	public string Name { get; set; } = String.Empty;
	public List<Show> Shows { get; set; } = [];
}

public class Artist {
	public int Id { get; set; }
	public string Name { get; set; } = String.Empty;
	public List<Show> HeadlineShows { get; set; } = [];
	public List<SupportSlot> SupportSlots { get; set; } = [];
}

public class Show {
	public Venue Venue { get; set; } = default!;
	public DateOnly Date { get; set; }
	public Artist? HeadlineArtist { get; set; }
	public List<SupportSlot> SupportSlots { get; set; } = [];
}

public class SupportSlot {
	public Show Show { get; set; } = default!;
	public int SlotNumber { get; set; }
	public Artist? Artist { get; set; }
}

public class DemoDbContext(string sqlConnectionString) : DbContext {

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseSqlServer(sqlConnectionString);

	public DbSet<Artist> Artists { get; set; } = default!;
	public DbSet<Venue> Venues { get; set; } = default!;
	public DbSet<Show> Shows { get; set; } = default!;
	public DbSet<SupportSlot> SupportSlots { get; set; } = default!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		// There appears to be no way in EF Core to configure these
		// composite keys without using strings:
		modelBuilder.Entity<Show>().HasKey("VenueId", "Date");
		modelBuilder.Entity<SupportSlot>().HasKey("ShowVenueId", "ShowDate", "SlotNumber");

		modelBuilder.Entity<Show>().HasMany(show => show.SupportSlots).WithOne(slot => slot.Show).OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Artist>().HasMany(artist => artist.HeadlineShows).WithOne(slot => slot.HeadlineArtist).OnDelete(DeleteBehavior.SetNull);
		modelBuilder.Entity<Artist>().HasMany(artist => artist.SupportSlots).WithOne(slot => slot.Artist).OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<Artist>().HasData(
			new { Id = 1, Name = "Alice Cooper" },
			new { Id = 2, Name = "Bon Jovi" },
			new { Id = 3, Name = "Counting Crows" },
			new { Id = 4, Name = "Def Leppard" },
			new { Id = 5, Name = "Europe" },
			new { Id = 6, Name = "Foo Fighters" },
			new { Id = 7, Name = "Green Day" },
			new { Id = 8, Name = "Heart " }
		);
		modelBuilder.Entity<Venue>().HasData(
			new { Id = 1, Name = "Astoria, London" },
			new { Id = 2, Name = "Bataclan, Paris" },
			new { Id = 3, Name = "Columbus Club, Berlin" }
		);

		modelBuilder.Entity<Show>().HasData(
			new { VenueId = 1, Date = new DateOnly(2024, 3, 1), HeadlineArtistId = 1 },
			new { VenueId = 1, Date = new DateOnly(2024, 3, 2), HeadlineArtistId = 2 },
			new { VenueId = 1, Date = new DateOnly(2024, 3, 3), HeadlineArtistId = 3 },
			new { VenueId = 2, Date = new DateOnly(2024, 3, 1), HeadlineArtistId = 2 },
			new { VenueId = 2, Date = new DateOnly(2024, 3, 2), HeadlineArtistId = 3 },
			new { VenueId = 2, Date = new DateOnly(2024, 3, 3), HeadlineArtistId = 1 },
			new { VenueId = 3, Date = new DateOnly(2024, 3, 1), HeadlineArtistId = 3 },
			new { VenueId = 3, Date = new DateOnly(2024, 3, 2), HeadlineArtistId = 4 },
			new { VenueId = 3, Date = new DateOnly(2024, 3, 3), HeadlineArtistId = 5 }
		);

		modelBuilder.Entity<SupportSlot>().HasData(
			new { ShowVenueId = 1, ShowDate = new DateOnly(2024, 3, 1), SlotNumber = 1, ArtistId = 4 },
			new { ShowVenueId = 1, ShowDate = new DateOnly(2024, 3, 1), SlotNumber = 2, ArtistId = 7 },
			new { ShowVenueId = 1, ShowDate = new DateOnly(2024, 3, 2), SlotNumber = 1, ArtistId = 5 },
			new { ShowVenueId = 1, ShowDate = new DateOnly(2024, 3, 2), SlotNumber = 2, ArtistId = 8 },
			new { ShowVenueId = 2, ShowDate = new DateOnly(2024, 3, 3), SlotNumber = 1, ArtistId = 4 },
			new { ShowVenueId = 2, ShowDate = new DateOnly(2024, 3, 3), SlotNumber = 2, ArtistId = 7 }
		);
	}
}