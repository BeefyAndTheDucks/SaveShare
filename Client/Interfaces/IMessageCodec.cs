using Common.Protocol.V1;

namespace Client.Interfaces;

public interface IMessageCodec
{
    string Serialize(C2SMessage message);
    
    S2CMessage Deserialize(string json);
}
