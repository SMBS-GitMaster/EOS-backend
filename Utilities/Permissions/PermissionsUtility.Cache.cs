using RadialReview.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {
    public class CacheChecker {
      private PermissionsUtility p;
      private String key;

      public CacheChecker(String key, PermissionsUtility p) {
        this.key = p.caller.Id + "~" + key;
        this.p = p;
      }

      public PermissionsUtility Execute(Func<PermissionsUtility> action) {
        if (p.cache.ContainsKey(key)) {
          if (p.cache[key].Exception != null) {
            throw p.cache[key].Exception;
          }

          return p;
        } else {
          try {
            var result = action();
            p.cache[key] = new CacheResult();
            return result;
          } catch (Exception e) {
            p.cache[key] = new CacheResult() { Exception = e };
            throw;
          }
        }
      }
    }



    public class CacheResult {
      public Exception Exception { get; set; }
    }

    public void SetCache(bool allowed, string key, params long[] arguments) {
      key = caller.Id + "~" + key + "~" + String.Join("_", arguments);
      if (allowed) {
        this.cache[key] = new CacheResult();
      } else {
        this.cache[key] = new CacheResult() { Exception = new PermissionsException() };
      }
    }

    protected Dictionary<string, CacheResult> cache = new Dictionary<string, CacheResult>();

    public CacheChecker CheckCacheFirst(string key, params long[] arguments) {
      key = key + "~" + String.Join("_", arguments);
      return new CacheChecker(key, this);
    }

  }
}