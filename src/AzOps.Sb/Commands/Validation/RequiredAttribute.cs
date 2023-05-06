using Spectre.Console;
using Spectre.Console.Cli;

namespace AzOps.Sb.Commands.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredArgumentAttribute : ParameterValidationAttribute
{
    public RequiredArgumentAttribute() : base("null")
    {
    }

    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value == null)
        {
            return ValidationResult.Error($"{context.Parameter.Description} is required");
        }

        return ValidationResult.Success();
    }
}