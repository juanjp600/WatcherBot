using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WatcherBot.Models;

public class WatcherDatabaseContext : DbContext
{
    private static readonly Lazy<string> ConnectionString = new(() =>
    {
        var                   configBuilder          = new ConfigurationBuilder();
        IConfigurationBuilder jsonFile               = configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot    build                  = jsonFile.Build();
        const string          watcherDatabaseSection = "WatcherDatabase";
        string connString = build.GetConnectionString(watcherDatabaseSection)
                            ?? throw new
                                InvalidOperationException($"Missing connection string {watcherDatabaseSection}");
        string executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
        return connString.Replace("$WD", Path.GetDirectoryName(executingAssemblyLocation));
    });

#pragma warning disable 8618
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public DbSet<User> Users { get; set; }
#pragma warning restore 8618

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(ConnectionString.Value);
}
