using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FixIt.Messages;

/// <summary>Sent when reports were changed in the background (e.g. addresses synced after reconnecting).</summary>
public sealed class ReportsChangedMessage(int changedCount) : ValueChangedMessage<int>(changedCount);
