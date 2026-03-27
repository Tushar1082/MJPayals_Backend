using DotnetBoilerplate.Common;

namespace DotnetBoilerplate.Common;

public static class EndpointExtensions
{
    public static void MapEndpointDefinitions(this IEndpointRouteBuilder app)
    {
        var endpointDefinitions = typeof(IEndpointDefinition).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpointDefinition).IsAssignableFrom(t))
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            endpointDefinition.MapEndpoints(app);
        }
    }
}
