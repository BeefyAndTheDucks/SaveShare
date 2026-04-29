using System.Collections.Concurrent;
using System.Net.WebSockets;
using Common;
using Newtonsoft.Json.Linq;
using Server.MessageHandlers.V1;

namespace Server;

public class ConnectionManagerV1(CancellationToken exitToken)
{
    private readonly List<WebSocket> _activeConnections = [];
    
    private readonly ConcurrentDictionary<WebSocket, User> _users = new();
    private readonly ConcurrentDictionary<WebSocket, bool> _shouldUpdateSaveList = new();
    
    public User GetUser(WebSocket ws) => _users[ws];

    private async Task<Result<User>> TryAuthenticate(JObject receivedMessage, WebSocket ws, CancellationToken ct = default)
    {
        User user;
        if (receivedMessage.TryParseAsMessage(out C2SSignInAsNewUserMessage? newUserMessage))
        {
            Result<User> createResult = await UserRegistry.CreateUser(newUserMessage!.UserName, ct);
            if (!createResult.Succeeded)
                return Result<User>.Failure(createResult.Error);
            user = createResult.Value;
            await MessageHelpers.SendMessage(new S2CNewUserCreatedMessage(createResult.Value.Id), ws, ct);
        } else if (receivedMessage.TryParseAsMessage(out C2SSignInAsExistingUserMessage? existingUserMessage))
        {
            Result<User> getResult = await UserRegistry.GetUser(existingUserMessage!.UserId, ct);
            if (!getResult.Succeeded)
                return Result<User>.Failure(getResult.Error);
            user = getResult.Value;
            await MessageHelpers.SendMessage(new S2CSuccessfullySignedInMessage(getResult.Value.Username), ws, ct);
        }
        else
            return Result<User>.Failure("You are not signed in. Please sign in or create a new user first.");
        
        _users[ws] = user;
        return user;
    }
    
    public async Task OnRequest(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
    
        using WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
        _activeConnections.Add(ws);
        _shouldUpdateSaveList.TryAdd(ws, false);
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, exitToken);

        bool signedIn = false;
        
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                string received = await WebSocketUtils.ReceiveString(ws, cts.Token);
                if (string.IsNullOrWhiteSpace(received)) continue;
                
                JObject receivedJObject = JObject.Parse(received);
                if (!signedIn)
                {
                    Result<User> authResult  = await TryAuthenticate(receivedJObject, ws, cts.Token);
                    if (!authResult.Succeeded)
                        await MessageHelpers.SendMessage(new S2CErrorMessage(ErrorCode.FailedToAuthenticate, authResult.Error), ws, cts.Token);
                    else
                        signedIn = true;
                    continue;
                }

                if (_shouldUpdateSaveList[ws])
                {
                    await MessageHelpers.SendMessage(new S2CSavesChangedMessage(await SaveRegistry.GetSaves(cts.Token)), ws, cts.Token);
                    continue;
                }
                
                bool propagate = await MessageHandlerFactory.Handle(receivedJObject, ws, cts.Token);

                if (!propagate)
                    continue;

                foreach (WebSocket connection in _activeConnections.Where(c => c != ws))
                    _shouldUpdateSaveList[connection] = true;
            }
        }
        finally
        {
            _activeConnections.Remove(ws);
            _shouldUpdateSaveList.Remove(ws, out _);
            _users.TryRemove(ws, out _);
        }
    }
}