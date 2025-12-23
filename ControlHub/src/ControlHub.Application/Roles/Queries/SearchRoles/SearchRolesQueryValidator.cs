using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace ControlHub.Application.Roles.Queries.SearchRoles
{
    public class SearchRolesQueryValidator : AbstractValidator<SearchRolesQuery>
    {
        public SearchRolesQueryValidator()
        {
            RuleFor(x => x.pageIndex)
                .GreaterThanOrEqualTo(1).WithMessage("Page index must be at least 1.");

            RuleFor(x => x.pageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

            RuleForEach(x => x.conditions)
                .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.")
                .When(x => x.conditions != null);
        }
    }
}
