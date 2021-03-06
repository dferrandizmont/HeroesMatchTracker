﻿using Heroes.Helpers;
using Heroes.Icons;
using Heroes.ReplayParser;
using HeroesMatchTracker.Data.Databases;
using HeroesMatchTracker.Data.Generic;
using HeroesMatchTracker.Data.Models.Replays;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HeroesMatchTracker.Data.Queries.Replays
{
    public class ReplayFileData : IDisposable
    {
        private ReplaysContext ReplaysContext;
        private Replay Replay;
        private ReplaysDb ReplaysDb;
        private bool DisposedValue = false;
        private IHeroesIconsService HeroesIcons;

        public ReplayFileData(Replay replay, IHeroesIconsService heroesIcons)
        {
            ReplaysContext = new ReplaysContext();
            Replay = replay;
            HeroesIcons = heroesIcons;
            HeroesIcons.LoadHeroesBuild(Replay.ReplayBuild); // needed for auto translations
            ReplaysDb = new ReplaysDb();
        }

        public long ReplayId { get; private set; }
        public DateTime ReplayTimeStamp { get; private set; }

        public ReplayResult SaveAllData(string fileName)
        {
            using (ReplaysContext)
            {
                using (var dbTransaction = ReplaysContext.Database.BeginTransaction())
                {
                    try
                    {
                        if (BasicData(fileName))
                            return ReplayResult.Duplicate;

                        PlayerRelatedData();
                        MatchTeamBans();
                        MatchTeamLevels();
                        MatchTeamExperience();
                        MatchMessages();
                        MatchObjectives();

                        dbTransaction.Commit();

                        return ReplayResult.Saved;
                    }
                    catch (Exception)
                    {
                        dbTransaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)ReplaysContext).Dispose();
                }

                DisposedValue = true;
            }
        }

        // returns true if replay already exists in database
        private bool BasicData(string fileName)
        {
            if (!HeroesIcons.MapBackgrounds().MapNameTranslation(Replay.Map, out string mapName))
            {
                if (!AutoTranslateMapNameByMapObjectives(out mapName))
                    throw new TranslationException(RetrieveAllMapAndHeroNames());
            }

            ReplayMatch replayMatch = new ReplayMatch
            {
                Frames = Replay.Frames,
                GameMode = Replay.GameMode,
                GameSpeed = Replay.GameSpeed.ToString(),
                IsGameEventsParsed = Replay.IsGameEventsParsedSuccessfully,
                MapName = mapName,
                RandomValue = Replay.RandomValue,
                ReplayBuild = Replay.ReplayBuild,
                ReplayLength = Replay.ReplayLength,
                ReplayVersion = Replay.ReplayVersion,
                TeamSize = Replay.TeamSize,
                TimeStamp = Replay.Timestamp,
                FileName = fileName,
            };

            replayMatch.Hash = ReplayHasher.HashReplay(replayMatch);
            ReplayTimeStamp = replayMatch.TimeStamp.Value;

            // check if replay was added to database already
            if (ReplaysDb.MatchReplay.IsExistingRecord(ReplaysContext, replayMatch))
            {
                ReplayId = ReplaysDb.MatchReplay.ReadReplayIdByHash(replayMatch);
                return true;
            }
            else
            {
                ReplayId = ReplaysDb.MatchReplay.CreateRecord(ReplaysContext, replayMatch);
                return false;
            }
        }

        private void PlayerRelatedData()
        {
            Player[] players = GetPlayers();

            foreach (var player in players.Select((value, index) => new { value, index }))
            {
                if (player.value == null)
                    continue;

                ReplayAllHotsPlayer hotsPlayer = new ReplayAllHotsPlayer
                {
                    BattleTagName = HeroesHelpers.BattleTags.GetBattleTagName(player.value.Name, player.value.BattleTag),
                    BattleNetId = player.value.BattleNetId,
                    BattleNetRegionId = player.value.BattleNetRegionId,
                    BattleNetSubId = player.value.BattleNetSubId,
                    BattleNetTId = player.value.BattleNetTId,
                    AccountLevel = player.value.AccountLevel,
                    LastSeen = Replay.Timestamp,
                    LastSeenBefore = null,
                    Seen = 1,
                };

                long playerId;

                // check if player is already in the database, update if found, otherwise add a new record
                if (ReplaysDb.HotsPlayer.IsExistingRecord(ReplaysContext, hotsPlayer))
                    playerId = ReplaysDb.HotsPlayer.UpdateRecord(ReplaysContext, hotsPlayer);
                else
                    playerId = ReplaysDb.HotsPlayer.CreateRecord(ReplaysContext, hotsPlayer);

                if (player.value.Character == null && Replay.GameMode == GameMode.Custom)
                {
                    player.value.Team = 4;
                    player.value.Character = "None";

                    ReplayMatchPlayer replayPlayer = new ReplayMatchPlayer
                    {
                        ReplayId = ReplayId,
                        PlayerId = playerId,
                        Character = player.value.Character,
                        CharacterLevel = player.value.CharacterLevel,
                        Difficulty = player.value.Difficulty.ToString(),
                        Handicap = player.value.Handicap,
                        IsAutoSelect = player.value.IsAutoSelect,
                        IsSilenced = player.value.IsSilenced,
                        IsWinner = player.value.IsWinner,
                        MountAndMountTint = player.value.MountAndMountTint,
                        PartyValue = player.value.PartyValue,
                        PlayerNumber = -1,
                        SkinAndSkinTint = player.value.SkinAndSkinTint,
                        Team = player.value.Team,
                        AccountLevel = player.value.AccountLevel,
                    };

                    ReplaysDb.MatchPlayer.CreateRecord(ReplaysContext, replayPlayer);
                }
                else
                {
                    if (!HeroesIcons.Heroes().HeroNameTranslation(player.value.Character, out string character))
                    {
                        if (!AutoTranslateHeroNameByTalent(player.value.Talents, out character))
                            throw new TranslationException(RetrieveAllMapAndHeroNames());
                    }

                    ReplayMatchPlayer replayPlayer = new ReplayMatchPlayer
                    {
                        ReplayId = ReplayId,
                        PlayerId = playerId,
                        Character = character,
                        CharacterLevel = player.value.CharacterLevel,
                        Difficulty = player.value.Difficulty.ToString(),
                        Handicap = player.value.Handicap,
                        IsAutoSelect = player.value.IsAutoSelect,
                        IsSilenced = player.value.IsSilenced,
                        IsWinner = player.value.IsWinner,
                        MountAndMountTint = player.value.MountAndMountTint,
                        PartyValue = player.value.PartyValue,
                        PlayerNumber = player.index,
                        SkinAndSkinTint = player.value.SkinAndSkinTint,
                        Team = player.value.Team,
                        AccountLevel = player.value.AccountLevel,
                    };

                    ReplaysDb.MatchPlayer.CreateRecord(ReplaysContext, replayPlayer);

                    AddScoreResults(player.value.ScoreResult, playerId);
                    AddPlayerTalents(player.value.Talents, playerId, character);
                    AddMatchAwards(player.value.ScoreResult.MatchAwards, playerId);
                }

                AddPlayerHeroes(player.value.PlayerCollectionDictionary, playerId);
            } // end foreach loop for players
        }

        private void AddScoreResults(ScoreResult sr, long playerId)
        {
            ReplayMatchPlayerScoreResult playerScore = new ReplayMatchPlayerScoreResult
            {
                ReplayId = ReplayId,
                Assists = sr.Assists,
                PlayerId = playerId,
                CreepDamage = sr.CreepDamage,
                DamageTaken = sr.DamageTaken,
                Deaths = sr.Deaths,
                ExperienceContribution = sr.ExperienceContribution,
                Healing = sr.Healing,
                HeroDamage = sr.HeroDamage,
                MercCampCaptures = sr.MercCampCaptures,
                MetaExperience = sr.MetaExperience,
                MinionDamage = sr.MinionDamage,
                SelfHealing = sr.SelfHealing,
                SiegeDamage = sr.SiegeDamage,
                SoloKills = sr.SoloKills,
                StructureDamage = sr.StructureDamage,
                SummonDamage = sr.SummonDamage,
                TakeDowns = sr.Takedowns,
                TimeCCdEnemyHeroes = sr.TimeCCdEnemyHeroes.HasValue ? sr.TimeCCdEnemyHeroes.Value.Ticks : (long?)null,
                TimeSpentDead = sr.TimeSpentDead,
                TownKills = sr.TownKills,
                WatchTowerCaptures = sr.WatchTowerCaptures,
            };

            ReplaysDb.MatchPlayerScoreResult.CreateRecord(ReplaysContext, playerScore);
        }

        private void AddPlayerTalents(Talent[] talents, long playerId, string playerCharacter)
        {
            Talent[] talentArray = new Talent[7]; // hold all 7 talents

            // add known talents
            for (int j = 0; j < talents.Count(); j++)
            {
                talentArray[j] = new Talent()
                {
                    TalentID = talents[j].TalentID,
                    TalentName = talents[j].TalentName,
                    TimeSpanSelected = talents[j].TimeSpanSelected,
                };
            }

            // make the rest null
            for (int j = talents.Count(); j < 7; j++)
            {
                talentArray[j] = new Talent()
                {
                    TalentID = null,
                    TalentName = null,
                    TimeSpanSelected = null,
                };
            }

            ReplayMatchPlayerTalent replayTalent = new ReplayMatchPlayerTalent
            {
                ReplayId = ReplayId,
                PlayerId = playerId,
                Character = playerCharacter,
                TalentId1 = talentArray[0].TalentID,
                TalentName1 = talentArray[0].TalentName,
                TimeSpanSelected1 = talentArray[0].TimeSpanSelected,
                TalentId4 = talentArray[1].TalentID,
                TalentName4 = talentArray[1].TalentName,
                TimeSpanSelected4 = talentArray[1].TimeSpanSelected,
                TalentId7 = talentArray[2].TalentID,
                TalentName7 = talentArray[2].TalentName,
                TimeSpanSelected7 = talentArray[2].TimeSpanSelected,
                TalentId10 = talentArray[3].TalentID,
                TalentName10 = talentArray[3].TalentName,
                TimeSpanSelected10 = talentArray[3].TimeSpanSelected,
                TalentId13 = talentArray[4].TalentID,
                TalentName13 = talentArray[4].TalentName,
                TimeSpanSelected13 = talentArray[4].TimeSpanSelected,
                TalentId16 = talentArray[5].TalentID,
                TalentName16 = talentArray[5].TalentName,
                TimeSpanSelected16 = talentArray[5].TimeSpanSelected,
                TalentId20 = talentArray[6].TalentID,
                TalentName20 = talentArray[6].TalentName,
                TimeSpanSelected20 = talentArray[6].TimeSpanSelected,
            };

            ReplaysDb.MatchPlayerTalent.CreateRecord(ReplaysContext, replayTalent);
        }

        private void AddMatchAwards(List<MatchAwardType> playerAwards, long playerId)
        {
            foreach (var award in playerAwards)
            {
                ReplayMatchAward matchAward = new ReplayMatchAward
                {
                    ReplayId = ReplayId,
                    PlayerId = playerId,
                    Award = award.ToString(),
                };
                ReplaysDb.MatchAward.CreateRecord(ReplaysContext, matchAward);
            }
        }

        private void AddPlayerHeroes(Dictionary<string, bool> playersHeroes, long playerId)
        {
            if (HeroesIcons != null)
            {
                ReplayAllHotsPlayerHero playersHero = new ReplayAllHotsPlayerHero();
                foreach (var hero in playersHeroes)
                {
                    if (HeroesIcons.Heroes().HeroExists(hero.Key, false))
                    {
                        playersHero.PlayerId = playerId;
                        playersHero.HeroName = hero.Key;
                        playersHero.IsUsable = hero.Value;
                        playersHero.LastUpdated = Replay.Timestamp;

                        if (ReplaysDb.HotsPlayerHero.IsExistingRecord(ReplaysContext, playersHero))
                            ReplaysDb.HotsPlayerHero.UpdateRecord(ReplaysContext, playersHero);
                        else
                            ReplaysDb.HotsPlayerHero.CreateRecord(ReplaysContext, playersHero);
                    }
                }
            }
        }

        private void MatchTeamBans()
        {
            if (Replay.GameMode == GameMode.UnrankedDraft || Replay.GameMode == GameMode.HeroLeague || Replay.GameMode == GameMode.TeamLeague || Replay.GameMode == GameMode.Custom)
            {
                if (Replay.TeamHeroBans != null)
                {
                    ReplayMatchTeamBan replayTeamBan = new ReplayMatchTeamBan
                    {
                        ReplayId = ReplayId,
                        Team0Ban0 = Replay.TeamHeroBans[0][0],
                        Team0Ban1 = Replay.TeamHeroBans[0][1],
                        Team1Ban0 = Replay.TeamHeroBans[1][0],
                        Team1Ban1 = Replay.TeamHeroBans[1][1],
                    };

                    if (replayTeamBan.Team0Ban0 != null || replayTeamBan.Team0Ban1 != null || replayTeamBan.Team1Ban0 != null || replayTeamBan.Team1Ban1 != null)
                        ReplaysDb.MatchTeamBan.CreateRecord(ReplaysContext, replayTeamBan);
                }
            }
        }

        private void MatchTeamLevels()
        {
            Dictionary<int, TimeSpan?> team0 = Replay.TeamLevels[0];
            Dictionary<int, TimeSpan?> team1 = Replay.TeamLevels[1];

            if (team0 != null || team1 != null)
            {
                int levelDiff = team0.Count - team1.Count;
                if (levelDiff > 0)
                {
                    for (int j = team1.Count + 1; j <= team0.Count; j++)
                    {
                        team1.Add(j, null);
                    }
                }
                else if (levelDiff < 0)
                {
                    for (int j = team0.Count + 1; j <= team1.Count; j++)
                    {
                        team0.Add(j, null);
                    }
                }

                for (int level = 1; level <= team0.Count; level++)
                {
                    ReplayMatchTeamLevel replayTeamLevel = new ReplayMatchTeamLevel
                    {
                        ReplayId = ReplayId,
                        TeamTime0 = team0[level],
                        Team0Level = team0[level].HasValue ? level : (int?)null,

                        TeamTime1 = team1[level],
                        Team1Level = team1[level].HasValue ? level : (int?)null,
                    };

                    ReplaysDb.MatchTeamLevel.CreateRecord(ReplaysContext, replayTeamLevel);
                }
            }
        }

        private void MatchTeamExperience()
        {
            var xpTeam0 = Replay.TeamPeriodicXPBreakdown[0];
            var xpTeam1 = Replay.TeamPeriodicXPBreakdown[1];

            if (xpTeam0 != null && xpTeam1 != null)
            {
                if (xpTeam0.Count != xpTeam1.Count)
                {
                    throw new QueryException("Teams don't have equal periodic xp gain.");
                }

                for (int j = 0; j < xpTeam0.Count; j++)
                {
                    var x = xpTeam0[j];
                    var y = xpTeam1[j];

                    ReplayMatchTeamExperience xp = new ReplayMatchTeamExperience
                    {
                        ReplayId = ReplayId,
                        Time = x.TimeSpan,

                        Team0TeamLevel = x.TeamLevel,
                        Team0CreepXP = x.CreepXP,
                        Team0HeroXP = x.HeroXP,
                        Team0MinionXP = x.MinionXP,
                        Team0StructureXP = x.StructureXP,
                        Team0TrickleXP = x.TrickleXP,

                        Team1TeamLevel = y.TeamLevel,
                        Team1CreepXP = y.CreepXP,
                        Team1HeroXP = y.HeroXP,
                        Team1MinionXP = y.MinionXP,
                        Team1StructureXP = y.StructureXP,
                        Team1TrickleXP = y.TrickleXP,
                    };

                    ReplaysDb.MatchTeamExperience.CreateRecord(ReplaysContext, xp);
                }
            }
        }

        private void MatchMessages()
        {
            foreach (var message in Replay.Messages)
            {
                var messageEventType = message.MessageEventType;
                var player = message.MessageSender;

                if (messageEventType == ReplayMessageEvents.MessageEventType.SChatMessage)
                {
                    var chatMessage = message.ChatMessage;

                    ReplayMatchMessage chat = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = chatMessage.Message,
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = chatMessage.MessageTarget.ToString(),
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp,
                    };

                    ReplaysDb.MatchMessage.CreateRecord(ReplaysContext, chat);
                }
                else if (messageEventType == ReplayMessageEvents.MessageEventType.SPingMessage)
                {
                    var pingMessage = message.PingMessage;

                    ReplayMatchMessage ping = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = "used a ping",
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = pingMessage.MessageTarget.ToString(),
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp,
                    };

                    ReplaysDb.MatchMessage.CreateRecord(ReplaysContext, ping);
                }
                else if (messageEventType == ReplayMessageEvents.MessageEventType.SPlayerAnnounceMessage)
                {
                    var announceMessage = message.PlayerAnnounceMessage;

                    ReplayMatchMessage announce = new ReplayMatchMessage
                    {
                        ReplayId = ReplayId,
                        CharacterName = player != null ? player.Character : string.Empty,
                        Message = $"announce {announceMessage.AnnouncementType.ToString()}",
                        MessageEventType = messageEventType.ToString(),
                        MessageTarget = "Allies",
                        PlayerName = player != null ? player.Name : string.Empty,
                        TimeStamp = message.Timestamp,
                    };

                    ReplaysDb.MatchMessage.CreateRecord(ReplaysContext, announce);
                }
            }
        }

        private void MatchObjectives()
        {
            var objTeam0 = Replay.TeamObjectives[0];
            var objTeam1 = Replay.TeamObjectives[1];

            if (objTeam0 != null && objTeam0.Count > 0)
            {
                foreach (var objective in objTeam0)
                {
                    var player = objective.Player;

                    ReplayMatchTeamObjective obj = new ReplayMatchTeamObjective
                    {
                        Team = 0,
                        PlayerId = player != null ? ReplaysDb.HotsPlayer.ReadPlayerIdFromBattleNetId(ReplaysContext, player.BattleNetId, player.BattleNetRegionId, player.BattleNetSubId) : (long?)null,
                        ReplayId = ReplayId,
                        TeamObjectiveType = objective.TeamObjectiveType.ToString(),
                        TimeStamp = objective.TimeSpan,
                        Value = objective.Value,
                    };

                    ReplaysDb.MatchTeamObjective.CreateRecord(ReplaysContext, obj);
                }
            }

            if (objTeam1 != null && objTeam1.Count > 0)
            {
                foreach (var objective in objTeam1)
                {
                    var player = objective.Player;

                    ReplayMatchTeamObjective obj = new ReplayMatchTeamObjective
                    {
                        Team = 1,
                        PlayerId = player != null ? ReplaysDb.HotsPlayer.ReadPlayerIdFromBattleNetId(ReplaysContext, player.BattleNetId, player.BattleNetRegionId, player.BattleNetSubId) : (long?)null,
                        ReplayId = ReplayId,
                        TeamObjectiveType = objective.TeamObjectiveType.ToString(),
                        TimeStamp = objective.TimeSpan,
                        Value = objective.Value,
                    };

                    ReplaysDb.MatchTeamObjective.CreateRecord(ReplaysContext, obj);
                }
            }
        }

        private Player[] GetPlayers()
        {
            if (Replay.ReplayBuild > 39445)
                return Replay.ClientListByUserID;
            else
                return Replay.Players;
        }

        private string RetrieveAllMapAndHeroNames()
        {
            List<string> names = new List<string>();

            if (HeroesIcons.MapBackgrounds().MapNameTranslation(Replay.Map, out string mapName))
                names.Add($"{Replay.Map}: {mapName} [Good]");
            else if (AutoTranslateMapNameByMapObjectives(out mapName))
                names.Add($"{Replay.Map}: {mapName} [Auto]");
            else
                names.Add($"{Replay.Map}: ??? [Unknown]");

            foreach (var player in GetPlayers())
            {
                if (player == null)
                    continue;

                if (player.Character == null)
                {
                    names.Add($"No character");
                    continue;
                }

                if (HeroesIcons.Heroes().HeroNameTranslation(player.Character, out string character))
                    names.Add($"{player.Character}: {character} [Good]");
                else if (AutoTranslateHeroNameByTalent(player.Talents, out character))
                    names.Add($"{player.Character}: {character} [Auto]");
                else
                    names.Add($"{player.Character}: ??? [Unknown]");
            }

            string output = "Unable to translate some or all of the following names";
            output += Environment.NewLine;

            foreach (var name in names)
            {
                output += name;
                output += Environment.NewLine;
            }

            output += "================================";
            output += Environment.NewLine;
            return output;
        }

        /// <summary>
        /// Attempt to auto translate Hero name using talents, returns true if succeeded
        /// </summary>
        /// <param name="talents"></param>
        /// <param name="character">Translated hero name</param>
        /// <returns></returns>
        private bool AutoTranslateHeroNameByTalent(Talent[] talents, out string character)
        {
            int talentCount = talents.Count();
            character = string.Empty;

            while (talentCount > 0)
            {
                if (HeroesIcons.HeroBuilds().GetHeroNameFromTalentReferenceName(talents[talentCount - 1].TalentName, out character))
                    return true;
                else
                    talentCount--;
            }

            return false;
        }

        /// <summary>
        /// Attempt to auto translate the Map name using the objectives, returns true if succeeded
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        private bool AutoTranslateMapNameByMapObjectives(out string mapName)
        {
            mapName = string.Empty;

            var objTeam0 = Replay.TeamObjectives[0].Select(x => x.TeamObjectiveType.ToString().ToUpperInvariant()).ToList();
            var objTeam1 = Replay.TeamObjectives[1].Select(x => x.TeamObjectiveType.ToString().ToUpperInvariant()).ToList();

            var mapsList = HeroesIcons.MapBackgrounds().GetMapsList();
            var mapsListStripped = new List<string>();

            // remove all the spaces from the map names
            foreach (var map in mapsList)
            {
                mapsListStripped.Add(new string(map.Where(x => !char.IsWhiteSpace(x) && !char.IsPunctuation(x)).ToArray()).ToUpperInvariant());
            }

            // known list of objectives that do not contain a map name
            var nonMapObjectives = new List<string>
            {
                "FirstCatapultSpawn",
                "BossCampCaptureWithCampID",
            };

            if (objTeam0 == null && objTeam1 == null)
                return false;

            objTeam0.AddRange(objTeam1); // add all to first
            objTeam0 = objTeam0.Distinct().ToList(); // remove duplicates

            if (objTeam0 != null && objTeam0.Count > 0)
            {
                foreach (var objective in objTeam0)
                {
                    if (nonMapObjectives.Contains(objective))
                        continue;

                    for (int i = 0; i < mapsListStripped.Count; i++)
                    {
                        // find the map
                        if (objective.Contains(mapsListStripped[i]))
                        {
                            mapName = mapsList[i];
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
