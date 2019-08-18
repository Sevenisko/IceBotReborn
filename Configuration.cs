using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Sevenisko.IceBot
{
    public class RemoteUser
    {
        [XmlElement("Username")]
        public string Username { get; set; }
        [XmlElement("Password")]
        public string Password { get; set; }
    }

    public class Message
    {
        [XmlElement("Question")]
        public string Question { get; set; }
        [XmlElement("Answer")]
        public string Answer { get; set; }
    }

    public class RadioStation
    {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("Filename")]
        public string Filename { get; set; }
        [XmlElement("Thumbnail")]
        public string Thumbnail { get; set; }
    }

    [Serializable()]
    public class YouTube
    {
        public bool Enabled { get; set; }
        public string APIToken { get; set; }
    }

    [Serializable()]
    public class Soundcloud
    {
        public bool Enabled { get; set; }
        public string APIToken { get; set; }
    }

    [Serializable()]
    [XmlRoot("IceBotConfig")]
    public class ConfigFile
    {
        [XmlElement("BotToken")]
        public string BotToken { get; set; }
        [XmlElement("SCSettings")]
        public Soundcloud SCSettings { get; set; }
        [XmlElement("YTSettings")]
        public YouTube YTSettings { get; set; }
        [XmlElement("StatusOnJoin")]
        public string StatusOnJoin { get; set; }
        public string YoutubeToken { get; set; }
        [XmlElement("CommandPrefix")]
        public string CommandPrefix { get; set; }
        [XmlElement("RemoteAdminEnabled")]
        public bool RemoteEnabled { get; set; }
        [XmlElement("RemoteAdminPort")]
        public int RemotePort { get; set; }
        [XmlElement("AntiBadWord")]
        public bool AntiBWEnabled { get; set; }
        [XmlArray("Users")]
        [XmlArrayItem("User")]
        public List<RemoteUser> Users { get; set; }
        [XmlArray("BadWords")]
        [XmlArrayItem("BadWord")]
        public List<string> BadWords { get; set; }
        [XmlArray("Messages")]
        [XmlArrayItem("Message")]
        public List<Message> Messages { get; set; }
        [XmlArray("Radio")]
        [XmlArrayItem("RadioStation")]
        public List<RadioStation> Stations { get; set; }
    }

    public class Configuration
    {
        public static ConfigFile LoadConfig(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigFile));

            StreamReader reader = new StreamReader(path);
            ConfigFile returnConf = (ConfigFile)serializer.Deserialize(reader);
            reader.Close();
            return returnConf;
        }
        public static bool SaveConfig(string path, ConfigFile config)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigFile));

                FileStream file = File.Create(path);

                serializer.Serialize(file, config);
                file.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
