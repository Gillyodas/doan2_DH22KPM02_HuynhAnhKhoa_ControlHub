using FluentValidation;

namespace ControlHub.Application.Identity.Queries.GetUsers
{
    public class GetUsersValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page Size must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Page Size must not exceed 100.");
        }
    }
}
