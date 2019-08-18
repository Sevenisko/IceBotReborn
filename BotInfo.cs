using Microsoft.Win32;
using System;

namespace Sevenisko.IceBot
{
    public class BotInfo
    {
        public enum DotNetRelease
        {
            NOTFOUND,
            NET20,
            NET35,
            NET40,
            NET45,
            NET451,
            NET452,
            NET46,
            NET461,
            NET462,
            NET47,
            NET471,
            NET472,
            NET48,
            CORE10,
            CORE11,
            CORE12,
            CORE20,
            CORE21,
            CORE22
        }

        public static DotNetRelease GetRelease(int release = default(int))
        {
#if CORE10
            return DotNetRelease.CORE10;
#elif CORE11
            return DotNetRelease.CORE11;
#elif CORE12
            return DotNetRelease.CORE12;
#elif CORE20
            return DotNetRelease.CORE20;
#elif CORE21
            return DotNetRelease.CORE21;
#elif CORE22
            return DotNetRelease.CORE22;
#elif DOTNET20
            return DotNetRelease.NET20;
#elif DOTNET35
            return DotNetRelease.NET35;
#elif DOTNET40
            return DotNetRelease.NET40;
#elif DOTNET45
            return DotNetRelease.NET45;
#elif DOTNET451
            return DotNetRelease.NET451;
#elif DOTNET452
            return DotNetRelease.NET452;
#elif DOTNET46
            return DotNetRelease.NET46;
#elif DOTNET461
            return DotNetRelease.NET461;
#elif DOTNET462
            return DotNetRelease.NET462;
#elif DOTNET47
            return DotNetRelease.NET47;
#elif DOTNET471
            return DotNetRelease.NET471;
#elif DOTNET472
            return DotNetRelease.NET472;
#elif DOTNET48
            return DotNetRelease.NET48;
#else
            return DotNetRelease.NOTFOUND;
#endif
        }

        public const int VersionMajor = 1;
        public const int VersionMinor = 2;
        public const int VersionBuild = 1416;
        public const int VersionRevision = 64;

        public static DateTime GetBotBuildDateTime()
        {
            return System.IO.File.GetLastWriteTime(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        public static string GetBotVersion()
        {
            return VersionMajor + "." + VersionMinor + "." + VersionBuild + "." + VersionRevision;
        }

        public static int GetVersion()
        {
            int release = 0;
            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                                                .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                release = Convert.ToInt32(key.GetValue("Release"));
            }
            return release;
        }
    }    
}
