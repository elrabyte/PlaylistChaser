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
            NotChecked = 100,
            NotAvailable = 101
        }
        public enum PlaylistSongStates
        {
            Added = 210,
            NotAdded = 200,
        }
        public enum Entity
        {
            PlaylistSong,
            Playlist,
            Song
        }
    }
}
