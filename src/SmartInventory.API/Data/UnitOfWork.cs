using SmartInventory.Domain.Interfaces;

namespace SmartInventory.API.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IProductRepository Products { get; }
    public IWarehouseRepository Warehouses { get; }
    public IInventoryTransactionRepository Transactions { get; }

    public UnitOfWork(
        AppDbContext context,
        IProductRepository products,
        IWarehouseRepository warehouses,
        IInventoryTransactionRepository transactions)
    {
        _context = context;
        Products = products;
        Warehouses = warehouses;
        Transactions = transactions;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}

