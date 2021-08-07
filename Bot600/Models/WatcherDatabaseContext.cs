using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bot600.Models
{
    public class WatcherDatabaseContext : DbContext
    {
        private static readonly Lazy<string> ConnectionString =
            new(() => new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()
                                                .GetConnectionString("WatcherDatabase")
                                                .Replace("$WD", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));

#pragma warning disable 8618
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DbSet<User> Users { get; set; }
#pragma warning restore 8618

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite(ConnectionString.Value);
    }
}
