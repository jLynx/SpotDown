using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json.Linq;
using SpotDown_V2.Classes;
using TagLib;
using SpotDown_V2.Properties;
using File = System.IO.File;

namespace SpotDown_V2
{
    /// <summary>
    /// Please give credit to me if you use this or any part of it.
    /// HF: http://www.hackforums.net/member.php?action=profile&uid=1389752
    /// GitHub: https://github.com/DarkN3ss61
    /// Website: http://jlynx.net/
    /// Twitter: https://twitter.com/jLynx_DarkN3ss
    /// </summary>

    //ToDo
    //-Fix this mess of code.
    //-Does not always start downloading first time. Restart program and try again.
    //-Progress bar can be glitchy.

    public partial class Form1 : Form
    {
        private const string ApiKey = "AIzaSyDGiPclA6K7Kr7MoiHd7iIplSB5mfNrudg";
        string _dir = "songs/";
        string _tempDir = "songs/temp/";
        int maxRunning = 10;
        int _running = 0;
        int _songs = 0;
        int _youtubeNum = 0;
        int _youtubeDownloadedNum = 0;
        int _mp3ClanNum = 0;
        int _mp3ClanDownloadedNum = 0;
        int _totalQuedNum = 0;
        int _totalFinished = 0;
        int _current = 0;
        bool _debug = false;
        string _ffmpegPath;

        ListViewData[] downloadData = new ListViewData[10000];
        private int[] songsArray = new int[10000];
        private PassArguments[][] songArray = new PassArguments[10000][];

        private const string Website = @"http://jlynx.net/download/spotdown/SpotDown.exe";
        private readonly string _spotDownUa = "SpotDown " + Assembly.GetExecutingAssembly().GetName().Version + " " + Environment.OSVersion;

        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            _ffmpegPath = Path.Combine(Path.GetTempPath(), "ffmpeg.exe");
            File.WriteAllBytes(_ffmpegPath, Resources.ffmpeg);

            CheckUpdate();
            listView1.SmallImageList = imageList1;

            Size = new Size(597, 448);

            if (Settings.Default.SaveDir.Length < 1)
            {
                downloadDirTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            else
            {
                downloadDirTextBox.Text = Settings.Default.SaveDir;
            }
            _dir = downloadDirTextBox.Text + "/";
            _tempDir = _dir + "temp/";
            if (!Directory.Exists(_tempDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(_tempDir);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            Log("Started");
        }

        public void Log(string text, bool debugLog = false)
        {
            const string logFormat = "[{0}] >> {1}\n";

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Log text can not be empty!");

            var logText = text;
            if (debugLog && _debug)
            {
                textBox1.AppendText(string.Format(logFormat, DateTime.Now.ToLongTimeString(), logText));
            }
            else if (debugLog == false)
            {
                textBox1.AppendText(string.Format(logFormat, DateTime.Now.ToLongTimeString(), logText));
            }
        }

        public class PassArguments
        {
            public string PassedURL { get; set; }
            public string PassedSong { get; set; }
            public string PassedArtist { get; set; }
            public string PassedAlbum { get; set; }
            public string PassedAlbumId { get; set; }
            public int PassedNum { get; set; }
            public string PassedFileName { get; set; }
            public Mp3ClanTrack PassedTrack { get; set; }
            public List<YouTubeVideoQuality> YouTubeVideoQuality { get; set; }
            public string PassedSpotCode { get; set; }
            public double PassedLength { get; set; }
            public double PassedLengthMs { get; set; }
            public string PassedimageURL { get; set; }
            public int PassedSession { get; set; }
        } 
        
        public class ListViewData
        {
            public string Message { get; set; }
            public string FileName { get; set; }
            public int Number { get; set; }
            public int Image { get; set; }
        }

        private void CheckUpdate()
        {
            int latest = Convert.ToInt32(GetPage("http://jlynx.net/download/spotdown/version.txt", _spotDownUa));
            int currentVersion = Convert.ToInt32(Assembly.GetExecutingAssembly().GetName().Version.ToString().Replace(".", ""));
            labelVersion.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version;
            if (latest <= currentVersion)
                return;
            if (MessageBox.Show("There is a newer version of SpotDown available. Would you like to upgrade?", "SpotDown", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Process.Start(Website);
                Application.Exit();
            }
        }

        private string GetPage(string url, string ua)
        {
            var w = new WebClient();
            w.Headers.Add("user-agent", ua);
            string s = w.DownloadString(url);
            return s;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_running != 0)
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to quit? Some songs are sill being downloaded.", "Are you sure?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Process.GetCurrentProcess().Kill();
                }
                else if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            try
            {
                File.Delete(_ffmpegPath);
            }
            catch (Exception)
            {
            }
        }

        private void downloadDirTextBox_TextChanged(object sender, EventArgs e)
        {
            _dir = downloadDirTextBox.Text + "/";
            _tempDir = _dir + "temp/";
            if (!Directory.Exists(_tempDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(_tempDir);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
                e.Item.Selected = false;
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    _current++;
                    songArray[_current] = new PassArguments[10000];
                    string data = (string)e.Data.GetData(DataFormats.StringFormat);
                    data = data.Replace("http://open.spotify.com/track/", "");
                    string[] strArrays = data.Split(new char[] { '\n' });
                    _songs = _songs + strArrays.Length;
                    songsArray[_current] = strArrays.Length;
                    Log("Loading " + strArrays.Length + " songs...");
                    foreach (string str in strArrays)
                    {
                        if (str.Length > 1)
                        {
                            BackgroundWorker backgroundWorkerStart = new BackgroundWorker();
                            backgroundWorkerStart.DoWork += backgroundWorkerStart_DoWork;
                            backgroundWorkerStart.RunWorkerAsync(new PassArguments
                            {
                                PassedSpotCode = str,
                                PassedSession = _current
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void downloadDirOpenButton_Click(object sender, EventArgs e)
        {
            Process.Start(downloadDirTextBox.Text);
        }

        private void downloadDirBrowseButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var directory = fbd.SelectedPath;

                    downloadDirTextBox.Text = directory;
                    Settings.Default["SaveDir"] = downloadDirTextBox.Text;
                    Settings.Default.Save();
                }
            }
        }

        private void backgroundWorkerStart_DoWork(object sender, DoWorkEventArgs e)
        {
            PassArguments result = (PassArguments)e.Argument;
            e.Result = result;
            newProgressBar1.CustomText = "Loading...";
            SearchSpotify(result.PassedSpotCode, result.PassedSession);
        }

        private void SearchSpotify(string code, int session)
        {
            int num = 0;
            PassArguments spotifyName = GetSpotifyName(code);
            bool add = true;

            foreach (PassArguments[] songArrayInArray in songArray)
            {
                if (songArrayInArray != null)
                {
                    foreach (var songThing in songArrayInArray)
                    {
                        if (songThing != null)
                        {
                            if (
                                songThing.PassedFileName.Equals(spotifyName.PassedSong + " - " + spotifyName.PassedArtist))
                            {
                                //File already in list
                                _songs--;
                                songsArray[_current]--;
                                add = false;
                                Log("[Attention] The song " + spotifyName.PassedSong + " - " + spotifyName.PassedArtist +
                                    " was already added.");
                            }
                        }
                    }
                }
            }
            if (File.Exists(_dir + escapeFilename(spotifyName.PassedFileName) + ".mp3"))
            {
                //File already exsists/Downloaded
                _songs--;
                songsArray[_current]--;
                add = false;
            }

            try
            {
                if (add)
                {
                    {
                        listView1.BeginUpdate();
                        string[] row = { "Waiting", spotifyName.PassedSong + " - " + spotifyName.PassedArtist };
                        var listViewItem = new ListViewItem(row);
                        listViewItem.ImageIndex = 1;
                        listView1.Items.Add(listViewItem);
                        SetLabelVisible(false);
                        num = listViewItem.Index;
                        listView1.EndUpdate();

                        songArray[session][num] = (new PassArguments
                        {
                            PassedSong = spotifyName.PassedSong,
                            PassedArtist = spotifyName.PassedArtist,
                            PassedNum = num,
                            PassedFileName = spotifyName.PassedSong + " - " + spotifyName.PassedArtist,
                            PassedAlbum = spotifyName.PassedAlbum,
                            PassedAlbumId = spotifyName.PassedAlbumId,
                            PassedLength = spotifyName.PassedLength,
                            PassedLengthMs = spotifyName.PassedLengthMs,
                            PassedimageURL = spotifyName.PassedimageURL
                        });
                    }
                }

//                if (listView1.Items.Count == songs)
                int result = songArray[session].Count(s => s != null);
//                Log(result + " | " + songsArray[current]);
                if (result == songsArray[_current])
                {
                    Log(songsArray[_current] + " songs added. Total songs: " + _songs);
                    SearchSongArray(session);
                }
            }
            catch (Exception ex)
            {
                Log("[Error: x1] " + ex.Message + Environment.NewLine + num, true);
            }
        }

        public void SearchSongArray(int session)
        {
            foreach (PassArguments songInfo in songArray[session])
            {
                if (songInfo != null)
                {
                    try
                    {
                        DownloadMP3Clan(songInfo.PassedNum, session);
                    }
                    catch (Exception ex)
                    {
                        Log("[Error: x2] " + ex.Message + " " + songInfo.PassedNum + " | " + songInfo.PassedFileName, true);
                    }
                }
            }
            Log("All song platforms found.");
        }

        public void DownloadMP3Clan(int num, int session)
        {
            PassArguments result = songArray[session][num];
            
            EditList("Loading...", result.PassedFileName, result.PassedNum, 0);

            int highestBitrateNum = 0;
            Mp3ClanTrack highestBitrateTrack = null;
            List<Mp3ClanTrack> tracks = null;

            const string searchBaseUrl = "http://mp3clan.com/mp3_source.php?q={0}";
            var searchUrl = new Uri(string.Format(searchBaseUrl, Uri.EscapeDataString(result.PassedSong)));
            string pageSource = null;

            while (true)
            {
                try
                {
                    pageSource = new MyWebClient().DownloadString(searchUrl);
                    break;
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        EditList("Queued", result.PassedFileName, result.PassedNum, 1); //In Que so when its less than 5 it will start 
                        _youtubeNum++;
                        _totalQuedNum++;
                        var th = new Thread(unused => Search(num, session));
                        th.Start();
                        return;
                    }
                    Log("[Error: x3] " + e.Message + " | " + result.PassedFileName, true);
                }
            }
            

            IEnumerable<Mp3ClanTrack> trackResualt;
            if (Mp3ClanTrack.TryParseFromSource(pageSource, out trackResualt))
            {
                tracks = trackResualt.ToList();
                foreach (var track in tracks)
                {
                    if (track.Artist.ToLower().Trim().Contains(result.PassedArtist.ToLower()) &&
                        track.Name.ToLower().Trim().Equals(result.PassedSong.ToLower()))
                    {
                        string bitrateString = null;
                        int attempts = 0;
                        while (true)
                        {
                            try
                            {
                                bitrateString = new MyWebClient().DownloadString("http://mp3clan.com/bitrate.php?tid=" + track.Mp3ClanUrl.Replace("http://mp3clan.com/app/get.php?mp3=", ""));
                                break;
                            }
                            catch (Exception ex)
                            {
                                attempts++;
                                if (attempts > 2)
                                {
                                    Log("[Infomation: x4] " + result.PassedFileName + " " + ex.Message);
                                    bitrateString = "0 kbps";
                                    break;
                                }
                            }
                        }

                        int bitrate = Int32.Parse(GetKbps(bitrateString));
                        if (bitrate >= 192)
                        {
                            if (bitrate > highestBitrateNum)
                            {
                                double persentage = (GetLength(bitrateString) / result.PassedLength) * 100;
//                                double durationMS = TimeSpan.FromMinutes(getLength(bitrateString)).TotalMilliseconds;
//                                double persentage = (durationMS/result.passedLengthMS)*100;
                                if (persentage >= 85 && persentage <= 115)
                                {
//                                    Log("Length acc: " + string.Format("{0:0.00}", persentage) + "%");
                                    highestBitrateNum = bitrate;
                                    highestBitrateTrack = track;
                                }
                            }
                        }
                    }
                }
            }
            //=======For testing================
//            EditList("Queued", result.passedFileName, result.passedNum, 1);
//            youtubeNum++;
//            totalQuedNum++;
//            var th = new Thread(unused => Search(num));
//            th.Start();
            //==================================

            if (highestBitrateTrack == null)
            {//Youtube
                EditList("Queued", result.PassedFileName, result.PassedNum, 1);
                _youtubeNum++;
                _totalQuedNum++;
                var th = new Thread(unused => Search(num, session));
                th.Start();
            }
            else
            {//MP3Clan
                songArray[session][num].PassedTrack = highestBitrateTrack;
                EditList("Queued", result.PassedFileName, result.PassedNum, 1);
                _mp3ClanNum++;
                _totalQuedNum++;

                var th = new Thread(unused => StartDownloadMp3Clan(num, session));
                th.Start();
            }
        }

        public async void Search(int num, int session, int retry = 0, string videoID = null)
        {
            PassArguments result = songArray[session][num];

            while (_running >= maxRunning)
            {
                Thread.Sleep(500);
            }
            _totalQuedNum--;
            _running++;
            EditList("Loading...", result.PassedFileName, result.PassedNum, 0);

            string url = "";
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = ApiKey,
                ApplicationName = this.GetType().ToString()
            });

            var searchListRequest = youtubeService.Search.List("snippet");

            if (retry == 0)
            {
                searchListRequest.Q = result.PassedFileName;
            }
            else if (retry == 1)
            {
                string newName = null;
                try
                {
                    newName = result.PassedSong.Substring(0, result.PassedSong.IndexOf("-")) + " - " + result.PassedArtist;
                }
                catch (Exception ex)
                {
                    newName = result.PassedFileName;
                }
                searchListRequest.Q = newName;
            }
            else if (retry == 2)
            {
                string newName = null;
                try
                {
                    newName = result.PassedSong.Substring(0, result.PassedSong.IndexOf("(")) + " - " + result.PassedArtist;
                }
                catch (Exception ex)
                {
                    newName = result.PassedFileName;
                }
                searchListRequest.Q = newName;
            }
            else if (retry == 3)
            {
                string newName = null;
                try
                {
                    newName = result.PassedSong.Substring(0, result.PassedSong.IndexOf("/")) + " - " + result.PassedArtist;
                }
                catch (Exception ex)
                {
                    newName = result.PassedFileName;
                }
                searchListRequest.Q = newName;
            }
            else if (retry == 4)
            {
                searchListRequest.Q = result.PassedFileName;
            }
            searchListRequest.MaxResults = 5;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            List<string> videos = new List<string>();

            string[] excludeStrings = { "live", "cover" };
            string[] includeStrings = { "hd", "official" };
            string[] includeChannelStrings = { "vevo" };
            double highpersentage = 99999999.0;

            for (int i = 0; i < excludeStrings.Length; i++)
            {
                if (result.PassedFileName.ToLower().Contains(excludeStrings[i]))
                {
                    excludeStrings = excludeStrings.Where(w => w != excludeStrings[i]).ToArray();
                }
            }

            foreach (var word in excludeStrings)
            {
                if ((result.PassedFileName).ToLower().Contains(word))
                {
                    excludeStrings = excludeStrings.Where(str => str != word).ToArray();
                }
            }

            bool keepgoing = true;
            foreach (var searchResult in searchListResponse.Items)
            {
                if (keepgoing)
                {
                    if (searchResult.Id.VideoId != null && "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId != videoID)
                    {
                        if (retry == 4)
                        {
                            Log("[Infomation x13] Downloaded song may be incorrect for " + result.PassedFileName);
                            videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                            url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;
                            keepgoing = false;
                            break;
                        }
                        if (excludeStrings.Any(searchResult.Snippet.Title.ToLower().Contains) ||
                            excludeStrings.Any(searchResult.Snippet.Description.ToLower().Contains))
                        {
//                        MessageBox.Show("ERROR IT CONTAINS BAD STUFF");
                        }
                        else
                        {
                            var searchListRequest2 = youtubeService.Videos.List("contentDetails");
                            searchListRequest2.Id = searchResult.Id.VideoId;
                            var searchListResponse2 = await searchListRequest2.ExecuteAsync();

                            foreach (var searchResult2 in searchListResponse2.Items)
                            {
                                string durationTimeSpan = searchResult2.ContentDetails.Duration;
                                TimeSpan youTubeDuration = XmlConvert.ToTimeSpan(durationTimeSpan);
                                double durationMs = (youTubeDuration).TotalMilliseconds;
                                double persentage = (durationMs/result.PassedLengthMs)*100;

                                if (persentage >= 90 && persentage <= 110)
                                {
                                    double number = Math.Abs(durationMs - result.PassedLengthMs);
                                    if (number < highpersentage)
                                    {
//                                        Log(string.Format("{0:0.00}", persentage) + "% from the original and number is " + number + " | " + searchResult.Id.VideoId);
                                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                                        url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;
                                        highpersentage = number;
                                    }

                                    if (includeChannelStrings.Any(searchResult.Snippet.ChannelTitle.ToLower().Contains) || searchResult.Snippet.ChannelTitle.ToLower().Contains(result.PassedArtist.Replace(" ","").ToLower()))
                                    {
//                                        Log("using Official | " + searchResult.Id.VideoId);
                                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                                        url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;
                                        keepgoing = false;
                                        break;
                                    }

                                    if (includeStrings.Any(searchResult.Snippet.Description.ToLower().Contains) || includeStrings.Any(searchResult.Snippet.Title.ToLower().Contains))
                                    {
//                                        Log("using Original " + string.Format("{0:0.00}", persentage) + "% from the original| " + searchResult.Id.VideoId);
                                        videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                                        url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId;
                                        keepgoing = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (url != "")
            {
                songArray[session][num].PassedURL = url;
            }
            else
            {
                if (retry == 0)
                {
                    _running--;
                    _totalQuedNum++;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
                    Search(num, session, 1);
                    return;
                }
                if (retry == 1)
                {
                    _running--;
                    _totalQuedNum++;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
                    Search(num, session, 2);
                    return;
                }
                if (retry == 2)
                {
                    _running--;
                    _totalQuedNum++;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
                    Search(num, session, 3);
                    return;
                }
                if (retry == 3)
                {
                    _running--;
                    _totalQuedNum++;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
                    Search(num, session, 4);
                    return;
                }
                if (retry == 4)
                {
                    Done(result.PassedFileName, result.PassedNum, "NotFound", 5);//Youtube not found
                    Log("[Error x9] Video not found for: " + result.PassedFileName, true);
                    _running--;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
                    return;
                }
                Log("[Error x10] " + result.PassedFileName, true);
                _running--;
                _totalQuedNum++;
                if (_running < 0)
                {
                    _running = 0;
                }
                return;
            }

            songArray[session][num].YouTubeVideoQuality = YouTubeDownloader.GetYouTubeVideoUrls(result.PassedURL);
            if (songArray[session][num].YouTubeVideoQuality == null)
            {
//                Log("Cant download " + result.passedFileName + " because of age restriction on video");
                _running--;
                if (_running < 0)
                {
                    _running = 0;
                }
                _totalQuedNum++;
                Search(num, session, 0, url);
                return;
            }
            YouTubeDownload(num, session);
        }

        void YouTubeDownload(int num, int session)
        {
            PassArguments result = songArray[session][num];
//            while (true)
//            {
                try
                {
                    List<YouTubeVideoQuality> urls = result.YouTubeVideoQuality;
                    YouTubeVideoQuality highestQual = new YouTubeVideoQuality();

                    foreach (var url in urls)
                    {
                        if (url.Extention == "mp4")
                        {
                            highestQual = urls[0];
                            break;
                        }
                    }
                    string Url = "";
                    string saveTo = "";
                    try
                    {
                        YouTubeVideoQuality tempItem = highestQual;
                        Url = tempItem.DownloadUrl;
                        saveTo = escapeFilename(result.PassedFileName) + ".mp4";
                    }
                    catch (Exception ex)
                    {
                        Log("[Error x11] " + ex.InnerException, true);
                    }
                    if (result.PassedFileName == null || result.PassedNum == null)
                    {
                        MessageBox.Show("Somthing null");
                    }
                    EditList("Downloading...", result.PassedFileName, result.PassedNum, 2);
                    var folder = Path.GetDirectoryName(_tempDir + saveTo);
                    string file = Path.GetFileName(_tempDir + saveTo);

                    var client = new WebClient();
                    Uri address = new Uri(Url);
                    client.DownloadFile(address, folder + "\\" + file);
                    EditList("Converting...", result.PassedFileName, result.PassedNum, 3);
                    StartConvert(result.PassedFileName);
                    MusicTags(num, session);
                    _youtubeDownloadedNum++;
                    Done(result.PassedFileName, num);
                    _running--;
                    if (_running < 0)
                    {
                        _running = 0;
                    }
//                    break;
                }
                catch (Exception ex)
                {
                    Log("[Error x12] " + "|" + ex.Message + "| " + ex.InnerException + " | " + result.PassedFileName, true);
                }
//            }
        }


        void StartConvert(string songName)
        {
//            string output = "";
            Process _process = new Process();
            _process.StartInfo.UseShellExecute = false;
//            _process.StartInfo.RedirectStandardInput = true;
//            _process.StartInfo.RedirectStandardOutput = true;
//            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
//            _process.StartInfo.FileName = "ffmpeg";
            _process.StartInfo.FileName = _ffmpegPath;
            //            _process.StartInfo.Arguments = " -i \"" + SongName + ".mp4\" -vn -f mp3 -ab 192k \"" + SongName + ".mp3\"";
            _process.StartInfo.Arguments = " -i \"" + _tempDir + escapeFilename(songName) + ".mp4\" -vn -f mp3 -ab 320k \"" + _dir + escapeFilename(songName) + ".mp3\"";
            _process.Start();
//            _process.StandardOutput.ReadToEnd();
//            output = _process.StandardError.ReadToEnd();
            _process.WaitForExit();
            if (File.Exists(_tempDir + escapeFilename(songName) + ".mp4"))
            {
                File.Delete(_tempDir + escapeFilename(songName) + ".mp4");
            }
        }

        private void StartDownloadMp3Clan(int num, int session)
        {
            PassArguments result = songArray[session][num];

            while (_running >= maxRunning)
            {
                Thread.Sleep(500);
            }
            _totalQuedNum--;
            _running++;
            _mp3ClanDownloadedNum++;

            EditList("Downloading...", result.PassedFileName, result.PassedNum, 2);

            Uri downloadUrl = null;
            int errorTimesX6 = 0;
            while (true)
            {
                try
                {
                    downloadUrl = new Uri(result.PassedTrack.Mp3ClanUrl);
                    var fileName = _dir + "\\" + escapeFilename(result.PassedFileName) + ".mp3";

                    int errorTimes = 0;
                    while (true)
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(downloadUrl, fileName);
                            client.Dispose();
                        }

                        long fileSize = new FileInfo(fileName).Length;
                        if (fileSize < 1000)    //Possible improvement. get file size from online fore download and check that its a 5% acc to approve the download
                        {
                            errorTimes++;
                            if (errorTimes >= 3)
                            {
                                Log("[Infomation: x5] " + result.PassedFileName + " failed, re-downloading");
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    errorTimesX6++;
                    if (errorTimesX6 >= 3)
                    {
                        Log("[Infomation: x6] " + ex.Message + " | " + ex.InnerException + " | " + result.PassedFileName);
                        errorTimesX6 = 0;
                        Thread.Sleep(500);
                    }
                }
            }
            MusicTags(num, session);
            Done(result.PassedFileName, result.PassedNum);
            _running--;
            if (_running < 0)
            {
                _running = 0;
            }
        }

        public void MusicTags(int num, int session)
        {
            PassArguments result = songArray[session][num];
            try
            {
                //===edit tags====
                TagLib.File f = TagLib.File.Create(_dir + escapeFilename(result.PassedFileName) + ".mp3");
                f.Tag.Clear();
                f.Tag.AlbumArtists = new string[1] { result.PassedArtist };
                f.Tag.Performers = new string[1] { result.PassedArtist };
                f.Tag.Title = result.PassedSong;
                f.Tag.Album = result.PassedAlbum;
//                //                Log(result.passedFileName + " and " + result.passedAlbumID);
                Image currentImage = GetAlbumArt(num, session);
                Picture pic = new Picture();
                pic.Type = PictureType.FrontCover;
                pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                pic.Description = "Cover";
                MemoryStream ms = new MemoryStream();
                currentImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); // <-- Error doesn't occur anymore
                ms.Position = 0;
                pic.Data = ByteVector.FromStream(ms);
                f.Tag.Pictures = new IPicture[1] { pic };
                f.Save();
                ms.Close();
            }
            catch (Exception ex)
            {
                Log("[Error: x7] " + ex.Message + Environment.NewLine + Environment.NewLine + result.PassedFileName, true);
            }
        }

        Bitmap GetAlbumArt(int num, int session)
        {
            PassArguments result = songArray[session][num];
            Bitmap bitmap2;
            try
            {
                WebRequest request = WebRequest.Create(result.PassedimageURL);
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                bitmap2 = new Bitmap(responseStream);
            }
            catch (Exception ex)
            {
                Log("[Error: x8] " + ex.Message, true);
                bitmap2 = null;
            }

            return bitmap2;
        }

        public string GetKbps(string s)
        {
            int l = s.IndexOf(" kbps");
            if (l > 0)
            {
                return s.Substring(0, l);
            }
            return "";
        }

        public double GetLength(string s)
        {
            int pFrom = s.IndexOf("<br>") + "<br>".Length;
            int pTo = s.IndexOf(" min");

            string result = s.Substring(pFrom, pTo - pFrom).Replace(":", ".");
            double songLength = double.Parse(result, CultureInfo.InvariantCulture);
            return songLength;
        }

        private void Done(string passedFileName, int passedNum, string message = "Done!", int num = 4)
        {
            EditList(message, passedFileName, passedNum, num);
            _totalFinished++;

            newProgressBar1.Maximum = _songs;
            newProgressBar1.Value = _totalFinished;
            newProgressBar1.CustomText = newProgressBar1.Value + "/" + newProgressBar1.Maximum;
//            double percent = ((TotalFinished - 1)/songs)*100;
//            harrProgressBar1.FillDegree = (int)percent;
            if (newProgressBar1.Value == newProgressBar1.Maximum)
            {
                newProgressBar1.CustomText = "Done!";
            }
        }

        public double MillisecondsTimeSpanToHms(double s)
        {
            s = TimeSpan.FromMilliseconds(s).TotalSeconds;
            var h = Math.Floor(s / 3600); //Get whole hours
            s -= h * 3600;
            var m = Math.Floor(s / 60); //Get remaining minutes
            s -= m * 60;
            s = Math.Round(s);
            string stringLength = (m + "." + s);
            return double.Parse(stringLength, CultureInfo.InvariantCulture);
        }

        private delegate void SetLabelVisibleDelegate(bool status);
        private void SetLabelVisible(bool status)
        {
            if (label3.InvokeRequired)
                label3.Invoke(new SetLabelVisibleDelegate(SetLabelVisible), status);
            else
                label3.Visible = status;
        }

        public PassArguments GetSpotifyName(string query)//Uptaded to use new API
        {
            string getData = null;
            while (true)
            {
                try
                {
                    WebClient c = new WebClient();
                    getData = c.DownloadString("https://api.spotify.com/v1/tracks/" + query);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }
            
            JObject o = JObject.Parse(getData);
            string name = o["name"].ToString();
            string artist = o["artists"][0]["name"].ToString();
            string albumId = o["artists"][0]["id"].ToString();
            string album = o["album"]["name"].ToString();
            string imageURL = o["album"]["images"][0]["url"].ToString();
            double lengthMs = double.Parse(o["duration_ms"].ToString());
            double length = MillisecondsTimeSpanToHms(double.Parse(o["duration_ms"].ToString()));

            name = Encoding.UTF8.GetString(Encoding.Default.GetBytes(name));
            artist = Encoding.UTF8.GetString(Encoding.Default.GetBytes(artist));
            album = Encoding.UTF8.GetString(Encoding.Default.GetBytes(album));

            PassArguments pass = new PassArguments
            {
                PassedSong = name,
                PassedArtist = artist,
                PassedAlbum = album,
                PassedAlbumId = albumId,
                PassedFileName = name + " - " + artist,
                PassedLength = length,
                PassedLengthMs = lengthMs,
                PassedimageURL = imageURL
            };
            return pass;
        }

        string escapeFilename(string name)
        {
            name = name.Replace("/", "_");
            name = name.Replace("\\", "_");
            name = name.Replace("<", "_");
            name = name.Replace(">", "_");
            name = name.Replace(":", "_");
            name = name.Replace("\"", "_");
            name = name.Replace("|", "_");
            name = name.Replace("?", "_");
            name = name.Replace("*", "_");
            return name;
        }

        public void EditList(string message, string fileName, int number, int image)
        {
            var data = new ListViewData {Message = message, FileName = fileName, Number = number, Image = image};
            downloadData[number] = (data);
        }
       
        private void timer1_Tick(object sender, EventArgs e)
        {
            listView1.BeginUpdate();
            foreach (var downloadInfo in downloadData)
            {
                if (downloadInfo != null)
                {
                    string[] row = { downloadInfo.Message, downloadInfo.FileName };
                    var listViewItem = new ListViewItem(row);
                    listViewItem.ImageIndex = downloadInfo.Image;
                    if (listView1.Items[downloadInfo.Number] == null)
                    {
                        listView1.Items.Add(listViewItem);
                    }
                    else
                    {
                        listView1.Items[downloadInfo.Number] = (listViewItem);
                    }
                }
               
            }
            listView1.EndUpdate();
        }

        private bool logStyle = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (logStyle)
            {
                button1.Text = "Log >";
                Size = new Size(597, 448);
                logStyle = false;
            }
            else
            {
                button1.Text = "Log <";
                Size = new Size(1245, 448);
                logStyle = true;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _debug = checkBox1.Checked;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label9.Text = _running.ToString();
            label5.Text = _youtubeNum.ToString();
            label4.Text = _mp3ClanNum.ToString();
            label17.Text = _totalQuedNum.ToString();
            label10.Text = _mp3ClanDownloadedNum.ToString();
            label19.Text = _totalFinished.ToString();
            label15.Text = _youtubeDownloadedNum.ToString();
        }
    }
}
