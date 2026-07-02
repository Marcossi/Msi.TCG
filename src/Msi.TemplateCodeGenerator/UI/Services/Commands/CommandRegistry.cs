using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.UI.Services.Commands;

internal sealed class CommandRegistry(
    ICommandContext commandContext,
    ILogger<CommandRegistry> logger) : ICommandRegistry
{
    private readonly ICommandContext _commandContext = commandContext;
    private readonly ILogger<CommandRegistry> _logger = logger;

    public bool CanExecute(string commandName)
    {
        ICommandRoute? route = _commandContext.ActiveRoute;
        if (route is null)
        {
            _logger.LogDebug("No hay contexto activo para ejecutar '{CommandName}'", commandName);
            return false;
        }

        bool canExecute = route.CanExecute(commandName);
        _logger.LogDebug("CanExecute('{CommandName}') = {CanExecute} en {ActiveRoute}",
            commandName, canExecute, route.GetType().Name);
        return canExecute;
    }

    public async Task<bool> ExecuteAsync(string commandName)
    {
        ICommandRoute? route = _commandContext.ActiveRoute;
        if (route is null)
        {
            _logger.LogWarning("No hay contexto activo para ejecutar '{CommandName}'", commandName);
            return false;
        }

        if (!route.CanExecute(commandName))
        {
            _logger.LogWarning("El comando '{CommandName}' no puede ejecutarse en el contexto actual", commandName);
            return false;
        }

        _logger.LogInformation("[UI] Command: {CommandName} (contextual)", commandName);
        await route.ExecuteAsync(commandName);
        return true;
    }
}
