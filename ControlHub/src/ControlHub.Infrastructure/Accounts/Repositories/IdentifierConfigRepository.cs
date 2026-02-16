using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;
using ControlHub.Domain.Identity.Identifiers;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Accounts.Repositories
{
    public class IdentifierConfigRepository : AppIdentifierConfigRepository
    {
        private readonly AppDbContext _db;

        public IdentifierConfigRepository(AppDbContext db) => _db = db;

        public async Task<Result<IdentifierConfig>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var config = await _db.IdentifierConfigs
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (config == null)
            {
                return Result<IdentifierConfig>.Failure(
                    new Error("IdentifierConfig.NotFound", $"Identifier configuration with ID {id} not found"));
            }

            return Result<IdentifierConfig>.Success(config);
        }

        public async Task<Result<IdentifierConfig>> GetByNameAsync(string name, CancellationToken ct)
        {
            var config = await _db.IdentifierConfigs
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Name == name, ct);

            if (config == null)
            {
                return Result<IdentifierConfig>.Failure(
                    new Error("IdentifierConfig.NotFound", $"Identifier configuration with name '{name}' not found"));
            }

            return Result<IdentifierConfig>.Success(config);
        }

        public async Task<Result<IEnumerable<IdentifierConfig>>> GetActiveConfigsAsync(CancellationToken ct)
        {
            var configs = await _db.IdentifierConfigs
                .Include(c => c.Rules)
                .Where(c => c.IsActive)
                .ToListAsync(ct);

            return Result<IEnumerable<IdentifierConfig>>.Success(configs);
        }

        public async Task<Result<IEnumerable<IdentifierConfig>>> GetDeactiveConfigsAsync(CancellationToken ct)
        {
            var configs = await _db.IdentifierConfigs
                .Include(c => c.Rules)
                .Where(c => c.IsActive == false)
                .ToListAsync(ct);

            return Result<IEnumerable<IdentifierConfig>>.Success(configs);
        }

        public async Task<Result> AddAsync(IdentifierConfig config, CancellationToken ct)
        {
            try
            {
                await _db.IdentifierConfigs.AddAsync(config, ct);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    new Error("IdentifierConfig.AddFailed", $"Failed to add identifier configuration: {ex.Message}"));
            }
        }
    }
}
