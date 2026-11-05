namespace MirrorControl;

/// <summary>
/// Stable/default builds are blocked. Only the separately packaged experimental
/// test variant defines EXPERIMENTAL_RECEIVER, includes a pinned hash, and asks
/// for explicit per-launch user consent. This is not a security certification.
/// </summary>
internal static class BackendSecurityGate
{
#if EXPERIMENTAL_RECEIVER
    internal static bool IsBlocked => false;
    internal static bool IsExperimental => true;
    internal const string Reason = "EXPERIMENTAL MIRRORING BUILD: unverified AirPlay network parser. Test on an isolated trusted network with non-sensitive content only.";
#else
    internal static bool IsBlocked => true;
    internal static bool IsExperimental => false;
    internal const string Reason = "SECURITY HOLD: normal builds do not launch the experimental AirPlay receiver. Only the separately identified opt-in test package can enable mirroring.";
#endif
}
