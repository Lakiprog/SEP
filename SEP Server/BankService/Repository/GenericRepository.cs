using BankService.Data;
using BankService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankService.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly BankServiceDbContext _context;

        public GenericRepository(BankServiceDbContext context)
        {
            _context = context;
        }
        public async Task<T> Add(T entity)
        {
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Delete(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
            {
                return entity;
            }

            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<T> Get(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<T> Update(int id, T entity)
        {
            var findEntity = await _context.Set<T>().FindAsync(id);
            if (findEntity == null)
            {
                return findEntity;
            }

            _context.Entry(findEntity).CurrentValues.SetValues(entity);

            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
