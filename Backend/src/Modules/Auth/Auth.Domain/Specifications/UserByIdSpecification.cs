namespace Auth.Domain.Specifications;

public class UserByIdSpecification(int userId) : BaseSpecification<User>(u => u.Id == userId)
{
    public UserByIdSpecification(int userId, bool includeInformation) : this(userId)
    {
        if (includeInformation)
            AddInclude(u => u.Information!);
    }
}
