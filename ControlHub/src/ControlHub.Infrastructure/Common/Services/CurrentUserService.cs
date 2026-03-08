using System.Security.Claims;
using ControlHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ControlHub.Infrastructure.Common.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return id != null ? Guid.Parse(id) : Guid.Empty;
            }
        }
    }
}
