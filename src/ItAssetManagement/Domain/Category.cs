namespace ItAssetManagement.Domain;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Asset> Assets { get; set; } = [];
}
