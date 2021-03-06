﻿using HeroesMatchTracker.Data.Databases;
using HeroesMatchTracker.Data.Models.Replays;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace HeroesMatchTracker.Data.Queries.Replays
{
    public class MatchTeamObjective : NonContextQueriesBase<ReplayMatchTeamObjective>, IRawDataQueries<ReplayMatchTeamObjective>
    {
        public IEnumerable<ReplayMatchTeamObjective> ReadAllRecords()
        {
            using (var db = new ReplaysContext())
            {
                return db.ReplayMatchTeamObjectives.AsNoTracking().ToList();
            }
        }

        public IEnumerable<ReplayMatchTeamObjective> ReadLastRecords(int amount)
        {
            using (var db = new ReplaysContext())
            {
                return db.ReplayMatchTeamObjectives.AsNoTracking().OrderByDescending(x => x.ReplayId).Take(amount).ToList();
            }
        }

        public IEnumerable<ReplayMatchTeamObjective> ReadRecordsCustomTop(int amount, string columnName, string orderBy)
        {
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(orderBy))
                return new List<ReplayMatchTeamObjective>();

            if (columnName.Contains("TimeStamp"))
                columnName = string.Concat(columnName, "Ticks");

            if (amount == 0)
                amount = 1;

            using (var db = new ReplaysContext())
            {
                return db.ReplayMatchTeamObjectives.SqlQuery($"SELECT * FROM ReplayMatchTeamObjectives ORDER BY {columnName} {orderBy} LIMIT {amount}").AsNoTracking().ToList();
            }
        }

        public IEnumerable<ReplayMatchTeamObjective> ReadRecordsWhere(string columnName, string operand, string input)
        {
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(operand))
                return new List<ReplayMatchTeamObjective>();

            if (columnName.Contains("TimeStamp"))
            {
                if (TimeSpan.TryParse(input, out TimeSpan timeSpan))
                {
                    input = timeSpan.Ticks.ToString();
                    columnName = string.Concat(columnName, "Ticks");
                }
                else
                {
                    return new List<ReplayMatchTeamObjective>();
                }
            }
            else if (LikeOperatorInputCheck(operand, input))
            {
                input = $"%{input}%";
            }
            else if (input == null)
            {
                input = string.Empty;
            }

            using (var db = new ReplaysContext())
            {
                return db.ReplayMatchTeamObjectives.SqlQuery($"SELECT * FROM ReplayMatchTeamObjectives WHERE {columnName} {operand} @Input", new SQLiteParameter("@Input", input)).AsNoTracking().ToList();
            }
        }

        public IEnumerable<ReplayMatchTeamObjective> ReadTopRecords(int amount)
        {
            using (var db = new ReplaysContext())
            {
                return db.ReplayMatchTeamObjectives.AsNoTracking().Take(amount).ToList();
            }
        }

        internal override long CreateRecord(ReplaysContext db, ReplayMatchTeamObjective model)
        {
            db.ReplayMatchTeamObjectives.Add(model);
            db.SaveChanges();

            return model.ReplayId;
        }

        internal override long UpdateRecord(ReplaysContext db, ReplayMatchTeamObjective model)
        {
            throw new NotImplementedException();
        }

        internal override bool IsExistingRecord(ReplaysContext db, ReplayMatchTeamObjective model)
        {
            throw new NotImplementedException();
        }
    }
}
