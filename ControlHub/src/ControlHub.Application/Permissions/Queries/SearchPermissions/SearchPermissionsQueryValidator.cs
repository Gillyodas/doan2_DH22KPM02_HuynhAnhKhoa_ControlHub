using FluentValidation;

namespace ControlHub.Application.Permissions.Queries.SearchPermissions
{
    public class SearchPermissionsQueryValidator : AbstractValidator<SearchPermissionsQuery>
    {
        public SearchPermissionsQueryValidator()
        {
            RuleFor(x => x.PageIndex)
                .GreaterThanOrEqualTo(1).WithMessage("Page index must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

            RuleForEach(x => x.Conditions)
                .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.")
                .When(x => x.Conditions != null);
        }
    }
}
