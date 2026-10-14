using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PlaylistChaser.Data;

public enum Source { Youtube = 1, Spotify = 2 }
public enum PlaylistKind { Simple = 1, Combined = 2 }
// Origin = the song was imported from this service; it is the song's identity there and can't be edited.
public enum LinkStatus { Origin = 1, Found = 2, NotFound = 3, Manual = 4 }
public enum JobKind { Pull = 1, Push = 2, SyncAll = 3 }
public enum JobStatus { Queued = 1, Running = 2, Done = 3, Failed = 4 }

public class AppUser : IdentityUser { }

public class SourceAccount
{
    public string UserId { get; set; } = "";
    public Source Source { get; set; }
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public string? ExternalUserId { get; set; }
    public string? DisplayName { get; set; }
    public bool NeedsReconnect { get; set; }
}

public class Playlist
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Creator { get; set; }
    public string? ThumbnailUrl { get; set; }
    public PlaylistKind Kind { get; set; }
    public Source? MainSource { get; set; }
    public DateTime? LastPulledAt { get; set; }
    public List<PlaylistLink> Links { get; set; } = [];
}

public class PlaylistLink
{
    public int PlaylistId { get; set; }
    public Source Source { get; set; }
    public string ExternalId { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsMine { get; set; }
    public DateTime? LastPushedAt { get; set; }
}

public class CombinedMember
{
    public int CombinedId { get; set; }
    public int MemberId { get; set; }
}

public class Song
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Artist { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public List<SongLink> Links { get; set; } = [];
}

public class SongLink
{
    public int SongId { get; set; }
    public Song Song { get; set; } = null!;
    public Source Source { get; set; }
    public string? ExternalId { get; set; }
    public string? Url { get; set; }
    public LinkStatus Status { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class PlaylistSong
{
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public Song Song { get; set; } = null!;
    public DateTime AddedAt { get; set; }
}

// Remembers what push already added, so a track removed by hand on the target is not re-added.
public class PushedSong
{
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public Source Source { get; set; }
}

public class Job
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public JobKind Kind { get; set; }
    public int? PlaylistId { get; set; }
    public Source? Target { get; set; }
    public JobStatus Status { get; set; }
    public int Done { get; set; }
    public int Total { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class AppDb(DbContextOptions<AppDb> options, IDataProtectionProvider dataProtection)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<SourceAccount> SourceAccounts => Set<SourceAccount>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistLink> PlaylistLinks => Set<PlaylistLink>();
    public DbSet<CombinedMember> CombinedMembers => Set<CombinedMember>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongLink> SongLinks => Set<SongLink>();
    public DbSet<PlaylistSong> PlaylistSongs => Set<PlaylistSong>();
    public DbSet<PushedSong> PushedSongs => Set<PushedSong>();
    public DbSet<Job> Jobs => Set<Job>();

    // all times are stored as UTC; SQLite forgets the kind, so mark them UTC on read (Postgres already does)
    protected override void ConfigureConventions(ModelConfigurationBuilder c) =>
        c.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    private class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
        v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        var protector = dataProtection.CreateProtector("SourceTokens");
        b.Entity<SourceAccount>(e =>
        {
            e.HasKey(a => new { a.UserId, a.Source });
            e.HasOne<AppUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(a => a.AccessToken).HasConversion(v => protector.Protect(v), v => protector.Unprotect(v));
            e.Property(a => a.RefreshToken).HasConversion(v => protector.Protect(v), v => protector.Unprotect(v));
        });

        b.Entity<Playlist>().HasOne<AppUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PlaylistLink>(e =>
        {
            e.HasKey(l => new { l.PlaylistId, l.Source });
            e.HasOne<Playlist>().WithMany(p => p.Links).HasForeignKey(l => l.PlaylistId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CombinedMember>(e =>
        {
            e.HasKey(m => new { m.CombinedId, m.MemberId });
            e.HasOne<Playlist>().WithMany().HasForeignKey(m => m.CombinedId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Playlist>().WithMany().HasForeignKey(m => m.MemberId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Song>().HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SongLink>(e =>
        {
            e.HasKey(l => new { l.SongId, l.Source });
            e.HasOne(l => l.Song).WithMany(s => s.Links).HasForeignKey(l => l.SongId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => new { l.Source, l.ExternalId });
        });
        b.Entity<PlaylistSong>(e =>
        {
            e.HasKey(ps => new { ps.PlaylistId, ps.SongId });
            e.HasOne<Playlist>().WithMany().HasForeignKey(ps => ps.PlaylistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ps => ps.Song).WithMany().HasForeignKey(ps => ps.SongId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PushedSong>(e =>
        {
            e.HasKey(p => new { p.PlaylistId, p.SongId, p.Source });
            e.HasOne<Playlist>().WithMany().HasForeignKey(p => p.PlaylistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Song>().WithMany().HasForeignKey(p => p.SongId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Job>(e =>
        {
            e.HasOne<AppUser>().WithMany().HasForeignKey(j => j.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(j => new { j.UserId, j.CreatedAt });
        });
    }
}

public static class OwnedQueries
{
    public static IQueryable<Playlist> Owned(this IQueryable<Playlist> q, string userId) => q.Where(p => p.UserId == userId);
    public static IQueryable<Song> Owned(this IQueryable<Song> q, string userId) => q.Where(s => s.UserId == userId);
}
