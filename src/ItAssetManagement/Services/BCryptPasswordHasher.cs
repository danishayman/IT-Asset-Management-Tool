namespace ItAssetManagement.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    // BCrypt generates a random salt per call and embeds it in the output string,
    // so no separate salt column is needed. Cost 11 is a reasonable 2020s default.
    private const int WorkFactor = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        // A malformed or truncated hash in the database must read as "wrong password",
        // not as a 500. This is the one place where swallowing is the correct behaviour,
        // and it is narrowed to the specific exception type BCrypt throws.
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
