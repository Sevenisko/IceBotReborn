using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sevenisko.IceBot.Admin
{
    public class WebRequest
    {
        public byte[] data;
        public int ResponseCode;
    }

    public class WebServer
    {
        private static HttpListener listener;

        public static string ProcessOperation(string url)
        {
            string[] val;

            if (url.Contains('='))
            {
                val = url.Split('=');
            }
            else
            {
                val = new string[2];
                val[0] = url;
                val[1] = "none";
            }

            string retCode = "";

            switch(val[0])
            {
                case "/action/addbadword":
                    {
                        Program.config.BadWords.Add(val[1].Replace("%20", " "));
                        Program.LogText(Discord.LogSeverity.Info, "WebServer", $"Added {val[1].Replace("%20", " ")} into badword list");
                        retCode = "";
                    }
                    break;
                case "/action/listbadwords":
                    {
                        retCode += "<html>" 
                            + "<body>"
                            + "<h1>Badword list</h1>"
                            + "<hr>"
                            + "<ul>";
                        foreach(string badword in Program.config.BadWords)
                        {
                            retCode += "<li>" + badword + "</li>";
                        }
                        retCode += "</ul>"
                            + "<hr>"
                            + "IceBot v" + BotInfo.GetBotVersion()
                            + "</body>"
                            + "</html>";
                    }
                    break;
                default:
                    {
                        retCode = "fail";
                    }
                    break;
            }

            return retCode;
        }

        public static WebRequest ProcessRequest(string path)
        {
            WebRequest request = new WebRequest();

            if (File.Exists("static\\" + path))
            {
                string type = MimeTypeMap.GetMimeType(Path.GetExtension("static\\" + path));

                string content = "";

                if (type.Contains("text/"))
                {
                    content = File.ReadAllText("static\\" + path);
                }
                else if (type.Contains("audio/"))
                {
                    content = "$audio$";
                }
                else if (type.Contains("image/"))
                {
                    content = "$image$";
                }
                else if (type.Contains("video/"))
                {
                    content = "$video$";
                }
                else
                {
                    switch (type)
                    {
                        case "application/xaml+xml":
                        case "application/xhtml+xml":
                        case "application/xml":
                            {
                                content = File.ReadAllText("static\\" + path);
                            }
                            break;
                    }
                }

                request.data = Encoding.UTF8.GetBytes(content);
                request.ResponseCode = 200;
            }
            else if (Directory.Exists("static\\" + path))
            {
                var dirsInDir = Directory.GetDirectories("static\\" + path).OrderBy(f => f);
                var filesInDir = Directory.GetFiles("static\\" + path, "*.*").OrderBy(f => f);

                string dirs = "";
                string files = "";

                var count = path.Count(x => x == '/');

                if (count == 1)
                {
                    var dirName = new DirectoryInfo(path).Name;

                    dirs += $"<a href=\"{path.Replace(dirName, "")}\">../</a><br>";
                }

                if (count == 2 || count > 2)
                {
                    var dirName = new DirectoryInfo(path).Name;
                    
                    dirs += $"<a href=\"{path.Replace("/" + dirName, "")}\">../</a><br>";
                }

                foreach (string dir in dirsInDir)
                {
                    var dirName = new DirectoryInfo(dir).Name;
                    dirs += $"<a href=\"{dir.Replace("static\\/", "")}\">{dirName}/</a><br>";
                }

                foreach (string file in filesInDir)
                {
                    files += $"<a href=\"{file.Replace("static\\/", "")}\">{Path.GetFileName(file)}</a><br>";
                }

                string content = "<html>"
                    + "<head>"
                    + $"<title>Index of {path}</title>"
                    + "</head>"
                    + "<body>"
                    + $"<h1>Index of {path}</h1>"
                    + "<hr>"
                    + dirs
                    + files
                    + "<hr>"
                    + $"<p>IceBot v{BotInfo.GetBotVersion()}</p>"
                    + "</body>"
                    + "</html>";
                request.data = Encoding.UTF8.GetBytes(content);
                request.ResponseCode = 200;
            }
            else
            {
                string other = ProcessOperation(path);

                if (other == "")
                {
                    request.data = Encoding.UTF8.GetBytes("Success");
                    request.ResponseCode = 404;
                }
                else if (other == "fail")
                {
                    try
                    {
                        string content = File.ReadAllText("static\\errorpages\\404.htm");
                        request.data = Encoding.UTF8.GetBytes(content);
                        request.ResponseCode = 404;
                    }
                    catch
                    {
                        request.data = Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
                        request.ResponseCode = 404;
                    }
                }
                else
                {
                    request.data = Encoding.UTF8.GetBytes(other);
                    request.ResponseCode = 404;
                }
            }
            return request;
        }

        public static void SendAudio(HttpListenerContext ctx, string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                ctx.Response.ContentLength64 = fs.Length;
                ctx.Response.SendChunked = true;
                //obj.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                ctx.Response.ContentType = "audio/wav";
                ctx.Response.Headers.Add("Content-Range", $"bytes 0-{fs.Length - 1}/{fs.Length}");

                //obj.Response.AddHeader("Content-disposition", "attachment; filename=" + fs.Name);
                ctx.Response.StatusCode = 206; // set to partial content

                byte[] buffer = new byte[64 * 1024];

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(ctx.Response.OutputStream))
                    {
                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {

                            bw.Write(buffer, 0, read);
                            bw.Flush(); //seems to have no effect
                        }

                        bw.Close();
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        public static void SendVideo(HttpListenerContext ctx, string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                ctx.Response.ContentLength64 = fs.Length;
                ctx.Response.SendChunked = true;
                //obj.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                ctx.Response.ContentType = "video/mp4";
                ctx.Response.Headers.Add("Content-Range", $"bytes 0-{fs.Length - 1}/{fs.Length}");

                //obj.Response.AddHeader("Content-disposition", "attachment; filename=" + fs.Name);
                ctx.Response.StatusCode = 206; // set to partial content

                byte[] buffer = new byte[64 * 1024];

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(ctx.Response.OutputStream))
                    {
                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {

                            bw.Write(buffer, 0, read);
                            bw.Flush(); //seems to have no effect
                        }

                        bw.Close();
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        public static void SendImage(HttpListenerContext ctx, string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                ctx.Response.ContentLength64 = fs.Length;
                ctx.Response.SendChunked = true;
                //obj.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                ctx.Response.ContentType = "image/png";
                ctx.Response.Headers.Add("Content-Range", $"bytes 0-{fs.Length - 1}/{fs.Length}");

                //obj.Response.AddHeader("Content-disposition", "attachment; filename=" + fs.Name);
                ctx.Response.StatusCode = 206; // set to partial content

                byte[] buffer = new byte[64 * 1024];

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(ctx.Response.OutputStream))
                    {
                        int read;
                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {

                            bw.Write(buffer, 0, read);
                            bw.Flush(); //seems to have no effect
                        }

                        bw.Close();
                    }
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        public static void WriteFile(HttpListenerContext ctx, string path)
        {
            var response = ctx.Response;
            using (FileStream fs = File.OpenRead(path))
            {
                string filename = Path.GetFileName(path);
                //response is HttpListenerContext.Response...
                response.ContentLength64 = fs.Length;
                response.SendChunked = false;
                response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                response.AddHeader("Content-disposition", "attachment; filename=" + filename);

                byte[] buffer = new byte[64 * 1024];
                int read;
                using (BinaryWriter bw = new BinaryWriter(response.OutputStream))
                {
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, read);
                        bw.Flush(); //seems to have no effect
                    }

                    bw.Close();
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.OutputStream.Close();
            }
        }

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                string path = req.Url.AbsolutePath.Replace("%20", " ");

                WebRequest request = ProcessRequest(path);

                byte[] data = request.data;

                string c = Encoding.UTF8.GetString(data);

                if(c == "")
                {
                    WriteFile(ctx, "static\\" + path);
                }
                else if (c == "$audio$")
                {
                    SendAudio(ctx, "static\\" + path);
                }
                else if (c == "$video$")
                {
                    SendVideo(ctx, "static\\" + path);
                }
                else if (c == "$image$")
                {
                    SendImage(ctx, "static\\" + path);
                }
                else
                {
                    resp.ContentType = "text/html";
                    resp.StatusCode = request.ResponseCode;
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            }
        }

        public void Start(string url)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Program.LogText(Discord.LogSeverity.Info, "WebServer", "Started HTTP listener for " + url);

            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            listener.Close();
        }
    }
}
