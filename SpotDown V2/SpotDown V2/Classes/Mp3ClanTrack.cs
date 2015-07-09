using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class Mp3ClanTrack : IEnumerable
    {
        /// <summary>
        /// The track's artist
        /// </summary>
        public string Artist { get; private set; }

        /// <summary>
        /// The track's Mp3Clan download URL
        /// </summary>
        public string Mp3ClanUrl { get; private set; }

        /// <summary>
        /// The track's name
        /// </summary>
        public string Name { get; private set; }

        public Mp3ClanTrack(string artist, string name, string mp3ClanUrl)
        {
            Artist = artist;
            Mp3ClanUrl = mp3ClanUrl;
            Name = name;
        }

        /// <summary>
        /// Parses Mp3ClanTrack objects from a search page result
        /// </summary>
        /// <param name="source">a page source of http://mp3clan.com/mp3_source.php?q=query</param>
        /// <param name="result">out parameter to store the Mp3ClanTrack objects in</param>
        /// <returns>boolean indicating success</returns>
        public static bool TryParseFromSource(string source, out IEnumerable<Mp3ClanTrack> result)
        {
            //TODO: Learn RegEx properly D:
            const string trackPattern = "((http)(?::\\/{2}[\\w]+)(?:[\\/|\\.]?)(?:[^\\s\"]*)).*?(download).*?(\".*?\")";
            const string downloadBaseUrl = "http://mp3clan.com/app/get.php?mp3=";

            var matches = Regex.Matches(source, trackPattern);

            if (matches.Count < 1)
            {
                result = null;

                return false;
            }

            result = from match in matches.Cast<Match>()
                     where match.Groups.Count >= 4
                     let trackInfo = match.Groups[4].Value.Replace('_', ' ').Trim('"')
                     let split = trackInfo.Split('-')
                     select new Mp3ClanTrack(split[0], split[1],
                                             downloadBaseUrl + match.Groups[1].Value.Split('&')[2].Split('=')[1]);

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns>Returns the track info in "artist - name" format</returns>
        public override string ToString()
        {
            return string.Format("{0} - {1}", Artist, Name);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}