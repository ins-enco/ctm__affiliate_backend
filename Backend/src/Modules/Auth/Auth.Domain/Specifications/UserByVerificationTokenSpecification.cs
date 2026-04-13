namespace Auth.Domain.Specifications;

public class UserByVerificationTokenSpecification(string token) : BaseSpecification<EmailVerificationToken>(t => t.Token == token)
{
    public UserByVerificationTokenSpecification(string token, bool includeUser) : this(token)
    {
        if (includeUser)
            AddInclude(t => t.User);
    }
}
