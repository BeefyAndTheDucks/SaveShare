using System.Reflection;

namespace Common.Protocol;

public static class MessageTypeHelpers
{
    public static IReadOnlyDictionary<TEnum, Type> BuildMessageTypeMap<TBase, TAttribute, TEnum>(
        Func<TAttribute, TEnum> getMessageType)
        where TAttribute : Attribute
        where TBase : class
        where TEnum : struct, Enum
    {
        var messageTypes = typeof(TBase)
            .Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(TBase).IsAssignableFrom(type));

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
            });

        return entries.ToDictionary(
            x => x.MessageType,
            x => x.MessageClass);
    }
}