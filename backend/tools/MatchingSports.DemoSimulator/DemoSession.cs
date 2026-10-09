using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.DemoSimulator;

internal sealed class SimulationCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
}

internal sealed class DemoSession(ServiceProvider provider, string password, CancellationToken token)
{
    public HashSet<Guid> AuthenticatedUsers { get; } = [];
    public string Password { get; } = password;
    public CancellationToken Token { get; } = token;
    public ServiceProvider Provider { get; } = provider;

    public async Task<T> SendAsync<T>(Guid actorId, IRequest<T> request)
    {
        if (!AuthenticatedUsers.Contains(actorId)) throw new DemoSimulationException("Actor demo chưa được xác minh mật khẩu.");
        if (request is IPermissionAuthorizedRequest authorized && authorized.ActorUserId != actorId)
            throw new DemoSimulationException("Actor của command/query không khớp phiên mô phỏng.");
        await using var scope = Provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<SimulationCurrentUser>().UserId = actorId;
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, Token);
    }

    public async Task<T> ReadAsync<T>(Func<MatchingSportsDbContext, CancellationToken, Task<T>> read)
    {
        await using var scope = Provider.CreateAsyncScope();
        return await read(scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>(), Token);
    }

    public async Task PumpMatchingAsync()
    {
        for (var index = 0; index < 2000; index++)
        {
            Token.ThrowIfCancellationRequested();
            await using var scope = Provider.CreateAsyncScope();
            if (!await scope.ServiceProvider.GetRequiredService<MatchingJobs>().ProcessNextAsync(Token)) return;
        }
        throw new DemoSimulationException("Worker chưa hết batch sau 2000 bước; checkpoint được giữ để tiếp tục.");
    }

    public async Task PublishRemindersAsync()
    {
        for (var index = 0; index < 20; index++)
        {
            await using var scope = Provider.CreateAsyncScope();
            if (await scope.ServiceProvider.GetRequiredService<MatchingJobs>().PublishRemindersBatchAsync(Token) == 0) return;
        }
        throw new DemoSimulationException("Worker nhắc lịch chưa hết batch; checkpoint được giữ.");
    }
}
