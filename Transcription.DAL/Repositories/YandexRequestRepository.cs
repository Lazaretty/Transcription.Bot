using Microsoft.EntityFrameworkCore;
using Transcription.DAL.Models;

namespace Transcription.DAL.Repositories;

public class YandexRequestRepository
{
    public YandexRequestRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public WbContext Context { get; }
    
    public async Task<List<YandexRequest>> GetAllActiveAsync()
    {
        var users = await Context.YandexRequests.Where(x => ! x.IsDone).ToListAsync();
        return users;
    }

    public async Task Update(YandexRequest yandexRequest)
    {
        if (yandexRequest == null)
            throw new ArgumentNullException(nameof(yandexRequest));

        yandexRequest.UpdateTime = DateTimeOffset.Now;

        Context.Entry(yandexRequest).State = EntityState.Modified;
        await Context.SaveChangesAsync();
        Context.Entry(yandexRequest).State = EntityState.Detached;
    }
    
    public async Task Insert(YandexRequest yandexRequest)
    {
        yandexRequest.UpdateTime = DateTimeOffset.UtcNow;
        yandexRequest.CreateTime = DateTimeOffset.UtcNow;
        
        Context.YandexRequests.Add(yandexRequest);

        await Context.SaveChangesAsync();
    }
}