using FluentValidation;

namespace ControlHub.Application.AccessControl.Queries.GetUserRoles
{
    public class GetUserRolesValidator : AbstractValidator<GetUserRolesQuery>
    {
        public GetUserRolesValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User Id is required.");
        }
    }
}
