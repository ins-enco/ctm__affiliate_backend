namespace Auth.Domain.Specifications;

public class UserByEmailSpecification(string email) : BaseSpecification<User>(u => u.Email == email);
