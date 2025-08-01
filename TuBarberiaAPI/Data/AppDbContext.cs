using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Models;

namespace TuBarberiaAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<BarberShop> BarberShops => Set<BarberShop>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<BarberService> BarberServices => Set<BarberService>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<BarberBlockedTime> BarberBlockedTimes => Set<BarberBlockedTime>();
        public DbSet<BarberSchedule> BarberSchedules => Set<BarberSchedule>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación uno-a-muchos: una barbería tiene muchos usuarios (barberos)
            modelBuilder.Entity<User>()
                .HasOne(u => u.BarberShop)
                .WithMany(b => b.Users)
                .HasForeignKey(u => u.BarberShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación uno-a-uno: barbería tiene un administrador
            modelBuilder.Entity<BarberShop>()
                .HasOne(b => b.AdminUser)
                .WithMany() // sin propiedad inversa (para evitar ambigüedad)
                .HasForeignKey(b => b.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BarberService>()
            .HasOne(bs => bs.User)
            .WithMany(u => u.BarberServices)
            .HasForeignKey(bs => bs.UserId);

            modelBuilder.Entity<BarberService>()
                .HasOne(bs => bs.Service)
                .WithMany(s => s.BarberServices)
                .HasForeignKey(bs => bs.ServiceId);
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Barber)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.BarberId);

            modelBuilder.Entity<BarberBlockedTime>()
                .HasOne(b => b.Barber)
                .WithMany(u => u.BlockedTimes)
                .HasForeignKey(b => b.BarberId);
            modelBuilder.Entity<BarberSchedule>()
                .HasOne(s => s.Barber)
                .WithMany(u => u.Schedules)
                .HasForeignKey(s => s.BarberId);
        }



    }
}
