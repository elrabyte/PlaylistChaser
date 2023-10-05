namespace PlaylistChaser.Web.Util
{
    public class BuiltInIds
    {
        public enum PLaylistTypes
        {
            Simple = 1,
            Combined = 2
        }
        public enum Sources
        {
            Youtube = 1,
            Spotify = 2
        }

        public enum SongStates
        {
            NotAvailable = 100,
            NotChecked = 101,
            Available = 110,
            MaybeAvailable = 111
        }
        public enum PlaylistSongStates
        {
            Added = 210,
            NotAdded = 200,
            Deleted = 205,
        }
        public enum Entity
        {
            PlaylistSong,
            Playlist,
            Song
        }
    }
}
