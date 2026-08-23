using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(Student student);
}
