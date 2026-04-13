using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenKNX.IoT.Database
{
    internal class ResourceContext : DbContext
    {
        public DbSet<ResourceData> Resources { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string db_path = "resources.db";
            if(Directory.Exists("/config"))
            {
                db_path = "/config/resources.db";
            }
            optionsBuilder.UseSqlite($"Data Source={db_path}");
        }
    }
}
