﻿using Cortex.Models;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {           
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
