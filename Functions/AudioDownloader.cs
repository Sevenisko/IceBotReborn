using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sevenisko.IceBot
{
    public class AudioDownloader
    {
        private readonly ConcurrentQueue<AudioFile> DownloadQueue = new ConcurrentQueue<AudioFile>();

        private string DownloadPath = "Downloads";

        private bool CIsRunning;

        private string CCurrentlyDownloading = "";

        private bool AllowDuplicates = true;

        public string GetDownloadPath()
        {
            return DownloadPath;
        }

        public bool IsRunning()
        {
            return CIsRunning;
        }

        public string CurrentlyDownloading()
        {
            return CCurrentlyDownloading;
        }

        public string[] GetAllItems()
        {
            string[] files = Directory.GetFiles(DownloadPath);
            if (files.Length == 0)
            {
                return new string[1]
                {
                    "There are currently no items downloaded."
                };
            }
            return files;
        }

        public string GetItem(string item)
        {
            try
            {
                if (File.Exists(DownloadPath + "\\" + item) && !CCurrentlyDownloading.Equals(item))
                {
                    return DownloadPath + "\\" + item;
                }
            }
            catch
            {
            }
            try
            {
                if (File.Exists(DownloadPath + "\\" + item + ".mp3") && !CCurrentlyDownloading.Equals(item))
                {
                    return DownloadPath + "\\" + item + ".mp3";
                }
            }
            catch
            {
            }
            return null;
        }

        public string GetItem(int index)
        {
            string[] files = Directory.GetFiles(DownloadPath);
            if (index < 0 || index >= files.Length)
            {
                return null;
            }
            return files[index].Split(Path.DirectorySeparatorChar).Last();
        }

        private string GetDuplicateItem(string item)
        {
            string text = null;
            int num = 0;
            text = Path.Combine(DownloadPath, item + ".mp3");
            while (File.Exists(text))
            {
                text = Path.Combine(DownloadPath, item + "_" + num++ + ".mp3");
            }
            return text;
        }

        public void RemoveDuplicateItems()
        {
            ConcurrentDictionary<string, int> concurrentDictionary = new ConcurrentDictionary<string, int>();
            string[] files = Directory.GetFiles(DownloadPath);
            foreach (string path in files)
            {
                string text = Path.GetFileNameWithoutExtension(path);
                if (int.TryParse(text.Split('_').Last(), out int _))
                {
                    text = text.Split(new char[1]
                    {
                        '_'
                    }, 2)[0];
                }
                concurrentDictionary.TryRemove(text, out int value);
                concurrentDictionary.TryAdd(text, ++value);
                try
                {
                    if (value >= 2)
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    Console.WriteLine("Problem while deleting duplicates.");
                }
            }
        }

        private AudioFile Pop()
        {
            DownloadQueue.TryDequeue(out AudioFile result);
            return result;
        }

        public void Push(AudioFile song)
        {
            DownloadQueue.Enqueue(song);
        }

        public async Task StartDownloadAsync()
        {
            if (CIsRunning)
            {
                return;
            }
            CIsRunning = true;
            while (DownloadQueue.Count > 0)
            {
                if (!CIsRunning)
                {
                    return;
                }
                await DownloadAsync(Pop());
            }
            CIsRunning = false;
        }

        private async Task DownloadAsync(AudioFile song)
        {
            if (!song.IsNetwork)
            {
                return;
            }
            string item = GetItem(song.Title + ".mp3");
            if (item != null)
            {
                if (!AllowDuplicates)
                {
                    return;
                }
                item = GetDuplicateItem(song.Title);
            }
            else
            {
                item = DownloadPath + "\\" + song.Title + ".mp3";
            }
            CCurrentlyDownloading = item;
            await Program.LogText(Discord.LogSeverity.Info, "AudioDownloader", "Currently downloading: " + song.Title);
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "youtube-dl",
                    Arguments = "-x --audio-format mp3 -o \"" + item.Replace(".mp3", ".%(ext)s") + "\" " + song.FileName,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }).WaitForExit();
            }
            catch
            {
                await Program.LogText(Discord.LogSeverity.Error, "AudioDownloader", "Error while downloading " + song.Title);
                if (GetItem(item) != null)
                {
                    File.Delete(item);
                }
            }
            song.FileName = item;
            song.IsNetwork = false;
            song.IsDownloaded = true;
            CCurrentlyDownloading = "";
            await Task.Delay(0);
        }

        public void StopDownload()
        {
            CIsRunning = false;
        }

        public bool? VerifyNetworkPath(string path)
        {
            return path?.StartsWith("http");
        }

        public async Task<AudioFile> GetAudioFileInfo(string path)
        {
            if (path == null)
            {
                return null;
            }
            await Program.LogText(Discord.LogSeverity.Info, "AudioDownloader", "Extracting Metadata for: " + path);
            bool? flag = VerifyNetworkPath(path);
            if (!flag.HasValue)
            {
                await Program.LogText(Discord.LogSeverity.Warning, "AudioDownloader", "Path is invalid.");
                return null;
            }
            AudioFile StreamData = new AudioFile();
            if (flag == false)
            {
                try
                {
                    string item = GetItem(path);
                    if (item != null)
                    {
                        path = item;
                    }
                    if (!File.Exists(path))
                    {
                        await Program.LogText(Discord.LogSeverity.Warning, "AudioDownloader", $"File {path} doesn't exist.");
                        throw new NullReferenceException();
                    }
                    StreamData.FileName = path;
                    StreamData.Title = path.Split(Path.DirectorySeparatorChar).Last();
                    if (StreamData.Title.CompareTo("") == 0)
                    {
                        StreamData.Title = path;
                    }
                    StreamData.IsNetwork = flag.Value;
                }
                catch
                {
                    await Program.LogText(Discord.LogSeverity.Error, "AudioDownloader", "Failed to get local file information!");
                    return null;
                }
            }
            else if (flag == true)
            {
                try
                {
                    Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "youtube-dl",
                        Arguments = "-s -e " + path,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    });
                    process.WaitForExit();
                    string[] array = process.StandardOutput.ReadToEnd().Split('\n');
                    StreamData.FileName = path;
                    if (array.Length != 0)
                    {
                        StreamData.Title = array[0];
                    }
                    StreamData.IsNetwork = flag.Value;
                }
                catch
                {
                    await Program.LogText(Discord.LogSeverity.Error, "youtube-dl", "Failed to extract the data!");
                    return null;
                }
            }
            await Task.Delay(0);
            return StreamData;
        }
    }
}