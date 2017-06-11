﻿using HeroesMatchTracker.Data.Databases;
using HeroesMatchTracker.Data.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroesMatchTracker.Data.Queries.Settings
{
    public class UserProfiles
    {
        public void CreateUserProfile(UserProfile profile)
        {
            using (var db = new SettingsContext())
            {
                db.UserProfiles.Add(profile);
                db.SaveChanges();
            }
        }

        public bool IsExistingUserProfile(UserProfile profile)
        {
            using (var db = new SettingsContext())
            {
                var replay = db.UserProfiles.FirstOrDefault(x => x.UserBattleTagName == profile.UserBattleTagName && x.UserRegion == profile.UserRegion);
                if (replay != null)
                    return true;
                else
                    return false;
            }
        }

        public void DeleteUserProfile(int userProfileId)
        {
            using (var db = new SettingsContext())
            {
                var profile = db.UserProfiles.FirstOrDefault(x => x.UserProfileId == userProfileId);
                if (profile != null)
                {
                    db.UserProfiles.Remove(profile);
                    db.SaveChanges();
                }
            }
        }
    }
}
