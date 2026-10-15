namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Opts a request out of the default authentication requirement (ADR-012).
/// Use only for genuinely public operations (login, refresh, password reset).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AllowAnonymousAttribute : Attribute
{
}
