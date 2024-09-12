using Newtonsoft.Json;

namespace RadialReview.Core.Models.Terms {
  public class Term {
    public Term(TermKey key, string value, string deflt) {
      Key = key;
      Value = value;
      Default = deflt;
    }

    [JsonIgnore]
    public TermKey Key { get; private set; }

    [JsonProperty("value")]
    public string Value { get; private set; }
    [JsonProperty("default")]
    public string Default { get; private set; }
    [JsonProperty("key")]
    public string KeyString { get { return Key.ToString(); } }
  }
}
