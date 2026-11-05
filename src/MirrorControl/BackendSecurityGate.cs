namespace MirrorControl;

/// <summary>
/// Release safety gate. DO NOT switch to true before security review and a
/// tested, pinned UxPlay build. See docs/SECURITY_REVIEW.md.
/// </summary>
internal static class BackendSecurityGate
{
    internal static bool IsBlocked => true;

    internal const string Reason =
        "SECURITY HOLD: UxPlay v1.73.7 contains a reported heap buffer overflow " +
        "(FDH2/UxPlay issue #614). Receiver launch is disabled until a patched " +
        "backend has been reviewed and validated.";
}
