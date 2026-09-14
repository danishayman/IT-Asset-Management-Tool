namespace ItAssetManagement.Services;

/// <summary>
/// Wraps the hashing algorithm behind an interface so the choice of algorithm is a
/// DI registration rather than a detail smeared across the controllers.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
