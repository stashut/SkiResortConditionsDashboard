namespace SkiResort.Domain.Entities;

// Criterion 4: Represents a user favorite resort (stored in DynamoDB)
public class UserFavorite
{
    public string UserId { get; set; } = string.Empty;
    public Guid ResortId { get; set; }
}

