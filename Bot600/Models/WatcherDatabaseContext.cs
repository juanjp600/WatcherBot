#nullable enable
using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bot600.Models
{
    public class WatcherDatabaseContext : DbContext
    {
        private static readonly Lazy<string> ConnectionString =
            new(() => {
                var configBuilder = new ConfigurationBuilder();
                var jsonFile = configBuilder.AddJsonFile("appsettings.json");
                var build = jsonFile.Build();
                var connString = build.GetConnectionString("WatcherDatabase");
                var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
                return connString.Replace("$WD",
                                        Path.GetDirectoryName(
                                            executingAssemblyLocation));
            });

#pragma warning disable 8618
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DbSet<User> Users { get; set; }
#pragma warning restore 8618

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite(ConnectionString.Value);
    }
}
