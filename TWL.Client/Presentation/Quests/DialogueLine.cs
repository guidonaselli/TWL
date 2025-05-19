using System.Collections.Generic;

namespace TWL.Client.Presentation.Quests;

public sealed record DialogueLine(
    string           SpeakerName,
    string           Text,
    IReadOnlyList<string> Options,
    int              NextLineId = -1);