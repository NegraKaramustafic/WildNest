using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using eCommerce.Model;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;

namespace eCommerce.Services
{
    public abstract class BaseReadService<TEntity, TResponse, TSearch> : IBaseReadService<TResponse, TSearch>
        where TEntity : class
        where TSearch : BaseSearchObject, new()
    {
        private static readonly Regex SortClausePattern = new(
            @"^[A-Za-z_][A-Za-z0-9_]*(\s+(asc|desc))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        protected readonly MapsterMapper.IMapper _mapper;
        protected readonly ECommerceDbContext _dbContext;
        protected readonly ILogger _logger;

        protected BaseReadService(MapsterMapper.IMapper mapper, ECommerceDbContext dbContext, ILoggerFactory loggerFactory)
        {
            _mapper = mapper;
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// Shown when a record is missing. Subclasses set the language of the screen that loads or edits the record.
        /// </summary>
        protected virtual string NotFoundMessage => "The requested record was not found.";

        [DoesNotReturn]
        protected void ThrowNotFound(int id)
        {
            ThrowNotFound(typeof(TEntity).Name, id, NotFoundMessage);
        }

        [DoesNotReturn]
        protected void ThrowNotFound(string entityName, object id, string message)
        {
            _logger.LogWarning("{Entity} {Id} was not found.", entityName, id);
            throw new NotFoundException(message);
        }

        protected abstract IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, TSearch? search);

        protected abstract IQueryable<TEntity> ApplyDefaultOrder(IQueryable<TEntity> query);

        public virtual async Task<PageResult<TResponse>> GetAllAsync(TSearch? search = null)
        {
            search ??= new TSearch();
            EnsureValidPaging(search);

            IQueryable<TEntity> query = _dbContext.Set<TEntity>().AsNoTracking();
            query = await IncludeRelatedEntitiesAsync(search, query);
            query = ApplyFilters(query, search);

            int? totalCount = null;
            if (search.IncludeTotalCount == true)
            {
                totalCount = await query.CountAsync();
            }

            query = ApplyOrdering(query, search.SortBy);

            var page = search.Page ?? Pagination.DefaultPage;
            var pageSize = search.PageSize ?? Pagination.DefaultPageSize;
            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            var entities = await query.ToListAsync();
            var list = entities.Select(item => _mapper.Map<TResponse>(item)).ToList();

            return new PageResult<TResponse>
            {
                Items = list,
                TotalCount = totalCount
            };
        }

        protected virtual Task<IQueryable<TEntity>> IncludeRelatedEntitiesAsync(TSearch? search, IQueryable<TEntity> query = null!)
        {
            return Task.FromResult(query);
        }

        public virtual async Task<TResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => EF.Property<int>(item, "Id") == id);

            if (entity == null)
            {
                ThrowNotFound(id);
            }

            return _mapper.Map<TResponse>(entity);
        }

        private static void EnsureValidPaging(TSearch search)
        {
            var page = search.Page ?? Pagination.DefaultPage;
            var pageSize = search.PageSize ?? Pagination.DefaultPageSize;

            if (page < 1)
            {
                throw new ClientException("Page mora biti najmanje 1.");
            }

            if (pageSize < 1 || pageSize > Pagination.MaxPageSize)
            {
                throw new ClientException($"PageSize mora biti između 1 i {Pagination.MaxPageSize}.");
            }

            search.Page = page;
            search.PageSize = pageSize;
        }

        private IQueryable<TEntity> ApplyOrdering(IQueryable<TEntity> query, string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return ApplyDefaultOrder(query);
            }

            var clauses = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (clauses.Length == 0)
            {
                throw new ClientException("SortBy is not valid.");
            }

            var ordering = new List<string>(clauses.Length);
            foreach (var clause in clauses)
            {
                if (!SortClausePattern.IsMatch(clause))
                {
                    throw new ClientException("SortBy is not valid.");
                }

                var parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var property = typeof(TEntity).GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                {
                    throw new ClientException("SortBy is not valid.");
                }

                var direction = parts.Length == 2 ? parts[1].ToLowerInvariant() : "asc";
                ordering.Add($"{property.Name} {direction}");
            }

            return query.OrderBy(string.Join(", ", ordering));
        }
    }
}
