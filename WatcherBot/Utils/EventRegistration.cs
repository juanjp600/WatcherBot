using DisCatSharp;
using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace WatcherBot.Utils;

[AttributeUsage(AttributeTargets.Method)]
public class DiscordEvent : Attribute
{
    public readonly string? EventName;

    public DiscordEvent(string? name = null)
        => EventName = name;
}

[AttributeUsage(AttributeTargets.Class)]
public class DiscordEventHandler : Attribute { }

public class EventRegistrar
{
    private readonly IServiceProvider serviceProvider;
    private readonly DiscordClient client;

    public EventRegistrar(DiscordClient client, IServiceProvider serviceProvider)
    {
        this.client = client;
        this.serviceProvider = serviceProvider;
    }

    private void RegisterEventHandlerImpl(object? handler, Type type)
    {
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                               .Select(method => (method, attribute: method.GetCustomAttribute<DiscordEvent>()))
                               .Where(m => m.attribute is not null && (m.method.IsStatic || handler is not null))
                               .Select(m => (m.method, typeof(DiscordClient).GetEvent(m.attribute!.EventName ?? m.method.Name)
                                   ?? throw new Exception("Event does not exist")));

        foreach (var (method, evtn) in methods)
        {
            Type handlerType = evtn.EventHandlerType ?? throw new Exception("Failed to get Type");
            evtn.AddEventHandler(client, method.IsStatic
                ? Delegate.CreateDelegate(handlerType, method)
                : Delegate.CreateDelegate(handlerType, handler, method));
        }
    }

    public void RegisterEventHandler(object handler) => RegisterEventHandlerImpl(handler, handler.GetType());

    public void RegisterEventHandler(Type type)
    {
        if (type.IsAbstract)
        {
            RegisterEventHandlerImpl(null, type);
        }
        else
        {
            RegisterEventHandler(ActivatorUtilities.CreateInstance(serviceProvider, type));
        }
    }

    public void RegisterEventHandler<T>() => RegisterEventHandler(typeof(T));

    public void RegisterEventHandlers(Assembly assembly)
    {
        foreach (Type t in assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<DiscordEventHandler>() is not null))
        {
            RegisterEventHandler(t);
        }
    }
}
