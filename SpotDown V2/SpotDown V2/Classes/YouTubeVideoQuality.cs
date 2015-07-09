using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Windows.Forms;

namespace SpotDown_V2.Classes
{
    /// <summary>
    /// Please give credit to me if you use this or any part of it.
    /// HF: http://www.hackforums.net/member.php?action=profile&uid=1389752
    /// GitHub: https://github.com/DarkN3ss61
    /// Website: http://jlynx.net/
    /// Twitter: https://twitter.com/jLynx_DarkN3ss
    /// </summary>
    /// 
    /// <summary>
    /// Contains information about the video url extension and dimension
    /// </summary>
    public class YouTubeVideoQuality
    {
        /// <summary>
        /// Gets or Sets the file name
        /// </summary>
        public string VideoTitle { get; set; }
        /// <summary>
        /// Gets or Sets the file extention
        /// </summary>
        public string Extention { get; set; }
        /// <summary>
        /// Gets or Sets the file url
        /// </summary>
        public string DownloadUrl { get; set; }
        /// <summary>
        /// Gets or Sets the youtube video url
        /// </summary>
        public string VideoUrl { get; set; }
        /// <summary>
        /// Gets or Sets the youtube video size
        /// </summary>
        public long VideoSize { get; set; }
        /// <summary>
        /// Gets or Sets the youtube video dimension
        /// </summary>
        public Size Dimension { get; set; }
        /// <summary>
        /// Gets the youtube video length
        /// </summary>
        public long Length { get; set; }
        public override string ToString()
        {
            string videoExtention = this.Extention;
            string videoDimension = formatSize(this.Dimension);
            string videoSize = String.Format(new FileSizeFormatProvider(), "{0:fs}", this.VideoSize);

            return String.Format("{0} ({1}) - {2}", videoExtention.ToUpper(), videoDimension, videoSize);
        }

        private string formatSize(Size value)
        {
            string s = value.Height >= 720 ? " HD" : "";
            return ((Size)value).Width + " x " + value.Height + s;
        }
    }
    /// <summary>
    /// Use this class to get youtube video urls
    /// </summary>
    public class YouTubeDownloader
    {
        private Helper helper = new Helper();

        public List<YouTubeVideoQuality> GetYouTubeVideoUrls(string VideoUrl)
        {
            var list = new List<YouTubeVideoQuality>();

            var id = GetVideoIDFromUrl(VideoUrl);
            var infoUrl = string.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);
            var infoText = new WebClient().DownloadString(infoUrl);
            var infoValues = HttpUtility.ParseQueryString(infoText);
            if (infoValues["errorcode"] == "150")
            {
                //Age restriction error, cant download
                return null;
            }
            var title = infoValues["title"];
            var videoDuration = infoValues["length_seconds"];
            var videos = infoValues["url_encoded_fmt_stream_map"].Split(',');
            foreach (var item in videos)
            {
                try
                {
                    var data = HttpUtility.ParseQueryString(item);
                    var server = Uri.UnescapeDataString(data["fallback_host"]);
                    var signature = data["sig"] ?? data["signature"] ?? data["s"];   // Hans: Added "s" for encrypted signatures
                    var url = Uri.UnescapeDataString(data["url"]) + "&fallback_host=" + server;

                    if (!string.IsNullOrEmpty(signature) && data["s"] == null)
                    {
                        url += "&signature=" + signature;
                    }

                    // If the download-URL contains encrypted signature
                    if (data["s"] != null)
                    {
                        string html = helper.DownloadWebPage(VideoUrl);
                        string Player_Version = Regex.Match(html, @"""\\/\\/s.ytimg.com\\/yts\\/jsbin\\/html5player-(.+?)\.js""").Groups[1].ToString();

                        // Decrypt the signature
                        string decryptedSignature = GetDecipheredSignature(Player_Version, signature.ToString());

                        // The new download-url with decrypted signature
                        url += "&signature=" + decryptedSignature;
                    }

                    var size = getSize(url);
                    var videoItem = new YouTubeVideoQuality();
                    videoItem.DownloadUrl = url;
                    videoItem.VideoSize = size;
                    videoItem.VideoTitle = title;
                    var tagInfo = new ITagInfo(Uri.UnescapeDataString(data["itag"]));
                    videoItem.Dimension = tagInfo.VideoDimensions;
                    videoItem.Extention = tagInfo.VideoExtentions;
                    videoItem.Length = long.Parse(videoDuration);
                    list.Add(videoItem);
                }
                catch { }
            }

            return list;
        }

        private long getSize(string videoUrl)
        {
            HttpWebRequest fileInfoRequest = (HttpWebRequest)HttpWebRequest.Create(videoUrl);
            fileInfoRequest.Proxy = helper.InitialProxy();
            HttpWebResponse fileInfoResponse = (HttpWebResponse)fileInfoRequest.GetResponse();
            long bytesLength = fileInfoResponse.ContentLength;
            fileInfoRequest.Abort();
            return bytesLength;
        }

        /// <summary>
        /// Get the ID of a youtube video from its URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetVideoIDFromUrl(string url)
        {
            url = url.Substring(url.IndexOf("?") + 1);
            char[] delimiters = { '&', '#' };
            string[] props = url.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            string videoid = "";
            foreach (string prop in props)
            {
                if (prop.StartsWith("v="))
                {
                    videoid = prop.Substring(prop.IndexOf("v=") + 2);
                }
            }

            return videoid;
        }

        /// <summary>
        /// Decrypt the signature (uses code from project "YoutubeExtractor")
        /// </summary>
        private string GetDecipheredSignature(string htmlPlayerVersion, string signature)
        {
            Decipherer decipherer = new Decipherer();
            int CorrectSignatureLength = 81;
            if (signature.Length == CorrectSignatureLength)
            {
                return signature;
            }
            return decipherer.DecipherWithVersion(signature, htmlPlayerVersion);
        }

    }
    /// <summary>
    /// Info for itag values
    /// </summary>
    public struct ITagInfo
    {
        const string itagExtentions = "5=flv,6=flv,17=3gp,18=mp4,22=mp4,34=flv,35=flv,36=3gp,37=mp4,38=mp4,43=webm,44=webm,45=webm,46=webm,82=3D.mp4,83=3D.mp4,84=3D.mp4,85=3D.mp4,100=3D.webm,101=3D.webm,102=3D.webm,120=live.flv";
        const string itagWideDimensions = "5=320x180,6=480x270,17=176x99,18=640x360,22=1280x720,34=640x360,35=854x480,36=320x180,37=1920x1080,38=2048x1152,43=640x360,44=854x480,45=1280x720,46=1920x1080,82=480x270,83=640x360,84=1280x720,85=1920x1080,100=640x360,101=640x360,102=1280x720,120=1280x720";
        const string itagDimensions = "5=320x240,6=480x360,17=176x144,18=640x480,22=1280x960,34=640x480,35=854x640,36=320x240,37=1920x1440,38=2048x1536,43=640x480,44=854x640,45=1280x960,46=1920x1440,82=480x360,83=640x480,84=1280x960,85=1920x1440,100=640x480,101=640x480,102=1280x960,120=1280x960";

        public ITagInfo(string iTag)
            : this()
        {
            iTag = (iTag + "").Trim();
            foreach (var item in itagExtentions.Split(','))
            {
                var nameValue = item.Split('=');
                if (nameValue[0] != iTag) continue;
                VideoExtentions = nameValue[1];
            }
            foreach (var item in itagWideDimensions.Split(','))
            {
                var nameValue = item.Split('=');
                if (nameValue[0] != iTag) continue;
                var widthAndHeight = nameValue[1].Split('x');
                VideoDimensions = new Size(int.Parse(widthAndHeight[0]), int.Parse(widthAndHeight[1]));
            }
        }

        /// <summary>
        /// Gets the Video Extentions that belong to itag
        /// </summary>
        public string VideoExtentions { get; private set; }
        /// <summary>
        /// Gets the Video Dimensions that belong to itag
        /// </summary>
        public Size VideoDimensions { get; private set; }

    }
}
