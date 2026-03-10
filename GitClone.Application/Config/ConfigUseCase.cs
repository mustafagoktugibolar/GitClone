using GitClone.Core.Helpers;
using GitClone.Core.Interfaces;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Config;

public sealed class ConfigUseCase(IRepositorySessionFactory repositorySessionFactory, IPasswordPrompter passwordPrompter) : IUseCase<ConfigRequest, ConfigResult>
{
    public async Task<ConfigResult> ExecuteAsync(ConfigRequest request)
    {
        var args = request.Args;
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var configService = session.ConfigService;
        var isGlobal = IsGlobal(args);
        var actionIndex = isGlobal ? 2 : 1;

        if (args.Length <= actionIndex)
        {
            return Usage("Missing config action.");
        }

        var action = args[actionIndex].ToLowerInvariant();
        switch (action)
        {
            case "list":
            case "-l":
                if (isGlobal)
                {
                    await configService.ShowGlobalConfigs();
                }
                else
                {
                    await configService.ShowLocalConfigs();
                }

                return new ConfigResult(true, false);

            case "add":
                return await AddConfig(configService, args, isGlobal, actionIndex);

            case "remove":
            case "-rm":
                return await RemoveConfig(configService, args, isGlobal, actionIndex);

            case "edit":
                return await EditConfig(configService, args, isGlobal, actionIndex);

            default:
                return Usage($"Unknown config action: {action}");
        }
    }

    private async Task<ConfigResult> AddConfig(IConfigService configService, string[] args, bool isGlobal, int actionIndex)
    {
        if (args.Length <= actionIndex + 2)
        {
            return Usage("Missing add arguments.");
        }

        var username = args[actionIndex + 1].ToLowerInvariant();
        var email = args[actionIndex + 2].ToLowerInvariant();
        var password = passwordPrompter.ReadConfirmedPassword(PasswordValidator.Validate);

        if (isGlobal)
        {
            await configService.AddGlobalConfig(username, email, password);
        }
        else
        {
            await configService.AddLocalConfig(username, email, password);
        }

        return new ConfigResult(true, false);
    }

    private async Task<ConfigResult> RemoveConfig(IConfigService configService, string[] args, bool isGlobal, int actionIndex)
    {
        if (args.Length <= actionIndex + 1)
        {
            return Usage("Missing remove email.");
        }

        var email = args[actionIndex + 1].ToLowerInvariant();
        if (isGlobal)
        {
            await configService.RemoveGlobalConfig(email);
        }
        else
        {
            await configService.RemoveLocalConfig(email);
        }

        return new ConfigResult(true, false);
    }

    private async Task<ConfigResult> EditConfig(IConfigService configService, string[] args, bool isGlobal, int actionIndex)
    {
        if (args.Length <= actionIndex + 1)
        {
            return Usage("Missing edit target email.");
        }

        var editedEmail = args[actionIndex + 1];
        var parsed = ParseNamedOptions(args[(actionIndex + 2)..]);

        var username = parsed.GetValueOrDefault("username") ?? "";
        var email = parsed.GetValueOrDefault("email") ?? "";
        var password = parsed.ContainsKey("password")
            ? passwordPrompter.ReadConfirmedPassword(PasswordValidator.Validate)
            : "";

        if (isGlobal)
        {
            await configService.EditGlobalConfig(editedEmail, username, email, password);
        }
        else
        {
            await configService.EditLocalConfig(editedEmail, username, email, password);
        }

        return new ConfigResult(true, false);
    }

    private static Dictionary<string, string> ParseNamedOptions(IEnumerable<string> args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var array = args.ToArray();

        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = array[i].TrimStart('-');
            if (i + 1 < array.Length && !array[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = array[i + 1];
                i++;
            }
            else
            {
                options[key] = "true";
            }
        }

        return options;
    }

    private static bool IsGlobal(IReadOnlyList<string> args)
    {
        return args.Count > 1 && (args[1].Equals("--global", StringComparison.OrdinalIgnoreCase) ||
                                  args[1].Equals("-g", StringComparison.OrdinalIgnoreCase));
    }

    private static ConfigResult Usage(string? message = null)
    {
        return new ConfigResult(false, true, message);
    }
}
