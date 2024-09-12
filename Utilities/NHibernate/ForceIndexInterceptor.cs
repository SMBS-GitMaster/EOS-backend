using NHibernate;
using NHibernate.SqlCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities {
  public class ForceIndexInterceptor {

    private static string PREFIX = "select 'force-index:";
    private static string SUFFIX = "'";

    private static Regex ACCEPTABLE_NAME = new Regex("^[a-zA-Z0-9_]+$");
    private static int MAX = 16;

    public static SqlString Process(SqlString hql) {

      try {
        var safetyCounter = 0;
        var start = hql.IndexOf(PREFIX, 0, hql.Length, StringComparison.InvariantCultureIgnoreCase);
        //Replace all 
        while (start != -1 && safetyCounter < MAX) {
          //Find classname
          var end = hql.IndexOf(SUFFIX, start + PREFIX.Length, hql.Length - (start + PREFIX.Length), StringComparison.InvariantCultureIgnoreCase);
          if (end != -1) {
            var className = hql.Substring(start + PREFIX.Length, end - (start + PREFIX.Length));
            if (className.Length != 0) {
              if (ACCEPTABLE_NAME.IsMatch(className.ToString())) {
                //Find next instance of class and insert after alias
                //                   |
                //                   v
                //"FROM `class` this_ " 
                var classSearch = "FROM `" + className + "` ";
                var nextClassLoc = hql.IndexOf(classSearch, end + SUFFIX.Length, hql.Length - (end + SUFFIX.Length), StringComparison.InvariantCultureIgnoreCase);
                if (nextClassLoc == -1) {
                  //try without the back-quote
                  classSearch = "FROM " + className + " ";
                  nextClassLoc = hql.IndexOf(classSearch, end + SUFFIX.Length, hql.Length - (end + SUFFIX.Length), StringComparison.InvariantCultureIgnoreCase);
                }
                if (nextClassLoc != -1) {
                  //only insert if we found the spot for it.
                  var afterClassSpace = hql.IndexOf(" ", nextClassLoc + classSearch.Length, hql.Length - (nextClassLoc + classSearch.Length), StringComparison.InvariantCultureIgnoreCase);
                  hql = hql.Insert(afterClassSpace, " FORCE INDEX (PRIMARY) ");
                }
              }
            }
            start = hql.IndexOf(PREFIX, end, hql.Length - end, StringComparison.InvariantCultureIgnoreCase);
          } else {
            start += 1;
          }
          safetyCounter += 1;
        }
        return hql;
      } catch (Exception e) {
        return hql;
      }
    }

    /// <summary>
    /// Needs to be called immediately before it is used.
    /// may cause issues if there are multiple instances of the class name in the query
    /// The query you are updating ought to be marked "Future" even if you plan to execute it imediately afterwards
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="className"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SetForceIndexHint(ISession s, string className) {

      if (!ACCEPTABLE_NAME.IsMatch(className.ToString())) {
        throw new ArgumentOutOfRangeException("Class name must contain letters, number and underscore only");
      }

      s.CreateQuery(PREFIX + className + SUFFIX + " from ApplicationWideModel")
        .SetMaxResults(1)
        .FutureValue<int>();

    }

  }
}
