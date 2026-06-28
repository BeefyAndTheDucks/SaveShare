namespace Common.Protocol.V1;

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
