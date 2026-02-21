using ControlHub.Domain.Identity.Identifiers;
using ControlHub.SharedKernel.Results;
using Microsoft.Extensions.Caching.Memory;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;

namespace ControlHub.Infrastructure.Accounts.Repositories;

public class CachedIdentifierConfigRepository : AppIdentifierConfigRepository
{
    private readonly AppIdentifierConfigRepository _decorated;
    private readonly IMemoryCache _memoryCache;

    public CachedIdentifierConfigRepository(AppIdentifierConfigRepository decorated, IMemoryCache memoryCache)
    {
        _decorated = decorated;
        _memoryCache = memoryCache;
    }

    public async Task<Result> AddAsync(IdentifierConfig config, CancellationToken ct)
    {
        var result = await _decorated.AddAsync(config, ct);
        if (result.IsSuccess)
        {
            // Invalidate relevant cache entries
            _memoryCache.Remove($"IdentifierConfig-Name-{config.Name}");
            // We might also need to invalidate the "Active" list, as a new config might be active
            _memoryCache.Remove("IdentifierConfig-Active");
        }
        return result;
    }

    public async Task<Result<IEnumerable<IdentifierConfig>>> GetActiveConfigsAsync(CancellationToken ct)
    {
        return await _memoryCache.GetOrCreateAsync(
            "IdentifierConfig-Active",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                return _decorated.GetActiveConfigsAsync(ct);
            }) ?? Result<IEnumerable<IdentifierConfig>>.Failure(SharedKernel.Common.Errors.Error.Failure("CacheError", "Failed to retrieve from cache"));
    }

    public async Task<Result<IEnumerable<IdentifierConfig>>> GetDeactiveConfigsAsync(CancellationToken ct)
    {
        return await _decorated.GetDeactiveConfigsAsync(ct);
    }

    public async Task<Result<IdentifierConfig>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        string key = $"IdentifierConfig-Id-{id}";
        return await _memoryCache.GetOrCreateAsync(
             key,
             entry =>
             {
                 entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
                 return _decorated.GetByIdAsync(id, ct);
             }) ?? Result<IdentifierConfig>.Failure(SharedKernel.Common.Errors.Error.Failure("CacheError", "Failed to retrieve from cache"));
    }

    public async Task<Result<IdentifierConfig>> GetByNameAsync(string name, CancellationToken ct)
    {
        string key = $"IdentifierConfig-Name-{name}";
        return await _memoryCache.GetOrCreateAsync(
             key,
             entry =>
             {
                 entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
                 return _decorated.GetByNameAsync(name, ct);
             }) ?? Result<IdentifierConfig>.Failure(SharedKernel.Common.Errors.Error.Failure("CacheError", "Failed to retrieve from cache"));
    }
}
