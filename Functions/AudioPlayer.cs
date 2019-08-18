using Discord.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Sevenisko.IceBot
{
	public class AudioPlayer
	{
		private bool CIsRunning;

		private Process Process;

		private Stream Stream;

		private bool CIsPlaying;

		private float Volume = 1f;

		private int BLOCK_SIZE = 3840;

		public Process CreateLocalStream(string path)
		{
			try
			{
				return Process.Start(new ProcessStartInfo
				{
					FileName = "ffmpeg.exe",
					Arguments = "-hide_banner -loglevel panic -i \"" + path + "\" -ac 2 -f s16le -ar 48000 pipe:1",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				});
			}
			catch
			{
				Console.WriteLine("Error while opening local stream : " + path);
				return null;
			}
		}

		public Process CreateNetworkStream(string path)
		{
			try
			{
				return Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = "/C youtube-dl.exe -o - " + path + " | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				});
			}
			catch
			{
				Console.WriteLine("Error while opening network stream : " + path);
				return null;
			}
		}

		public async Task AudioPlaybackAsync(IAudioClient client, AudioFile song)
		{
			CIsRunning = true;
			Process = (song.IsNetwork ? CreateNetworkStream(song.FileName) : CreateLocalStream(song.FileName));
			Stream = client.CreatePCMStream(AudioApplication.Music);
			CIsPlaying = true;
			await Task.Delay(5000);
			while (Process != null && !Process.HasExited && Stream != null)
			{
				if (CIsPlaying)
				{
					int bLOCK_SIZE = BLOCK_SIZE;
					byte[] buffer = new byte[bLOCK_SIZE];
					int num = await Process.StandardOutput.BaseStream.ReadAsync(buffer, 0, bLOCK_SIZE);
					if (num <= 0)
					{
						break;
					}
					try
					{
						await Stream.WriteAsync(ScaleVolumeSafeAllocateBuffers(buffer, Volume), 0, num);
					}
					catch (Exception value)
					{
						Console.WriteLine(value);
						break;
					}
				}
			}
			if (Process != null && !Process.HasExited)
			{
				Process.Kill();
			}
			if (Stream != null)
			{
				Stream.FlushAsync().Wait();
			}
			Process = null;
			Stream = null;
			CIsPlaying = false;
			CIsRunning = false;
		}

        public byte[] ScaleVolumeSafeAllocateBuffers(byte[] audioSamples, float volume)
		{
			if (audioSamples == null)
			{
				return null;
			}
			if (audioSamples.Length % 2 != 0)
			{
				return null;
			}
			if (volume < 0f || volume > 1f)
			{
				return null;
			}
			byte[] array = new byte[audioSamples.Length];
			try
			{
				if (Math.Abs(volume - 1f) < 0.0001f)
				{
					Buffer.BlockCopy(audioSamples, 0, array, 0, audioSamples.Length);
					return array;
				}
				int num = (int)Math.Round((double)volume * 65536.0);
				for (int i = 0; i < array.Length; i += 2)
				{
					int num2 = (short)((audioSamples[i + 1] << 8) | audioSamples[i]) * num >> 16;
					array[i] = (byte)num2;
					array[i + 1] = (byte)(num2 >> 8);
				}
				return array;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				return null;
			}
		}

		public void AdjustVolume(float volume)
		{
			if (volume < 0f)
			{
				volume = 0f;
			}
			else if (volume > 1f)
			{
				volume = 1f;
			}
			Volume = volume;
		}

		public bool IsRunning()
		{
			return CIsRunning;
		}

		public bool IsPlaying()
		{
			if (Process != null)
			{
				return CIsPlaying;
			}
			return false;
		}

		public async Task Play(IAudioClient client, AudioFile song)
		{
			if (CIsRunning)
			{
				Stop();
			}
			while (CIsRunning)
			{
				await Task.Delay(1000);
			}
			await AudioPlaybackAsync(client, song);
		}

		public void Pause()
		{
			CIsPlaying = false;
		}

		public void Resume()
		{
			CIsPlaying = true;
		}

		public void Stop()
		{
			if (Process != null)
			{
				Process.Kill();
			}
		}
	}
}
