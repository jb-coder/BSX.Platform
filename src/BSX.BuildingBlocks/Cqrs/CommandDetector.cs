namespace BSX.BuildingBlocks.Cqrs;

/// <summary>Determines whether a request type is a command.</summary>
internal static class CommandDetector
{
    /// <summary>Gets a value indicating whether the request type is a command.</summary>
    /// <param name="requestType">The concrete request type.</param>
    public static bool IsCommand(Type requestType)
    {
        if (typeof(ICommand).IsAssignableFrom(requestType))
        {
            return true;
        }

        return requestType
            .GetInterfaces()
            .Any(@interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}
