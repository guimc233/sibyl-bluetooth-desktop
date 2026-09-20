using SibylEarbuds.Core.Models;

namespace SibylEarbuds.App.ViewModels;

public record ShutdownOption(string Display, int Minutes);

public record KeyFunctionOption(string Display, KeyFunctionType Value);
