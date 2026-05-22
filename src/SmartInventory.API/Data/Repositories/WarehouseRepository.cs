using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.API.Data.Repositories;

public class WarehouseRepository : Repository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().Where(w => w.IsActive).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public override async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync(cancellationToken);
}
