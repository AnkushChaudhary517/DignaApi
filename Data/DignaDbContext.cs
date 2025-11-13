using DignaApi.Entities;
using DignaApi.Entities.DynamoEntitites;
using Microsoft.EntityFrameworkCore;


namespace DignaApi.Data;

public class DignaDbContext : DbContext
{
    public DignaDbContext(DbContextOptions<DignaDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<ImageDownloadSize> ImageDownloadSizes => Set<ImageDownloadSize>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);
        });

        // UserProfile configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.Website).HasMaxLength(255);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
            entity.Property(e => e.Instagram).HasMaxLength(255);
            entity.Property(e => e.Twitter).HasMaxLength(255);
            entity.Property(e => e.Youtube).HasMaxLength(255);
            entity.Property(e => e.Pinterest).HasMaxLength(255);
        });

        // Image configuration
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Photographer).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AspectRatio).IsRequired().HasMaxLength(50);
            //entity.Property(e => e.QualityUrls);
                //.WithOne(d => d.Image)
                //.HasForeignKey(d => d.ImageId)
                //.OnDelete(DeleteBehavior.Cascade);
        });

        // ImageDownloadSize configuration
        modelBuilder.Entity<ImageDownloadSize>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }

    public async Task SeedAsync()
    {
        // Only seed if database is empty
        if (await Users.AnyAsync())
            return;

        var users = new[]
        {
            new User
            {
                Id = "user_12345",
                Email = "johndoe@mail.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("securepassword123"),
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    UserId = "user_12345",
                    Bio = "Digital creator and photographer",
                    Website = "https://johndoe.com",
                    Instagram = "https://www.instagram.com/johndoe",
                    Twitter = "https://x.com/johndoe",
                    Youtube = "https://www.youtube.com/c/johndoe",
                    Pinterest = "https://in.pinterest.com/johndoe",
                    Newsletter = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            new User
            {
                Id = "user_67890",
                Email = "jane.smith@mail.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("securepassword456"),
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    UserId = "user_67890",
                    Newsletter = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        await Users.AddRangeAsync(users);
        await SaveChangesAsync();

        // Seed images with download sizes
        var images = new[]
        {
            new Image
            {
                Id = "img_001",
                Title = "Lion",
                Description = "Majestic lion portrait",
                Photographer = "Phillip Saris",
                ImageUrl = "https://images.unsplash.com/photo-1614027164847-1b28cfe1df60?w=400&h=400&fit=crop",
                AspectRatio = "square",
                Downloads = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Caption = "",
                //DownloadSizes = new List<ImageDownloadSize>
                //{
                //    new ImageDownloadSize { Name = "Small", Width = 640, Height = 640, FileSizeBytes = 256000 },
                //    new ImageDownloadSize { Name = "Medium", Width = 1280, Height = 1280, FileSizeBytes = 1024000 },
                //    new ImageDownloadSize { Name = "Large", Width = 2560, Height = 2560, FileSizeBytes = 4096000 },
                //    new ImageDownloadSize { Name = "Original", Width = 5000, Height = 5000, FileSizeBytes = 15000000 }
                //}
            }
        };

        await Images.AddRangeAsync(images);
        await SaveChangesAsync();
    }
}
