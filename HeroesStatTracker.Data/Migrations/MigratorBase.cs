﻿using HeroesStatTracker.Data.Databases;
using HeroesStatTracker.Migrations;
using System;
using System.IO;
using System.Linq;

namespace HeroesStatTracker.Data.Migrations
{
    internal class MigratorBase<T, U> 
        where T : StatTrackerDbContext, new()
        where U : ContextMigrator, new()
    {
        private string DbName;
        bool DatabaseFileCreated;
        int RequiredDatabaseVersion;

        private readonly string MigrationLogFile = "Logs/DatabasesMigrationLog.txt";

        public MigratorBase(string dbName, bool databaseFileCreated, int requiredDatabaseVersion)
        {
            DeleteMigrationLog();

            DbName = dbName;
            DatabaseFileCreated = databaseFileCreated;
            RequiredDatabaseVersion = requiredDatabaseVersion;
        }

        public void Initialize(bool logger = false)
        {
            try
            {
                using (T db = new T())
                {
                    int currentVersion = 0;
                    if (DatabaseFileCreated)
                    {
                        currentVersion = RequiredDatabaseVersion;
                        db.SchemaInfo.Add(new SchemaInfo() { Version = currentVersion });
                        db.SaveChanges();
                    }

                    if (db.SchemaInfo.Count() > 0)
                    {
                        currentVersion = db.SchemaInfo.Max(x => x.Version);
                        if (logger)
                        {
                            MigrationLogger($"Current Version: {currentVersion}");
                            MigrationLogger($"Required Version: {RequiredDatabaseVersion}");
                        }
                    }

                    if (currentVersion >= RequiredDatabaseVersion)
                    {
                        if (logger)
                            MigrationLogger("No migration required");

                        return;
                    }

                    U contextMigrator = new U();

                    while (currentVersion < RequiredDatabaseVersion)
                    {
                        currentVersion++;

                        if (logger)
                            MigrationLogger($"Migrating to version {currentVersion}");

                        foreach (string migration in contextMigrator.Migrations[currentVersion])
                        {
                            db.Database.ExecuteSqlCommand(migration);
                        }

                        if (contextMigrator.MigrationAddons.ContainsKey(currentVersion))
                        {
                            foreach (IMigrationAddon migration in contextMigrator.MigrationAddons[currentVersion])
                            {
                                migration.Execute();
                            }
                        }

                        db.SchemaInfo.Add(new SchemaInfo() { Version = currentVersion });
                        db.SaveChanges();

                        if (logger)
                            MigrationLogger($"Migration version {currentVersion} completed");
                    }

                    if (logger)
                        MigrationLogger($"Migration completed");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected void MigrationLogger(string message)
        {
            using (StreamWriter writer = new StreamWriter(MigrationLogFile, true))
            {
                writer.WriteLine($"[{DateTime.Now.ToLocalTime()}] [{DbName}] {message}");
            }
        }

        private void DeleteMigrationLog()
        {
            if (File.Exists(MigrationLogFile))
                File.Delete(MigrationLogFile);
        }
    }
}
