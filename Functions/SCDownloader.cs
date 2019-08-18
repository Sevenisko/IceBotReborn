using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sevenisko.IceBot
{
    class SCDownloader
    {
        static WebClient WebClient = new WebClient();

        public static JObject GetYouTubeInfo(string ID)
        {
            JObject data =
               GetJson("https://www.googleapis.com/youtube/v3/videos?part=id%2C+snippet&id=" + ID + "&key=" + Program.config.YTSettings.APIToken);

            return data;
        }

        public static JObject GetTrack(string url)
        {
            if (!Util.ValidTrackLink(url))
                return null;

            JObject data =
               GetJson("http://api.soundcloud.com/resolve.json?url=" + url + "&client_id=" +
                       Program.config.SCSettings.APIToken);

            return data;
        }

        public static string GetTrackDownloadLink(string url)
        {
            JObject data = GetTrack(url);
            if (data == null || data["streaurl"] == null)
            {
                Console.WriteLine("Error in GetTrackDownloadLink, link was invalid");
                return String.Empty;
            }
            return data["streaurl"].ToString() + "?client_id=" + Program.config.SCSettings.APIToken;
        }

        public static void DownloadTrack(string url, string folderPath)
        {
            JObject data = GetTrack(url);
            if (data == null)
            {
                return;
            }

            try
            {
                if (data["streaurl"] == null)
                {
                    if (data["id"] == null)
                        return;

                    WebClient.DownloadFile("https://api.soundcloud.com/tracks/" + data["id"].ToString() + "/stream" + "?client_id=" + Program.config.SCSettings.APIToken, folderPath + @"\" + Util.ValidateString(data["title"].ToString()) + ".mp3");
                    return;
                }
                WebClient.DownloadFile(data["streaurl"].ToString() + "?client_id=" + Program.config.SCSettings.APIToken, folderPath + @"\" + Util.ValidateString(data["title"].ToString()) + ".mp3");
            }
            catch (Exception e)
            {

            }
        }

        public static JArray GetFavorites(int userId)
        {
            JArray data =
                GetJsonArray("http://api.soundcloud.com/users/" + userId + "/favorites/?client_id=" + Program.config.SCSettings.APIToken + "&limit=200");

            return data;
        }

        public static JArray GetPlaylist(string url)
        {
            JObject data =
                GetJson("http://api.soundcloud.com/resolve.json?url=" + url + "&client_id=" + Program.config.SCSettings.APIToken);

            JArray t = JArray.Parse(data["tracks"].ToString());

            return t;
        }

        public static JArray GetFavoritesOffset(int offset, int userId)
        {
            JArray data =
                GetJsonArray("http://api.soundcloud.com/users/" + userId + "/favorites/?client_id=" + Program.config.SCSettings.APIToken + "&offset=" + offset + "&limit=200");

            return data;
        }

        public static User GetUser(string userName)
        {
            JObject data =
                GetJson("http://api.soundcloud.com/resolve.json?url=http://soundcloud.com/" + userName + "&client_id=" +
                        Program.config.SCSettings.APIToken);
            if (data == null)
            {
                Console.WriteLine("Wrong username");
                return null;
            }

            User user = new User(int.Parse(data["id"].ToString()), data["kind"].ToString(), data["permalink"].ToString(), data["username"].ToString(),
                data["uri"].ToString(), data["permalink_url"].ToString(), data["avatar_url"].ToString(), data["country"].ToString(), data["first_name"].ToString(),
                data["last_name"].ToString(), data["full_name"].ToString(), data["description"].ToString(), data["city"].ToString(), data["website"].ToString(),
                data["website_title"].ToString(), Boolean.Parse(data["online"].ToString()), int.Parse(data["track_count"].ToString()), int.Parse(data["playlist_count"].ToString()),
                data["plan"].ToString(), int.Parse(data["public_favorites_count"].ToString()), int.Parse(data["followings_count"].ToString()));
            return user;
        }

        public static JObject GetJson(string url)
        {
            try
            {
                WebClient.Encoding = Encoding.UTF8;
                var data = WebClient.DownloadString(url);
                return JObject.Parse(data);
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                return null;
            }
        }

        public static JArray GetJsonArray(string url)
        {
            try
            {
                WebClient.Encoding = Encoding.UTF8;
                var data = WebClient.DownloadString(url);
                return JArray.Parse(data);
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
                return null;
            }
        }
    }

    public class Util
    {
        public static bool ValidTrackLink(string url)
        {
            Regex r = new Regex(@"https{0,1}:\/\/w{0,3}\.*soundcloud\.com\/([A-Za-z0-9_-]+)\/([A-Za-z0-9_-]+)[^< ]*");
            return r.IsMatch(url);
        }

        public static bool ValidUser(string username)
        {
            JObject data =
                SCDownloader.GetJson("http://api.soundcloud.com/resolve.json?url=http://soundcloud.com/" + username + "&client_id=" +
                       Program.config.SCSettings.APIToken);
            return data != null;
        }

        public static string ValidateString(string s)
        {
            string str = s;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                str = str.Replace(c, '_');
            }

            return str;
        }
    }

    public class User
    {
        private int _id;
        private string _kind, _permalink, _username, _uri, _permalink_url, _avatar_url, _country, _first_name;
        private string _last_name, _full_name, _description, _city, _website, _website_title;
        private bool _online;
        private int _track_count, _playlist_count;
        private string _plan;
        private int _favorites_count, _following_count;

        public User(int id, string kind, string permalink, string username, string uri, string permalink_url,
            string avatar_url, string country, string first_name, string last_name, string full_name, string description,
            string city, string website, string website_title, bool online, int track_count, int playlist_count, string plan, int favorites_count, int following_count
            )
        {
            this._id = id;
            this._kind = kind;
            this._permalink = permalink;
            this._username = username;
            this._uri = uri;
            this._permalink_url = permalink_url;
            this._avatar_url = avatar_url;
            this._country = country;
            this._first_name = first_name;
            this._last_name = last_name;
            this._full_name = full_name;
            this._description = description;
            this._city = city;
            this._website = website;
            this._website_title = website_title;
            this._online = online;
            this._track_count = track_count;
            this._playlist_count = playlist_count;
            this._plan = plan;
            this._favorites_count = favorites_count;
            this._following_count = following_count;
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Kind
        {
            get { return _kind; }
            set { _kind = value; }
        }

        public string Permalink
        {
            get { return _permalink; }
            set { _permalink = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Uri1
        {
            get { return _uri; }
            set { _uri = value; }
        }

        public string PermalinkUrl
        {
            get { return _permalink_url; }
            set { _permalink_url = value; }
        }

        public string AvatarUrl
        {
            get { return _avatar_url; }
            set { _avatar_url = value; }
        }

        public string Country
        {
            get { return _country; }
            set { _country = value; }
        }

        public string FirstName
        {
            get { return _first_name; }
            set { _first_name = value; }
        }

        public string LastName
        {
            get { return _last_name; }
            set { _last_name = value; }
        }

        public string FullName
        {
            get { return _full_name; }
            set { _full_name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        public string Website
        {
            get { return _website; }
            set { _website = value; }
        }

        public string WebsiteTitle
        {
            get { return _website_title; }
            set { _website_title = value; }
        }

        public bool Online
        {
            get { return _online; }
            set { _online = value; }
        }

        public int TrackCount
        {
            get { return _track_count; }
            set { _track_count = value; }
        }

        public int PlaylistCount
        {
            get { return _playlist_count; }
            set { _playlist_count = value; }
        }

        public string Plan
        {
            get { return _plan; }
            set { _plan = value; }
        }

        public int FavoritesCount
        {
            get { return _favorites_count; }
            set { _favorites_count = value; }
        }

        public int FollowingCount
        {
            get { return _following_count; }
            set { _following_count = value; }
        }
    }

}
