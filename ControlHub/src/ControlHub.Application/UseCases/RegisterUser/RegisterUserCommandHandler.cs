using MediatR;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.UseCases.RegisterUser
{
    public record RegisterUserCommand(string Email, string Password) : IRequest<Result>;
    public class RegisterUserCommandHandler
    {
    }
}

/*public record RegisterUserCommand(string Email, string Password) : IRequest<Result>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return Result.Failure("Email already registered.");

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var user = new User(request.Email, hashedPassword);

        await _userRepository.AddAsync(user);
        return Result.Success();
    }
}
*/
