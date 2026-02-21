using ControlHub.Application.Users.DTOs;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Users.Repositories
{
    internal class UserQueries : IUserQueries
    {
        private readonly AppDbContext _db;

        public UserQueries(AppDbContext db)
        {
            _db = db;
        }
        public async Task<User> GetByAccountId(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccId == id);
        }

        public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var query = from u in _db.Users.AsNoTracking()
                        join a in _db.Accounts.AsNoTracking() on u.AccId equals a.Id
                        join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                        where u.Id == id
                        select new { u, a, r };

            var result = await query.FirstOrDefaultAsync(cancellationToken);

            if (result == null) return null;

            var email = result.a.Identifiers.FirstOrDefault(i => i.Type == ControlHub.Domain.Identity.Enums.IdentifierType.Email)?.Value;

            return new UserDto(
                result.u.Id,
                result.u.Username ?? string.Empty,
                email,
                result.u.FirstName,
                result.u.LastName,
                result.u.PhoneNumber,
                result.a.IsActive,
                result.r.Id,
                result.r.Name
            );
        }

        public async Task<UserDto?> GetDtoByAccountId(Guid accountId, CancellationToken cancellationToken)
        {
            var query = from u in _db.Users.AsNoTracking()
                        join a in _db.Accounts.AsNoTracking() on u.AccId equals a.Id
                        join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                        where u.AccId == accountId
                        select new { u, a, r };

            var result = await query.FirstOrDefaultAsync(cancellationToken);

            if (result == null) return null;

            var email = result.a.Identifiers.FirstOrDefault(i => i.Type == ControlHub.Domain.Identity.Enums.IdentifierType.Email)?.Value;

            return new UserDto(
                result.u.Id,
                result.u.Username ?? string.Empty,
                email,
                result.u.FirstName,
                result.u.LastName,
                result.u.PhoneNumber,
                result.a.IsActive,
                result.r.Id,
                result.r.Name
            );
        }

        public async Task<PaginatedResult<UserDto>> GetPaginatedAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken)
        {
            var query = from u in _db.Users.AsNoTracking()
                        join a in _db.Accounts.AsNoTracking() on u.AccId equals a.Id
                        join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                        where !u.IsDeleted // Assuming we filter deleted
                        select new { u, a, r };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(x =>
                    (x.u.Username != null && x.u.Username.ToLower().Contains(term)) ||
                    (x.u.FirstName != null && x.u.FirstName.ToLower().Contains(term)) ||
                    (x.u.LastName != null && x.u.LastName.ToLower().Contains(term)) ||
                    x.a.Identifiers.Any(i => i.Value.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(x => new UserDto(
                x.u.Id,
                x.u.Username ?? string.Empty,
                x.a.Identifiers.FirstOrDefault(i => i.Type == ControlHub.Domain.Identity.Enums.IdentifierType.Email)?.Value,
                x.u.FirstName,
                x.u.LastName,
                x.u.PhoneNumber,
                x.a.IsActive,
                x.r.Id,
                x.r.Name
            )).ToList();

            return PaginatedResult<UserDto>.Create(dtos, totalCount, page, pageSize);
        }
    }
}
