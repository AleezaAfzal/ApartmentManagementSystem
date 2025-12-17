using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ApartmentManagement.Models;

namespace ApartmentManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Building> Buildings { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<VisitRequest> VisitRequests { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<VenueBooking> VenueBookings { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision for Apartment.Size
            builder.Entity<Apartment>()
                .Property(a => a.Size)
                .HasPrecision(18, 2);

            // Configure relationships
            builder.Entity<Building>()
                .HasMany(b => b.Apartments)
                .WithOne(a => a.Building)
                .HasForeignKey(a => a.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // VisitRequests: Use NoAction to avoid cascade path conflicts
            // (VisitRequests -> Apartments CASCADE, VisitRequests -> Users CASCADE creates conflict)
            builder.Entity<Apartment>()
                .HasMany(a => a.VisitRequests)
                .WithOne(v => v.Apartment)
                .HasForeignKey(v => v.ApartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VisitRequest>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Apartment>()
                .HasMany(a => a.Tenants)
                .WithOne(t => t.Apartment)
                .HasForeignKey(t => t.ApartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tenant>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Tenant>()
                .HasMany(t => t.Payments)
                .WithOne(p => p.Tenant)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Tenant>()
                .HasMany(t => t.Complaints)
                .WithOne(c => c.Tenant)
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Tenant>()
                .HasMany(t => t.VenueBookings)
                .WithOne(v => v.Tenant)
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reviews: Use NoAction for Tenant to avoid cascade path conflicts
            // (Reviews -> Tenants CASCADE, Reviews -> Apartments CASCADE creates conflict)
            builder.Entity<Apartment>()
                .HasMany(a => a.Reviews)
                .WithOne(r => r.Apartment)
                .HasForeignKey(r => r.ApartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

