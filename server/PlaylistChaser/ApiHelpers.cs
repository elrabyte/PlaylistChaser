using System.Security.Claims;
using PlaylistChaser.Data;

namespace PlaylistChaser;

public static class ApiHelpers
{
    public static string Id(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public static bool TryParseSource(string value, out Source source) =>
        Enum.TryParse(value, ignoreCase: true, out source) && Enum.IsDefined(source);
}
