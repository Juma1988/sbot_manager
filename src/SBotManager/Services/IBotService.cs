using SBotManager.Models;

namespace SBotManager.Services;

public sealed record BotResult(bool Success, string Message, bool NeedsForce = false);
public interface IBotService
{
    ProcessSnapshot Inspect(AccountSettings account);
    Task<BotResult> OpenAsync(AccountSettings account);
    Task<BotResult> TrainingAsync(AccountSettings account, bool start);
    Task<BotResult> StartGameAsync(AccountSettings account);
    Task<BotResult> SetClientlessAfterGameAsync(AccountSettings account, bool enabled);
    Task<BotResult> GoClientlessAsync(AccountSettings account);
    Task<BotResult> SetVisibilityAsync(AccountSettings account, bool visible);
    Task<BotResult> SetRelatedClientVisibilityAsync(AccountSettings account, bool visible);
    Task<BotResult> TerminateAsync(AccountSettings account, bool force);
}
