namespace GitClone.Application.Shared;

/// <summary>MarkersWritten is false for binary files: the conflict is reported but the working
/// tree copy is left untouched rather than risking corrupting it with text conflict markers.</summary>
public sealed record MergeConflict(string Path, bool MarkersWritten);
