using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sevenisko.IceBot.Modules
{
    [Name("Audio")]
    [Summary("Audio module to interact with voice chat. Currently, used to playback audio in a stream.")]
    public class AudioModule : ModuleBase<SocketCommandContext>
    {
        private int NumPlaysCalled;

        private int DelayActionLength = 10000;

        private bool CanDelayAction;

        private bool AutoDownload = true;

        private bool AutoStop;

        private Timer VoiceChannelTimer;

        private bool LeaveWhenEmpty = true;

        private async Task DelayAction(Action f)
        {
            CanDelayAction = true;
            f();
            await Task.Delay(DelayActionLength);
            CanDelayAction = false;
        }

        public bool GetDelayAction()
        {
            if (CanDelayAction)
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "This action is delayed. Please try again later.");
                ReplyAsync("Please wait, i'm not an runner.");
            }
            return CanDelayAction;
        }

        public async Task JoinAudioAsync(IGuild guild, IVoiceChannel target)
        {
            if (guild != null && target != null)
            {
                if (CanDelayAction)
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "The client is currently disconnecting from a voice channel. Please try again later.");
                    return;
                }
                if (Shared.ConnectedChannels.TryGetValue(guild.Id, out IAudioClient gogod))
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "The client is already connected to the current voice channel.");
                    await ReplyAsync("I'm already connected to the current voice channel.");
                    return;
                }
                if (target.Guild.Id != guild.Id)
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Are you sure the current voice channel is correct?");
                    return;
                }
                gogod = await target.ConnectAsync();
                try
                {
                    if(gogod == null)
                    {
                        await Program.LogText(LogSeverity.Warning, "AudioModule", "ConnectAsync returned null.");
                        return;
                    }
                    if (Shared.ConnectedChannels.TryAdd(guild.Id, gogod))
                    {
                        await Program.LogText(LogSeverity.Warning, "AudioModule", "The client is now connected to the current voice channel.");
                        if (LeaveWhenEmpty)
                        {
                            VoiceChannelTimer = new Timer(CheckVoiceChannelState, target, TimeSpan.FromMinutes(15.0), TimeSpan.FromMinutes(15.0));
                        }
                        return;
                    }
                }
                catch
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "The client failed to connect to the target voice channel.");
                }
                await Program.LogText(LogSeverity.Warning, "AudioModule", "Unable to join the current voice channel.");
                await ReplyAsync("I can't join the current voice channel.");
            }
        }

        public async Task LeaveAudioAsync(IGuild guild)
        {
            if (guild != null)
            {
                if (Shared.AudioPlayer.IsRunning())
                {
                    StopAudio();
                }
                while (Shared.AudioPlayer.IsRunning())
                {
                    await Task.Delay(1000);
                }
                if (Shared.ConnectedChannels.TryRemove(guild.Id, out IAudioClient audioClient))
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "The client is now disconnected from the current voice channel.");
                    await DelayAction(delegate
                    {
                        audioClient.StopAsync();
                    });
                }
                else
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Unable to disconnect from the current voice channel (not connected).");
                    await ReplyAsync("I'm not connected to the voice channel.");
                }
            }
        }

        private async void CheckVoiceChannelState(object state)
        {
            IVoiceChannel voiceChannel;
            IVoiceChannel channel = voiceChannel = (state as IVoiceChannel);
            if (voiceChannel != null && (await channel.GetUsersAsync().FlattenAsync()).Count() < 2)
            {
                await LeaveAudioAsync(channel.Guild);
                if (VoiceChannelTimer != null)
                {
                    VoiceChannelTimer.Dispose();
                    VoiceChannelTimer = null;
                }
            }
        }

        public int GetNumPlaysCalled()
        {
            return NumPlaysCalled;
        }

        public async Task ForcePlayAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            if (guild == null)
            {
                return;
            }

            AudioFile song = await GetAudioFileAsync(path);
            if (song != null)
            {
                Interlocked.Increment(ref NumPlaysCalled);
                if (Shared.AudioPlayer.IsRunning())
                {
                    StopAudio();
                }
                while (Shared.AudioPlayer.IsRunning())
                {
                    await Task.Delay(1000);
                }
                if (Shared.ConnectedChannels.TryGetValue(guild.Id, out IAudioClient value))
                {
                    string thumbURL = "";
                    string title = song.Title;
                    foreach (RadioStation station in Program.config.Stations)
                    {
                        if(song.Title == station.Filename)
                        {
                            title = station.Name;
                            thumbURL = station.Thumbnail;
                        }
                    }
                    
                    if (title != "")
                    {
                        EmbedBuilder emb = new EmbedBuilder();
                        emb.WithAuthor(Context.Client.CurrentUser);
                        emb.WithColor(Color.Red);
                        
                        if (path.Contains("soundcloud.com") && !Program.config.SCSettings.Enabled)
                        {
                            await ReplyAsync("Soundcloud playback is disabled by admin.", false);
                            return;
                        }
                        else if ((path.Contains("youtube.com") || path.Contains("youtu.be")) && !Program.config.YTSettings.Enabled)
                        {
                            await ReplyAsync("YouTube playback is disabled by admin.", false);
                            return;
                        }
                        else
                        {
                            if (path.Contains("soundcloud.com"))
                            {
                                emb.WithThumbnailUrl(SCDownloader.GetTrack(path)["artwork_url"].ToString().Replace("large", "t500x500"));
                                emb.WithTitle("Now Playing:");
                                emb.AddField(SCDownloader.GetTrack(path)["user"]["username"].ToString(), SCDownloader.GetTrack(path)["title"].ToString());
                            }
                            else if (path.Contains("youtube.com"))
                            {
                                string[] url = path.Split(new string[] { "watch?v=" }, StringSplitOptions.None);
                                title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString();
                                thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from YouTube:", title);
                            }
                            else if (path.Contains("youtu.be"))
                            {
                                string[] url = path.Split(new string[] { "youtu.be/" }, StringSplitOptions.None);
                                title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString();
                                thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from YouTube:", title);
                            }
                            else if (path.Contains("play.cz") && path.Contains("icecast"))
                            {
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from PLAY.cz:", title);
                            }
                            else
                            {
                                emb.AddField("Now Playing:", title);
                            }

                            await ReplyAsync(null, false, emb.Build());
                            await Program.LogText(LogSeverity.Info, "AudioModule", "Now Playing: " + title + "");
                            await Context.Client.SetGameAsync(title, null, ActivityType.Listening);
                            await Shared.AudioPlayer.Play(value, song);
                        }
                    }
                    else if (path.Contains("spotify.com"))
                    {
                        await ReplyAsync("I cannot play songs from Spotify, because they have an really hard API for me. 😦", false);
                        return;
                    }
                    else
                    {
                        await ReplyAsync("Media not found on this link.");
                        await Program.LogText(LogSeverity.Warning, "AudioModule", "Cannot play media from link!");
                    }
                    await Context.Client.SetGameAsync("Just chilling");
                }
                else
                {
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Unable to play in the proper channel. Make sure the audio client is connected.");
                    await ReplyAsync("I'm not connected to the voice channel.");
                }
                Interlocked.Decrement(ref NumPlaysCalled);
            }
        }

        public async Task AutoPlayAudioAsync(IGuild guild, IMessageChannel channel)
        {
            if (guild == null || Shared.AutoPlayRunning)
            {
                return;
            }
            do
            {
                bool autoPlay = Shared.AutoPlay;
                bool flag = autoPlay;
                Shared.AutoPlayRunning = autoPlay;
                if (!flag)
                {
                    break;
                }
                if (Shared.AudioPlayer.IsRunning())
                {
                    await Task.Delay(1000);
                }
                if (Shared.Playlist.IsEmpty || !Shared.AutoPlayRunning || !Shared.AutoPlay)
                {
                    break;
                }
                if (Shared.ConnectedChannels.TryGetValue(guild.Id, out IAudioClient value))
                {
                    AudioFile audioFile = PlaylistNext();
                    if (audioFile != null)
                    {
                        string thumbURL = "";
                        string title = audioFile.Title;
                        foreach (RadioStation station in Program.config.Stations)
                        {
                            if (audioFile.Title == station.Filename)
                            {
                                title = station.Name;
                                thumbURL = station.Thumbnail;
                            }
                        }
                        if (title != "")
                        {
                            EmbedBuilder emb = new EmbedBuilder();
                            emb.WithAuthor(Context.Client.CurrentUser);
                            emb.WithColor(Color.Red);
                            if (audioFile.Link.Contains("soundcloud.com"))
                            {
                                emb.WithThumbnailUrl(SCDownloader.GetTrack(audioFile.Link)["artwork_url"].ToString().Replace("large", "t500x500"));
                                emb.WithTitle("Now Playing:");
                                emb.AddField(SCDownloader.GetTrack(audioFile.Link)["user"]["username"].ToString(), SCDownloader.GetTrack(audioFile.Link)["title"].ToString());
                            }
                            else if (audioFile.Link.Contains("youtube.com"))
                            {
                                string[] url = audioFile.Link.Split(new string[] { "watch?v=" }, StringSplitOptions.None);
                                title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString().Replace("â€”", "-");
                                thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from YouTube:", title);
                            }
                            else if (audioFile.Link.Contains("youtu.be"))
                            {
                                string[] url = audioFile.Link.Split(new string[] { "youtu.be/" }, StringSplitOptions.None);
                                title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString().Replace("â€”", "-");
                                thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from YouTube:", title);
                            }
                            else if (audioFile.Link.Contains("play.cz") && audioFile.Link.Contains("icecast"))
                            {
                                emb.WithThumbnailUrl(thumbURL);
                                emb.AddField("Now Playing from PLAY.cz:", Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(title)));
                            }
                            else
                            {
                                emb.AddField("Now Playing:", title);
                            }

                            await ReplyAsync(null, false, emb.Build());
                            await Program.LogText(LogSeverity.Info, "AudioModule", "Now Playing: " + title + "");
                            await Context.Client.SetGameAsync(title, null, ActivityType.Listening);
                            await Shared.AudioPlayer.Play(value, audioFile);
                        }
                        else
                        {
                            await ReplyAsync("Sorry, but i cannot play this link.");
                            await Program.LogText(LogSeverity.Warning, "AudioModule", "Cannot play media from link!");
                        }
                        await Context.Client.SetGameAsync("Just chilling");
                    }
                    else
                    {
                        await Program.LogText(LogSeverity.Warning, "AudioModule", $"Cannot play the audio source specified: {audioFile}");
                    }
                    continue;
                }
                await Program.LogText(LogSeverity.Warning, "AudioModule", "Unable to play in the proper channel. Make sure the audio client is connected.");
                break;
            }
            while (!Shared.Playlist.IsEmpty && Shared.AutoPlayRunning && Shared.AutoPlay);
            if (AutoStop)
            {
                Shared.AutoPlay = false;
            }
            Shared.AutoPlayRunning = false;
        }

        public bool IsAudioPlaying()
        {
            return Shared.AudioPlayer.IsPlaying();
        }

        public void PauseAudio()
        {
            Shared.AudioPlayer.Pause();
        }

        public void ResumeAudio()
        {
            Shared.AudioPlayer.Resume();
        }

        public void StopAudio()
        {
            Shared.AutoPlay = false;
            Shared.AutoPlayRunning = false;
            Shared.AudioPlayer.Stop();
            // DiscordReply("You don't like my music, i'm really sad!\nThat's an discrimination of bots. <:foreveralone:545662572972736544>");
        }

        public void AdjustVolume(float volume)
        {
            Shared.AudioPlayer.AdjustVolume(volume);
        }

        public void SetAutoPlay(bool enable)
        {
            Shared.AutoPlay = enable;
        }

        public bool GetAutoPlay()
        {
            return Shared.AutoPlay;
        }

        public async Task CheckAutoPlayAsync(IGuild guild, IMessageChannel channel)
        {
            if (Shared.AutoPlay && !Shared.AutoPlayRunning && !Shared.AudioPlayer.IsRunning())
            {
                await AutoPlayAudioAsync(guild, channel);
            }
        }

        public void PrintPlaylist()
        {
            int count = Shared.Playlist.Count;
            if (count == 0)
            {
                ReplyAsync("There are currently no items in the playlist.");
                return;
            }
            int num = (int)Math.Floor(Math.Log10(count) + 1.0);
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Gold);
            embedBuilder.WithAuthor(Context.Client.CurrentUser);
            embedBuilder.WithTitle("Current playlist:");
            for (int i = 0; i < count; i++)
            {
                string text = "";
                for (int j = (i == 0) ? 1 : ((int)Math.Floor(Math.Log10(i) + 1.0)); j < num; j++)
                {
                    text += "0";
                }
                AudioFile value = Shared.Playlist.ElementAt(i);
                
                embedBuilder.AddField(text + i, value);
            }
            ReplyAsync(null, false, embedBuilder.Build());
        }

        public async Task PlaylistAddAsync(string path)
        {
            AudioFile audioFile = await GetAudioFileAsync(path);
            if (audioFile == null)
            {
                return;
            }

            bool isStream = false;
            string thumbURL = "";
            string title = audioFile.Title;
            EmbedBuilder emb = new EmbedBuilder();
            foreach (RadioStation station in Program.config.Stations)
            {
                if (audioFile.Title == station.Filename)
                {
                    title = station.Name;
                    thumbURL = station.Thumbnail;
                }
            }
            if (title != "")
            {
                emb.WithAuthor(Context.Client.CurrentUser);
                emb.WithColor(Color.Red);
                if (path.Contains("soundcloud.com"))
                {
                    emb.WithThumbnailUrl(SCDownloader.GetTrack(path)["artwork_url"].ToString().Replace("large", "t500x500"));
                    emb.WithTitle("Added to playlist:");
                    emb.AddField(SCDownloader.GetTrack(path)["user"]["username"].ToString(), SCDownloader.GetTrack(path)["title"].ToString());
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Added to playlist: " + title + "");
                }
                else if (path.Contains("youtube.com"))
                {
                    string[] url = path.Split(new string[] { "watch?v=" }, StringSplitOptions.None);
                    title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString().Replace("â€”", "-");
                    thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                    emb.WithThumbnailUrl(thumbURL);
                    emb.AddField("Added to playlist from YouTube:", title);
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Added to playlist: " + title + "");
                }
                else if (path.Contains("youtu.be"))
                {
                    string[] url = path.Split(new string[] { "youtu.be/" }, StringSplitOptions.None);
                    title = SCDownloader.GetYouTubeInfo(url[1])["items"][0]["snippet"]["title"].ToString().Replace("â€”", "-");
                    thumbURL = "https://i.ytimg.com/vi/" + url[1] + "/maxresdefault.jpg";
                    emb.WithThumbnailUrl(thumbURL);
                    emb.AddField("Added to playlist from YouTube:", title);
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Added to playlist: " + title + "");
                }
                else if (path.Contains("play.cz") && path.Contains("icecast"))
                {
                    emb.WithThumbnailUrl(thumbURL);
                    isStream = true;
                    emb.AddField("Error", "Cannot add audio stream to playlist!");
                    await Program.LogText(LogSeverity.Warning, "AudioModule", "Cannot add audio streams to the playlist!");
                }
                else
                {
                    emb.AddField("Added to playlist:", title);
                    await Program.LogText(LogSeverity.Info, "AudioModule", "Added to playlist: " + title + "");
                }
                audioFile.Link = path;
            }

            if(isStream)
            {
                Shared.Playlist.Enqueue(audioFile);
            }
            await ReplyAsync(null, false, emb.Build());
            if (AutoDownload && !isStream)
            {
                if (audioFile.IsNetwork)
                {
                    Shared.AudioDownloader.Push(audioFile);
                }
                await Shared.AudioDownloader.StartDownloadAsync();
            }
        }

        private AudioFile PlaylistNext()
        {
            if (Shared.Playlist.TryDequeue(out AudioFile result))
            {
                return result;
            }
            if (Shared.Playlist.Count <= 0)
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "We reached the end of the playlist.");
            }
            else
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "The next song could not be opened.");
            }
            return result;
        }

        public void PlaylistSkip()
        {
            if (!Shared.AutoPlay)
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "Autoplay service hasn't been started.");
            }
            else if (!Shared.AudioPlayer.IsRunning())
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "There's no audio currently playing.");
            }
            else
            {
                Shared.AudioPlayer.Stop();
            }
        }

        private async Task<AudioFile> GetAudioFileAsync(string path)
        {
            try
            {
                AudioFile audioFile = await Shared.AudioDownloader.GetAudioFileInfo(path);
                if (audioFile != null)
                {
                    string item = Shared.AudioDownloader.GetItem(audioFile.Title);
                    if (item != null)
                    {
                        audioFile.FileName = item;
                        audioFile.IsNetwork = false;
                        audioFile.IsDownloaded = true;
                    }
                }
                return audioFile;
            }
            catch
            {
                return null;
            }
        }

        public void PrintLocalSongs(int page)
        {
            string[] allItems = Shared.AudioDownloader.GetAllItems();
            int num = allItems.Length;
            if (num == 0)
            {
                Program.LogText(LogSeverity.Warning, "AudioModule", "No local files found.");
                return;
            }
            int num2 = (int)Math.Floor(Math.Log10(allItems.Length) + 1.0);
            int num3 = 20;
            int num4 = num / num3 + 1;
            if (page < 1 || page > num4)
            {
                ReplyAsync($"There are {num4} pages. Select page 1 to {num4}.");
                return;
            }
            for (int i = page - 1; i < page; i++)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                for (int j = 0; j < num3; j++)
                {
                    int num5 = i * num3 + j;
                    if (num5 >= num)
                    {
                        break;
                    }
                    string text = "";
                    for (int k = (num5 == 0) ? 1 : ((int)Math.Floor(Math.Log10(num5) + 1.0)); k < num2; k++)
                    {
                        text += "0";
                    }
                    string text2 = allItems[num5].Split(Path.DirectorySeparatorChar).Last();
                }
                ReplyAsync($"Page {i + 1}", false, embedBuilder.Build());
            }
        }

        public string GetLocalSong(int index)
        {
            return Shared.AudioDownloader.GetItem(index);
        }

        public async Task DownloadSongAsync(string path)
        {
            AudioFile audioFile = await GetAudioFileAsync(path);
            if (audioFile != null)
            {
                if (audioFile.Title != "")
                {
                    await ReplyAsync("Added to the download queue: `" + audioFile.Title + "`");
                    if (audioFile.IsNetwork)
                    {
                        Shared.AudioDownloader.Push(audioFile);
                    }
                    await Shared.AudioDownloader.StartDownloadAsync();
                }
                else
                {
                    await ReplyAsync("Sorry, but i cannot download media from this link.");
                }
            }
        }

        public async Task RemoveDuplicateSongsAsync()
        {
            Shared.AudioDownloader.RemoveDuplicateItems();
            await Task.Delay(0);
        }

        [Command("join", RunMode = RunMode.Async)]
        [Remarks("!join")]
        [Summary("Joins the user's voice channel.")]
        public async Task JoinVoiceChannel()
        {
            if (!GetDelayAction())
            {
                await JoinAudioAsync(base.Context.Guild, (base.Context.User as IVoiceState).VoiceChannel);
                await CheckAutoPlayAsync(base.Context.Guild, base.Context.Channel);
            }
        }

        [Command("leave", RunMode = RunMode.Async)]
        [Remarks("!leave")]
        [Summary("Leaves the current voice channel.")]
        public async Task LeaveVoiceChannel()
        {
            await LeaveAudioAsync(base.Context.Guild);
        }

        [Command("play", RunMode = RunMode.Async)]
        [Remarks("!play [url/index]")]
        [Summary("Plays a song by url or local path.")]
        public async Task PlayVoiceChannel([Remainder] string song)
        {
            await ForcePlayAudioAsync(base.Context.Guild, base.Context.Channel, song);
            if (GetNumPlaysCalled() == 0)
            {
                await CheckAutoPlayAsync(base.Context.Guild, base.Context.Channel);
            }
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Remarks("!pause")]
        [Summary("Pauses the current song, if playing.")]
        public async Task PauseVoiceChannel()
        {
            PauseAudio();
            EmbedBuilder emb = new EmbedBuilder();
            emb.WithAuthor(Context.Client.CurrentUser);
            emb.WithColor(Color.Red);
            emb.WithThumbnailUrl("https://cdn.discordapp.com/app-icons/516975705066700850/dfe5df212a161435768af049c48c6944.png");
            emb.WithTitle("Song paused.");
            await ReplyAsync(null, false, emb.Build());
            await Task.Delay(0);
        }

        [Command("resume", RunMode = RunMode.Async)]
        [Remarks("!resume")]
        [Summary("Pauses the current song, if paused.")]
        public async Task ResumeVoiceChannel()
        {
            ResumeAudio();
            EmbedBuilder emb = new EmbedBuilder();
            emb.WithAuthor(Context.Client.CurrentUser);
            emb.WithColor(Color.Red);
            emb.WithThumbnailUrl("https://cdn.discordapp.com/app-icons/516975705066700850/dfe5df212a161435768af049c48c6944.png");
            emb.WithTitle("Song resumed.");
            await ReplyAsync(null, false, emb.Build());
            await Task.Delay(0);
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Remarks("!stop")]
        [Summary("Stops the current song, if playing or paused.")]
        public async Task StopVoiceChannel()
        {
            EmbedBuilder emb = new EmbedBuilder();
            emb.WithAuthor(Context.Client.CurrentUser);
            emb.WithColor(Color.Red);
            emb.WithThumbnailUrl("https://cdn.discordapp.com/app-icons/516975705066700850/dfe5df212a161435768af049c48c6944.png");
            emb.WithTitle("Playback has been stopped.");
            await ReplyAsync(null, false, emb.Build());
            StopAudio();
            await Task.Delay(0);
        }

        [Command("volume")]
        [Remarks("!volume [num]")]
        [Summary("Changes the volume to [0 - 100].")]
        public async Task VolumeVoiceChannel(int volume)
        {
            if(volume > 0 && volume < 101)
            {
                AdjustVolume((float)volume / 100f);
                EmbedBuilder emb = new EmbedBuilder();
                emb.WithAuthor(Context.Client.CurrentUser);
                emb.WithColor(Color.Red);
                emb.WithThumbnailUrl("https://cdn.discordapp.com/app-icons/516975705066700850/dfe5df212a161435768af049c48c6944.png");
                emb.WithTitle("Volume set to " + volume + "%");
                await ReplyAsync(null, false, emb.Build());
            }
            else
            {
                await ReplyAsync("Valid range: 0-100.");
            }
            await Task.Delay(0);
        }

        [Command("add", RunMode = RunMode.Async)]
        [Remarks("!add [url/index]")]
        [Summary("Adds a song by url or local path to the playlist.")]
        public async Task AddVoiceChannel([Remainder] string song)
        {
            await PlaylistAddAsync(song);
            await CheckAutoPlayAsync(base.Context.Guild, base.Context.Channel);
        }

        [Command("add", RunMode = RunMode.Async)]
        public async Task AddVoiceChannelByIndex(int index)
        {
            await AddVoiceChannel(GetLocalSong(index));
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias(new string[]
        {
            "skip",
            "next"
        })]
        [Remarks("!skip")]
        [Summary("Skips the current song, if playing from the playlist.")]
        public async Task SkipVoiceChannel()
        {
            PlaylistSkip();
            await Task.Delay(0);
        }

        [Command("playlist", RunMode = RunMode.Async)]
        [Remarks("!playlist")]
        [Summary("Shows what's currently in the playlist.")]
        public async Task PrintPlaylistVoiceChannel()
        {
            PrintPlaylist();
            await Task.Delay(0);
        }

        [Command("autoplay", RunMode = RunMode.Async)]
        [Remarks("!autoplay [enable]")]
        [Summary("Starts the autoplay service on the current playlist.")]
        public async Task AutoPlayVoiceChannel(bool enable)
        {
            SetAutoPlay(enable);
            await CheckAutoPlayAsync(base.Context.Guild, base.Context.Channel);
        }

        [Command("download", RunMode = RunMode.Async)]
        [Remarks("!download [http]")]
        [Summary("Download songs into our local folder.")]
        public async Task DownloadSong([Remainder] string path)
        {
            await DownloadSongAsync(path);
        }
    }
}
