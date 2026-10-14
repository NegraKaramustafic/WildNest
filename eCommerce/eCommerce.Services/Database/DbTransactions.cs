namespace eCommerce.Services.Database
{
    internal static class DbTransactions
    {
        public static async Task ExecuteAsync(ECommerceDbContext dbContext, Func<Task> action)
        {
            if (dbContext.Database.CurrentTransaction != null)
            {
                await action();
                return;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            await action();
            await transaction.CommitAsync();
        }
    }
}
