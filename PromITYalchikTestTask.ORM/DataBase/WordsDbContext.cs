using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PromITYalchikTestTask.SqlClient.Models;
using PromITYalchikTestTask.SqlClient.Models.Configurations;

namespace PromITYalchikTestTask.ORM.DataBase
{
    public class WordsDbContext( string _connectionString) : DbContext
    {
        public string ConnectionString { get; } = _connectionString;


        public DbSet<WordEntity> Words { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration<WordEntity>(new WordConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
