using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public abstract class BaseCRUDService<TEntity, TResponse, TSearch, TInsertRequest, TUpdateRequest>
        : BaseReadService<TEntity, TResponse, TSearch>
        where TEntity : class
        where TSearch : BaseSearchObject, new()
    {
        protected readonly IValidator<TInsertRequest> _insertValidator;
        protected readonly IValidator<TUpdateRequest> _updateValidator;

        protected BaseCRUDService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<TInsertRequest> insertValidator,
            IValidator<TUpdateRequest> updateValidator,
            ILoggerFactory loggerFactory) : base(mapper, dbContext, loggerFactory)
        {
            _insertValidator = insertValidator;
            _updateValidator = updateValidator;
        }

        protected virtual TEntity MapInsertRequestToEntity(TInsertRequest request)
        {
            var entity = _mapper.Map<TEntity>(request ?? throw new ArgumentNullException(nameof(request)));
            return entity;
        }

        protected virtual void MapUpdateRequestToEntity(TUpdateRequest request, TEntity entity)
        {
            _mapper.Map(request, entity);
        }

        public virtual async Task<TResponse> InsertAsync(TInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (validationResult.IsValid == false)
            {
                var errors = validationResult.Errors.Select(e => _mapper.Map<ValidationFailure>(e));
                throw new FluentValidation.ValidationException(errors);
            }

            var entity = MapInsertRequestToEntity(request);

            var createdAtProperty = entity.GetType().GetProperty("CreatedAt");
            if (createdAtProperty?.CanWrite == true)
            {
                createdAtProperty.SetValue(entity, DateTime.UtcNow);
            }

            _dbContext.Set<TEntity>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TResponse>(entity);
        }

        public virtual async Task<TResponse> UpdateAsync(int id, TUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (validationResult.IsValid == false)
            {
                var errors = validationResult.Errors.Select(e => _mapper.Map<ValidationFailure>(e));
                throw new FluentValidation.ValidationException(errors);
            }

            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            MapUpdateRequestToEntity(request, entity);

            var updatedAtProperty = entity.GetType().GetProperty("UpdatedAt");
            if (updatedAtProperty?.CanWrite == true)
            {
                updatedAtProperty.SetValue(entity, DateTime.UtcNow);
            }

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TResponse>(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
