using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Models;

namespace ProjektWPF.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
       
        public DbSet<Note> Notes { get; set; }

        public DbSet<Movie> Movies { get; set; }

        public DbSet<UserMovie> UserMovies { get; set; }

        public DbSet<Expenses> Expenses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WpfDB;Trusted_Connection=True;");
            
            
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>()
                .HasOne(n => n.User) 
                .WithMany(u => u.Notes) 
                .HasForeignKey(n => n.UserId) 
                .OnDelete(DeleteBehavior.Cascade)
                ;

            

            modelBuilder.Entity<UserMovie>()
       .HasKey(um => new { um.UserId, um.MovieId });

            modelBuilder.Entity<UserMovie>()
                .HasOne(um => um.User)
                .WithMany(u => u.UserMovies)
                .HasForeignKey(um => um.UserId);

            modelBuilder.Entity<UserMovie>()
                .HasOne(um => um.Movie)
                .WithMany(m => m.UserMovies)
                .HasForeignKey(um => um.MovieId);

            modelBuilder.Entity<Expenses>()
                .HasOne(p => p.User)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.UserId);
        }
    }
}
