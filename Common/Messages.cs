using System.Reflection;

namespace Common;

public enum S2CMessageType
{
    Error = -1,
    Success = 0,
    
    NewUserCreated,
    SuccessfullySignedIn,
    
    SaveList,
    GotSaveInfo,
    SavesChanged,
    
    RegisteredNewSave,
    
    UnpackProgress,
    
    ReadyForBinaryData,
    ReadyToSendBinaryData
}
public abstract record S2CMessage(S2CMessageType Type);

public enum ErrorCode
{
    UnknownMessage,
    
    FailedToAuthenticate,
    AlreadySignedIn,
    
    FailedToCreateNewSave,
    OverwriteSaveDataFailed,
    
    SaveDoesNotExist,
    
    FailedToCheckOut,
    
    FailedToDownload,
    
    ForceReleaseFailed
}



// Direct operation result messages
[S2CMessageType(S2CMessageType.SuccessfullySignedIn)]
public record S2CSuccessfullySignedInMessage(string UserName) : S2CMessage(S2CMessageType.SuccessfullySignedIn);

[S2CMessageType(S2CMessageType.NewUserCreated)]
public record S2CNewUserCreatedMessage(Guid Id) : S2CMessage(S2CMessageType.NewUserCreated);

[S2CMessageType(S2CMessageType.SaveList)]
public record S2CSaveListMessage(SaveInfo[] Saves) : S2CMessage(S2CMessageType.SaveList);

[S2CMessageType(S2CMessageType.GotSaveInfo)]
public record S2CGotSaveInfoMessage(SaveInfo Save) : S2CMessage(S2CMessageType.GotSaveInfo);

[S2CMessageType(S2CMessageType.RegisteredNewSave)]
public record S2CRegisteredNewSaveMessage(SaveInfo CreatedSaveInfo) : S2CMessage(S2CMessageType.RegisteredNewSave);

[S2CMessageType(S2CMessageType.ReadyForBinaryData)]
public record S2CReadyForBinaryDataMessage() : S2CMessage(S2CMessageType.ReadyForBinaryData);

[S2CMessageType(S2CMessageType.ReadyToSendBinaryData)]
public record S2CReadyToSendBinaryDataMessage(long ByteCount) : S2CMessage(S2CMessageType.ReadyToSendBinaryData);

[S2CMessageType(S2CMessageType.Success)]
public record S2CSuccessMessage(string Message) : S2CMessage(S2CMessageType.Success);

[S2CMessageType(S2CMessageType.Error)]
public record S2CErrorMessage(ErrorCode Code, string Message) : S2CMessage(S2CMessageType.Error);


// Progress messages
[S2CMessageType(S2CMessageType.UnpackProgress)]
public record S2CUnpackProgressMessage(double ProgressNormalized) : S2CMessage(S2CMessageType.UnpackProgress);

// State messages
[S2CMessageType(S2CMessageType.SavesChanged)]
public record S2CSavesChangedMessage(SaveInfo[] Saves) : S2CMessage(S2CMessageType.SavesChanged);

public enum C2SMessageType
{
    Unknown = -1,
    
    SignInAsNewUser,
    SignInAsExistingUser,
    
    ListSaves,
    GetSaveInfo,
    ForceRelease,
    
    RegisterNewSave,
    
    OverwriteSaveData,
    
    CheckoutSave,
    DownloadSave,
    
    ReadyForBinaryData,
}
public abstract record C2SMessage(C2SMessageType Type);

[C2SMessageType(C2SMessageType.SignInAsNewUser)]
public record C2SSignInAsNewUserMessage(string UserName) : C2SMessage(C2SMessageType.SignInAsNewUser);

[C2SMessageType(C2SMessageType.SignInAsExistingUser)]
public record C2SSignInAsExistingUserMessage(Guid UserId) : C2SMessage(C2SMessageType.SignInAsExistingUser);

[C2SMessageType(C2SMessageType.ListSaves)]
public record C2SListSavesMessage() : C2SMessage(C2SMessageType.ListSaves);

[C2SMessageType(C2SMessageType.GetSaveInfo)]
public record C2SGetSaveInfoMessage(SaveId SaveId) : C2SMessage(C2SMessageType.GetSaveInfo);

[C2SMessageType(C2SMessageType.ForceRelease)]
public record C2SForceReleaseMessage(SaveId SaveId) : C2SMessage(C2SMessageType.ForceRelease);

[C2SMessageType(C2SMessageType.RegisterNewSave)]
public record C2SRegisterNewSaveMessage(string Name) : C2SMessage(C2SMessageType.RegisterNewSave);

[C2SMessageType(C2SMessageType.OverwriteSaveData)]
public record C2SOverwriteSaveDataMessage(SaveId SaveId) : C2SMessage(C2SMessageType.OverwriteSaveData);

[C2SMessageType(C2SMessageType.CheckoutSave)]
public record C2SCheckoutSaveMessage(SaveId SaveId) : C2SMessage(C2SMessageType.CheckoutSave);

[C2SMessageType(C2SMessageType.DownloadSave)]
public record C2SDownloadSaveMessage(SaveId SaveId) : C2SMessage(C2SMessageType.DownloadSave);

[C2SMessageType(C2SMessageType.ReadyForBinaryData)]
public record C2SReadyForBinaryDataMessage() : C2SMessage(C2SMessageType.ReadyForBinaryData);




[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class C2SMessageTypeAttribute(C2SMessageType type) : Attribute
{
    public C2SMessageType Type { get; } = type;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class S2CMessageTypeAttribute(S2CMessageType type) : Attribute
{
    public S2CMessageType Type { get; } = type;
}

public static class MessageTypeHelpers
{
    public static IReadOnlyDictionary<TEnum, Type> BuildMessageTypeMap<TBase, TAttribute, TEnum>(
        Func<TAttribute, TEnum> getMessageType)
        where TAttribute : Attribute
        where TBase : class
        where TEnum : struct, Enum
    {
        Type[] messageTypes = typeof(TBase)
            .Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(TBase).IsAssignableFrom(type))
            .ToArray();

        var entries = messageTypes
            .Select(type => new
            {
                MessageClass = type,
                Attribute = type.GetCustomAttribute<TAttribute>()
            })
            .Where(x => x.Attribute is not null)
            .Select(x => new
            {
                x.MessageClass,
                MessageType = getMessageType(x.Attribute!)
            })
            .ToArray();

        return entries.ToDictionary(
            x => x.MessageType,
            x => x.MessageClass);
    }
}
