using Microsoft.EntityFrameworkCore;
using Transcription.DAL.Exceptions;
using Transcription.DAL.Models;

namespace Transcription.DAL.Repositories;

public class UserRepository
{
    public UserRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet =  Context.Set<User>();
    }
    public WbContext Context { get; }

    private DbSet<User> DbSet { get; }
    
    public async Task<User> GetAsync(long id)
    {
        var user = await Context.Users.Include(x=> x.ChatState).FirstOrDefaultAsync(x => x.UserChatId == id);

        if (user == null)
        {
            throw new NoValuesFoundException();
        }

        Context.Entry(user).State = EntityState.Detached;
        
        return user;
    }

    public async Task<List<User>> GetAllActiveAsync()
    {
        var users = await Context.Users.Where(x => x.IsActive).ToListAsync();
        return users;
    }
    
    public async Task Insert(User user)
    {
        user.LastUpdate = DateTimeOffset.UtcNow;
        
        Context.Users.Add(user);

        await Context.SaveChangesAsync();
    }
    
    public async Task Update(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        Context.Entry(user).State = EntityState.Modified;
        await Context.SaveChangesAsync();
        Context.Entry(user).State = EntityState.Detached;
    }
    
    public Task<bool> IsUserExists(long chatId)
    {
        return Context.Users.AnyAsync(x => x.UserChatId == chatId);
    }
}