﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using BotCatMaxy.Settings;
using BotCatMaxy;
//using Discord.Addons.Preconditions;

namespace BotCatMaxy {
    [Serializable]
    public struct Infraction {
        public DateTime time;
        public string reason;
        public float size;
    }

    public static class ModerationFunctions {
        public static void CheckDirectories(IGuild guild) {
            //old directory was fC:/Users/Daniel/Google-Drive/Botcatmaxy/Data/
            if (!Directory.Exists("/home/bob_the_daniel/Data/" + guild.OwnerId)) {
                Directory.CreateDirectory("/home/bob_the_daniel/Data/" + guild.OwnerId);
            }
            if (!Directory.Exists("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/")) {
                Directory.CreateDirectory("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/");
            }
            if (!Directory.Exists("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/Discord/")) {
                Directory.CreateDirectory("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/Discord/");
            }
            if (!Directory.Exists("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/Games/")) {
                Directory.CreateDirectory("/home/bob_the_daniel/Data/" + guild.OwnerId + "/Infractions/Games/");
            }
        }

        public static void SaveInfractions(string location, List<Infraction> infractions) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create("/home/bob_the_daniel/Data/" + location);
            bf.Serialize(file, infractions.ToArray());
            file.Close();
        }

        public static List<Infraction> LoadInfractions(string location) {
            List<Infraction> infractions = new List<Infraction>();

            if (File.Exists("/home/bob_the_daniel/Data/" + location)) {

                BinaryFormatter newbf = new BinaryFormatter();
                FileStream newFile = File.Open("/home/bob_the_daniel/Data/" + location, FileMode.Open);
                Infraction[] oldInfractions;
                oldInfractions = (Infraction[])newbf.Deserialize(newFile);
                newFile.Close();
                foreach (Infraction infraction in oldInfractions) {
                    infractions.Add(infraction);
                }
            }
            return infractions;
        }

        public static void WarnUser(SocketUser user, float size, string reason, string location) {
            List<Infraction> infractions = LoadInfractions(location);

            Infraction newInfraction = new Infraction {
                reason = reason,
                time = DateTime.Now,
                size = size
            };
            infractions.Add(newInfraction);

            SaveInfractions(location, infractions);
        }

        public static Embed CheckInfractions(SocketUser user, string location) {
            List<Infraction> infractions = LoadInfractions(location);

            string infractionList = "";
            float infractionsToday = 0;
            float infractions30Days = 0;
            float totalInfractions = 0;
            float last7Days = 0;
            string plural = "";
            for (int i = 0; i < infractions.Count; i++) {
                if (i != 0) { //Creates new line if it's not the first infraction
                    infractionList += "\n";
                }
                Infraction infraction = infractions[i];

                //Gets how long ago all the infractions were
                int daysAgo = (DateTime.Now.Date - infraction.time.Date).Days;
                string timeAgo;
                totalInfractions += infraction.size;
                timeAgo = DateTime.Now.Month - infraction.time.Month + " months ago";
                
                if (daysAgo <= 7) {
                    last7Days += infraction.size;
                }
                if (daysAgo <= 30) {
                    if (DateTime.Now.Day - infraction.time.Day == 1) {
                        plural = "";
                    } else {
                        plural = "s";
                    }
                    infractions30Days += infraction.size;
                    timeAgo = DateTime.Now.Day - infraction.time.Day + " day" + plural + " ago";
                    if (infraction.time.Date == DateTime.Now.Date) {
                        infractionsToday += infraction.size;
                        if (DateTime.Now.Hour - infraction.time.Hour == 1) {
                            plural = "";
                        } else {
                            plural = "s";
                        }
                        timeAgo = DateTime.Now.Hour - infraction.time.Hour + " hour" + plural + " ago";
                        if (infraction.time.Hour == DateTime.Now.Hour) {
                            if (DateTime.Now.Minute - infraction.time.Minute == 1) {
                                plural = "";
                            } else {
                                plural = "s";
                            }
                            timeAgo = DateTime.Now.Minute - infraction.time.Minute + " minute" + plural + " ago";
                            if (infraction.time.Minute == DateTime.Now.Minute) {
                                if (DateTime.Now.Second - infraction.time.Second == 1) {
                                    plural = "";
                                } else {
                                    plural = "s";
                                }
                                timeAgo = DateTime.Now.Second - infraction.time.Second + " second" + plural + " ago";
                            }
                        }
                    }
                }

                string size = "";
                if (infraction.size != 1) {
                    size = "["  + infraction.size  + "x] ";
                }

                infractionList += size + infraction.reason + " - " + timeAgo;
            }

            if (infractions.Count > 1) {
                plural = "s";
            } else {
                plural = "";
            }

            //Builds infraction embed
            var embed = new EmbedBuilder();
            embed.AddField("Today",
                infractionsToday, true);
            embed.AddField("Last 7 days",
                last7Days, true);
            embed.AddField("Last 30 days",
                infractions30Days, true);
            embed.AddField("Warning" + plural + " (total " + totalInfractions + " sum of size & " + infractions.Count + " individual)",
                infractionList)
                .WithAuthor(user)
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            return embed.Build();
        }
    }

    [Group("games")]
    [Alias("game")]
    [RequireContext(ContextType.Guild)]
    public class GameWarnModule : ModuleBase<SocketCommandContext> {
        [Command("warn")]
        public async Task WarnUserAsync(SocketUser user, float size, [Remainder] string reason) {
            if (!((SocketGuildUser)Context.User).CanWarn()) {
                await ReplyAsync("You do not have permission to use this command");
                return;
            }
            if (size > 999 || size < 0.01) {
                await ReplyAsync("Why would you need to warn someone with that size?");
                return;
            }

            ModerationFunctions.CheckDirectories(Context.Guild);
            ModerationFunctions.WarnUser(user, size, reason, Context.Guild.OwnerId + "/Infractions/Games/" + user.Id);

            await ReplyAsync(user.Username + " has been warned for " + reason);
        }

        [Command("warn")]
        public async Task WarnUserSmallSizeAsync(SocketUser user, [Remainder] string reason) {
            if (!((SocketGuildUser)Context.User).CanWarn()) {
                await ReplyAsync("You do not have permission to use this command");
                return;
            }
            ModerationFunctions.CheckDirectories(Context.Guild);
            ModerationFunctions.WarnUser(user, 1, reason, Context.Guild.OwnerId + "/Infractions/Games/" + user.Id);

            await ReplyAsync(user.Username + " has been warned for " + reason);
        }

        [Command("warns")]
        [Alias("infractions", "warnings")]
        public async Task CheckUserWarnsAsync(SocketUser user = null) {
            if (user == null) {
                user = Context.Message.Author;
            }

            ModerationFunctions.CheckDirectories(Context.Guild);
            if (File.Exists("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Games/" + user.Id)) {
                await ReplyAsync(embed: ModerationFunctions.CheckInfractions(user, Context.Guild.OwnerId + "/Infractions/Games/" + user.Id));
            } else {
                await ReplyAsync(user.Username + " has no warns");
            }
        }

        [Command("removewarn")]
        [Alias("warnremove", "removewarning")]
        public async Task RemooveWarnAsync(SocketUser user, int index) {
            if (!((SocketGuildUser)Context.User).HasAdmin()) {
                await ReplyAsync("You do have administrator permissions");
                return;
            }
            ModerationFunctions.CheckDirectories(Context.Guild);
            if (File.Exists("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Games/" + user.Id)) {
                List<Infraction> infractions = ModerationFunctions.LoadInfractions(Context.Guild.OwnerId + "/Infractions/Games/" + user.Id);

                if (infractions.Count < index || index <= 0) {
                    await ReplyAsync("invalid infraction number");
                } else if (infractions.Count == 1) {
                    await ReplyAsync("removed " + user.Username + "'s warning for " + infractions[0]);
                    File.Delete("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Games/" + user.Id);
                } else {
                    string reason = infractions[index - 1].reason;
                    infractions.RemoveAt(index - 1);

                    ModerationFunctions.SaveInfractions(Context.Guild.OwnerId + "/Infractions/Games/" + user.Id, infractions);

                    await ReplyAsync("removed " + user.Mention + "'s warning for " + reason);
                }
            } else {
                await ReplyAsync(user.Username + " has no warns");
            }
        }
    }

    [RequireContext(ContextType.Guild)]
    public class ModerationCommands : ModuleBase<SocketCommandContext> {
        [Command("moderationInfo")]
        public async Task ModerationInfo() {
            ModerationSettings settings = SettingFunctions.LoadModSettings(Context.Guild, false);
            if (settings == null) {
                _ = ReplyAsync("Moderation settings are null");
                return;
            }

            var embed = new EmbedBuilder();
            string rolesAbleToWarn = "";
            foreach (SocketRole role in Context.Guild.Roles) {
                if (role.Permissions.KickMembers && !role.IsManaged) {
                    if (rolesAbleToWarn != "") {
                        rolesAbleToWarn += "\n";
                    }
                    if (role.IsMentionable) {
                        rolesAbleToWarn += role.Mention;
                    } else {
                        rolesAbleToWarn += role.Name;
                    }
                }
            }
            if (settings.ableToWarn != null && settings.ableToWarn.Count > 0) {
                foreach (ulong roleID in settings.ableToWarn) {
                    SocketRole role = Context.Guild.GetRole(roleID);
                    if (role != null) {
                        if (rolesAbleToWarn != "") {
                            rolesAbleToWarn += "\n";
                        }
                        if (role.IsMentionable) {
                            rolesAbleToWarn += role.Mention;
                        } else {
                            rolesAbleToWarn += role.Name;
                        }
                    } else {
                        settings.ableToWarn.Remove(roleID);
                    }
                }
            }
            embed.AddField("Roles that can warn", rolesAbleToWarn, true);
            embed.AddField("Will invites lead to warn", !settings.invitesAllowed, true);
            await ReplyAsync(embed: embed.Build());
        }
    }

    [Group("discord")]
    [Alias("general", "chat", "")]
    [RequireContext(ContextType.Guild)]
    public class DiscordWarnModule : ModuleBase<SocketCommandContext> {
        [RequireUserPermission(GuildPermission.BanMembers)]
        [Command("warn")]
        public async Task WarnUserAsync(SocketUser user, float size, [Remainder] string reason) {
            ModerationFunctions.CheckDirectories(Context.Guild);
            ModerationFunctions.WarnUser(user, size, reason, Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id);

            await ReplyAsync(user.Username + " has been warned for " + reason);
        }

        [RequireUserPermission(GuildPermission.BanMembers)]
        [Command("warn")]
        public async Task WarnUserSmallSizeAsync(SocketUser user, [Remainder] string reason) {
            ModerationFunctions.CheckDirectories(Context.Guild);
            ModerationFunctions.WarnUser(user, 1, reason, Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id);

            await ReplyAsync(user.Username + " has been warned for " + reason);
        }

        [Command("warns")]
        [Alias("infractions", "warnings")]
        public async Task CheckUserWarnsAsync(SocketUser user = null) {
            ModerationFunctions.CheckDirectories(Context.Guild);
            if (user == null) {
                user = Context.Message.Author;
            }
            if (File.Exists("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id)) {
                await ReplyAsync(embed: ModerationFunctions.CheckInfractions(user, Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id));
            } else {
                await ReplyAsync(user.Username + " has no warns");
            }
        }

        [RequireUserPermission(GuildPermission.BanMembers)]
        [Command("removewarn")]
        [Alias("warnremove", "removewarning")]
        public async Task RemooveWarnAsync(SocketUser user, int index) {
            ModerationFunctions.CheckDirectories(Context.Guild);
            if (File.Exists("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id)) {
                List<Infraction> infractions = ModerationFunctions.LoadInfractions(Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id);

                if (infractions.Count < index || index <= 0) {
                    await ReplyAsync("invalid infraction number");
                } else if (infractions.Count == 1) {
                    await ReplyAsync("removed " + user.Username + "'s warning for " + infractions[index - 1].reason);
                    File.Delete("/home/bob_the_daniel/Data/" + Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id);
                } else {
                    string reason = infractions[index - 1].reason;
                    infractions.RemoveAt(index - 1);

                    ModerationFunctions.SaveInfractions(Context.Guild.OwnerId + "/Infractions/Discord/" + user.Id, infractions);

                    await ReplyAsync("removed " + user.Mention + "'s warning for " + reason);
                }
            } else {
                await ReplyAsync(user.Username + " has no warns");
            }
        }
    }

    public static class SwearFilter {
        public static async Task CheckMessage(SocketMessage message) {
            var chnl = message.Channel as SocketGuildChannel;
            var Guild = chnl.Guild;
            if (Guild != null && Directory.Exists("/home/bob_the_daniel/Data/" + Guild.OwnerId) && !Utilities.HasAdmin(message.Author as SocketGuildUser)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                List<BadWord> badWords;

                using (StreamReader sr = new StreamReader(@"/home/bob_the_daniel/Data/" + Guild.OwnerId + "/badwords.json"))
                using (JsonTextReader reader = new JsonTextReader(sr)) {
                    badWords = serializer.Deserialize<List<BadWord>>(reader);
                }

                if (message.Content.Contains("discord.gg/")) {
                    if (!SettingFunctions.LoadModSettings(Guild).invitesAllowed) {
                        ModerationFunctions.WarnUser(message.Author, 0.5f, "Posted Invite", Guild.OwnerId + "/Infractions/Discord/" + message.Author.Id);
                        await message.Channel.SendMessageAsync("warned " + message.Author.Mention + " for posting a discord invite");

                        Logging.LogDeleted("Bad word removed", message, Guild);
                        await message.DeleteAsync();
                        return;
                    }
                }

                //Guild.OwnerId
                if (File.Exists("/home/bob_the_daniel/Data/" + Guild.OwnerId + "/badwords.json")) {
                    foreach (BadWord badWord in badWords) {
                        if (message.Content.Contains(badWord.word)) {
                            ModerationFunctions.WarnUser(message.Author, 0.5f, "Bad word", Guild.OwnerId + "/Infractions/Discord/" + message.Author.Id);
                            await message.Channel.SendMessageAsync("warned " + message.Author.Mention + " for bad word");
                            
                            Logging.LogDeleted("Bad word removed", message, Guild);
                            await message.DeleteAsync();
                            return;
                        }
                    }
                }
            }
        }
    }
}