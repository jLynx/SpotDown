using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotDown_V2.Classes
{
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
}
