using System;

namespace RadialReview.Utilities.Synchronize {
  public class SyncMetaData {
    public ulong Id { get; set; }
    public string Key { get; set; }
    public DateTime StartTime { get; set; }
    public bool Complete { get; set; }
    public long TotalEllapsedMs { get; set; }
    public long AtomicActionEllapsedMs { get; set; }
    public int CreateAttempts { get; set; }
    public int UpdateAttempts { get; set; }
    public bool AtomicActionStarted { get; set; }
    public bool AtomicActionComplete { get; set; }
    public int TotalUpdates { get; set; }
    public long? LastClientUpdateTimeMs { get; set; }
    public string CreateKey { get; set; }
  }
}
