namespace Common.Protocol.V1;

public static class ProtocolV1
{
    public const int SUBVERSION = 1;
}

public enum S2CMessageType
{
    Error = -1,
    Success = 0,
    
    ServerVersion = 1,
    
    NewUserCreated,
    SuccessfullySignedIn,
    
    SaveList,
    GotSaveInfo,
    SavesChanged,
    
    RegisteredNewSave,
    
    ReadyForBinaryData,
    ReadyToSendBinaryData,
    
    Progress,
    
    SaveManifest,
}
public abstract record S2CMessage (S2CMessageType Type);

public enum ErrorCode
{
    UnknownMessage,
    UnexpectedResponse,
    
    FailedToAuthenticate,
    AlreadySignedIn,
    
    FailedToCreateNewSave,
    OverwriteSaveDataFailed,
    
    SaveDoesNotExist,
    SaveFilesMissing,
    
    FailedToCheckOut,
    
    FailedToDownload,
    
    NotCheckedOut,
    
    ForceReleaseFailed,
    ReleaseFailed
}



// Direct operation result messages
[S2CMessageType(S2CMessageType.ServerVersion)]
public record S2CServerVersionMessage(int ProtocolVersion = ProtocolV1.SUBVERSION, string ApplicationVersion = Constants.APPLICATION_VERSION) : S2CMessage(S2CMessageType.ServerVersion);

[S2CMessageType(S2CMessageType.SuccessfullySignedIn)]
public record S2CSuccessfullySignedInMessage (string UserName) : S2CMessage(S2CMessageType.SuccessfullySignedIn);

[S2CMessageType(S2CMessageType.NewUserCreated)]
public record S2CNewUserCreatedMessage(Guid Id) : S2CMessage(S2CMessageType.NewUserCreated);

[S2CMessageType(S2CMessageType.SaveList)]
public record S2CSaveListMessage(SaveInfo[] Saves) : S2CMessage(S2CMessageType.SaveList);

[S2CMessageType(S2CMessageType.GotSaveInfo)]
public record S2CGotSaveInfoMessage(SaveInfo Save) : S2CMessage(S2CMessageType.GotSaveInfo);

[S2CMessageType(S2CMessageType.SaveManifest)]
public record S2CSaveManifestMessage(DirectoryManifest Manifest) : S2CMessage(S2CMessageType.SaveManifest);

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
[S2CMessageType(S2CMessageType.Progress)]
public record S2CProgressMessage(double Progress) : S2CMessage(S2CMessageType.Progress);

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
    Release,
    
    RegisterNewSave,
    
    OverwriteSaveData,
    
    CheckoutSave,
    DownloadSave,
    DownloadSaveChanges,
    UploadSaveChanges,
    
    ReadyForBinaryData,
    ReadyToSendBinaryData,
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

[C2SMessageType(C2SMessageType.Release)]
public record C2SReleaseMessage(SaveId SaveId) : C2SMessage(C2SMessageType.Release);

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

[C2SMessageType(C2SMessageType.ReadyToSendBinaryData)]
public record C2SReadyToSendBinaryDataMessage() : C2SMessage(C2SMessageType.ReadyToSendBinaryData);

[C2SMessageType(C2SMessageType.DownloadSaveChanges)]
public record C2SDownloadSaveChangesMessage(SaveId SaveId, DirectoryManifest ClientSideManifest) : C2SMessage(C2SMessageType.DownloadSaveChanges);

[C2SMessageType(C2SMessageType.UploadSaveChanges)]
public record C2SUploadSaveChangesMessage(SaveId SaveId, DirectoryManifest ClientSideManifest) : C2SMessage(C2SMessageType.UploadSaveChanges);