using Common;

namespace Client.Interfaces;

public interface IMessageCodec
{
    string Serialize(C2SMessage message);
    
    S2CMessage Deserialize(string json);
}
