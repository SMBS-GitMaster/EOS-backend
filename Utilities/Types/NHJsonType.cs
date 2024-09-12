using Newtonsoft.Json;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Types
{

  public class NHJsonType<T> : IUserType where T : class
  {
    public SqlType[] SqlTypes => new SqlType[] { new SqlType(System.Data.DbType.String) };

    public Type ReturnedType => typeof(T);

    public bool IsMutable => true;

    public object DeepCopy(object value) => value == null ? null : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

    public int GetHashCode(object x)
    {
      if (x == null) return 0;
      var json = JsonConvert.SerializeObject(x);
      return json.GetHashCode();
    }

    public new bool Equals(object x, object y)
    {
      if (ReferenceEquals(x, y))
        return true;
      if (x == null || y == null)
        return false;
      var jsonX = JsonConvert.SerializeObject(x);
      var jsonY = JsonConvert.SerializeObject(y);
      return jsonX == jsonY;
    }

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
      var json = NHibernateUtil.String.NullSafeGet(rs, names[0], session, owner) as string;
      if (string.IsNullOrEmpty(json))
      {
        return null;
      }

      return JsonConvert.DeserializeObject<T>(json) ?? null;
    }

    public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
    {
      if (value == null)
      {
        NHibernateUtil.String.NullSafeSet(cmd, null, index, session);
        return;
      }

      var json = JsonConvert.SerializeObject(value);
      NHibernateUtil.String.NullSafeSet(cmd, json, index, session);
      
    }

    public object Replace(object original, object target, object owner) => original;

    public object Assemble(object cached, object owner) => DeepCopy(cached);

    public object Disassemble(object value) => DeepCopy(value);
  }
}
