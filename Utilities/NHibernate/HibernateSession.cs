using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using log4net;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Configuration;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Envers.Configuration;
using NHibernate.Event;
using NHibernate.Impl;
using NHibernate.Proxy;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Persister.Entity;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Middleware.BackgroundServices;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.VideoConference;
using RadialReview.Models.VTO;
using RadialReview.NHibernate;
using RadialReview.Utilities.Constants;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Productivity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static RadialReview.Models.Issues.IssueModel;
using FluentConfiguration = NHibernate.Envers.Configuration.Fluent.FluentConfiguration;
using Mapping = NHibernate.Mapping;
using System.Reflection;
using System.Collections;
using NHibernate.Collection;
using RadialReview.Core.Utilities;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Core.Models.L10;
using RadialReview.Core.Middleware.Request.GraphQL;
using RadialReview.Utilities;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using RadialReview.Crosscutting.Hooks;
using NHibernate.Type;
using Amazon.XRay.Recorder.Core;
using RadialReview.Core.Utilities.NHibernate;
using NHibernate.Driver.MySqlConnector;

namespace RadialReview.Utilities {
  public static class NHSQL {
    public static string NHibernateSQL { get; set; }
    public static bool SaveCommands { get; set; }
  }
  public class NHSQLInterceptor : EmptyInterceptor, IInterceptor {
    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    SqlString IInterceptor.OnPrepareStatement(SqlString sql) {
      //Add a special "Force Index (Primary)" into query
      sql = ForceIndexInterceptor.Process(sql);
      NHSQL.NHibernateSQL = sql.ToString();
      if(NHSQL.SaveCommands) {
        //log.Info(NHSQL.NHibernateSQL);
      }

      return sql;
    }
  }

  public class AWSXRayInterceptor : EmptyInterceptor, IInterceptor {
    SqlString IInterceptor.OnPrepareStatement(SqlString sql) {
      AWSXRayRecorder.Instance.AddSqlInformation("SQL", sql.ToString());

      return sql;
    }
    public override void PreFlush(ICollection entities) {
      AWSXRayRecorder.Instance.BeginSubsegment("MySQL NHibernate PreFlush");
    }

    public override void PostFlush(ICollection entities) {
      AWSXRayRecorder.Instance.EndSubsegment();
    }
  }

  public class HibernateSession {

    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public class RuntimeNames {
      private Configuration cfg;

      public RuntimeNames(Configuration cfg) {
        this.cfg = cfg;
      }

      public string ColumnName<T>(Expression<Func<T, object>> property)
        where T : class, new() {
        var accessor = FluentNHibernate.Utils.Reflection
          .ReflectionHelper.GetAccessor(property);

        var names = accessor.Name.Split('.');

        var classMapping = cfg.GetClassMapping(typeof(T));

        return WalkPropertyChain(classMapping.GetProperty(names.First()), 0, names);
      }

      private string WalkPropertyChain(Mapping.Property property, int index, string[] names) {
        if(property.IsComposite) {
          return WalkPropertyChain(((Mapping.Component)property.Value).GetProperty(names[++index]), index, names);
        }

        return property.ColumnIterator.First().Text;
      }

      public string TableName<T>() where T : class, new() {
        return cfg.GetClassMapping(typeof(T)).Table.Name;
      }
    }


    private static ConcurrentDictionary<Env, ISessionFactory> factories;
    private static Env? CurrentEnv;
    private static String DbFile = null;

    public static void MockFactory(Env env, ISessionFactory factory) {
      if(factories.ContainsKey(env)) {
        throw new Exception("Already set factory for Env=" + env);
      }

      factories[env] = factory;
    }

    private static object lck = new object();
    public static ISession Session { get; set; }

    public class TestClearDispose : IDisposable {
      public Action OnDispose { get; set; }
      public Env? OldEnv { get; set; }
      public void Dispose() {
        OnDispose?.Invoke();
        GetDatabaseSessionFactory(OldEnv);
      }
    }
    static HibernateSession() {
      factories = new ConcurrentDictionary<Env, ISessionFactory>();
    }
    public static RuntimeNames Names { get; private set; }


    public class Unsafe {

      private static bool? ShouldUpdateCache = null;
      private static string UpdatedHash = null;

      public static void ResetUpdate() {
        ShouldUpdateCache = null;
      }

      private static async Task SaveDbHash(ISessionFactory sessionFactory, string hash) {
        using(var session = sessionFactory.OpenSession()) {
          using(var tx = session.BeginTransaction()) {
            session.SaveOrUpdate(new Variable() {
              K = Variable.Names.DATABASE_HASH,
              V = hash,
            });
            tx.Commit();

          }
        }
      }
      private static async Task<string> GetDbHash(ISessionFactory sessionFactory) {
        using(var session = sessionFactory.OpenSession()) {
          using(var tx = session.BeginTransaction()) {
            try {
              return session.Get<Variable>(Variable.Names.DATABASE_HASH).V;
            } catch(Exception ex) {
              return Guid.NewGuid().ToString();
            }
          }
        }
      }

      public static async Task SetDBWasUpdatedSuccessfully(Configuration config) {
        if(ShouldUpdateCache == true && UpdatedHash != null) {
          using(var sessionFactory = config.BuildSessionFactory()) {
            try {
              await SaveDbHash(sessionFactory, UpdatedHash);
            } catch(Exception e) {
              Debug.WriteLine("Failed to set DatabaseHash. Database updates will always run.");
            }
          }
        } else {
          Debug.WriteLine("SetDBWasUpdated was called unexpectely. Only call this if the database was updated");
        }
      }

      public static async Task<bool> ShouldUpdateDB(Configuration config) {
        if(ShouldUpdateCache.HasValue)
          return ShouldUpdateCache.Value;

        var updateTester = new ShouldUpdateDatabase(config);
        var newHash = await updateTester.GetDatabaseCreationHash();
        string existingHash = null;
        using(var sessionFactory = config.BuildSessionFactory()) {
          try {
            existingHash = await GetDbHash(sessionFactory);
          } catch(Exception e) {
            //opps
          }
          var shouldUpdate = (existingHash != newHash);
          ShouldUpdateCache = shouldUpdate;
          if(shouldUpdate) {
            UpdatedHash = newHash;
          }
          return shouldUpdate;
        }

      }

      public static async Task<bool> ShouldUpdateDBCache() {
        if(ShouldUpdateCache == null)
          throw new Exception("Unset. Make sure to start at least one session first.");
        return ShouldUpdateCache.Value;
      }


    }



    [Obsolete("Run in a using(). Use only in synchronous environments. Built for test purposes.")]
    public static IDisposable SetDatabaseEnv_TestOnly(Env environmentOverride, Action onDispose = null) {
      lock(lck) {
        var old = CurrentEnv;
        GetDatabaseSessionFactory(environmentOverride);

        return new TestClearDispose() {
          OldEnv = old,
          OnDispose = onDispose,
        };
      }
    }

    public static string GetConnectionString(Env? environmentOverride_testOnly = null) {
      Configuration c;
      var env = environmentOverride_testOnly ?? CurrentEnv ?? Config.GetEnv();
      CurrentEnv = env;

      switch(environmentOverride_testOnly ?? Config.GetEnv()) {
        case Env.local_sqlite: {
            var connectionString = Config.GetConnectionString("LocalSqlite");
            return connectionString;
          }
        case Env.local_mysql: {
            var connectionString = Config.GetConnectionString("LocalMysql");
            return connectionString;
          }
        case Env.production: {
            var dbCred = Config.GetProductionDatabaseCredentials();
            var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1}; AllowUserVariables=True; UseAffectedRows=False; ", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);
            return connectionString;
          }
        case Env.local_test_sqlite: {
            var connectionString = "Data Source=|DataDirectory|\\_testdb.db";
            var useSqliteInMemory = Config.GetAppSetting("local_test_sqlite_memory", "false").ToBooleanJS();
            var useMysqlTest = Config.GetAppSetting("use_local_test_mysql", "false").ToBooleanJS();

            if(useMysqlTest && useSqliteInMemory) {
              throw new Exception("Multiple database types selected. Choose either mysql test or sqliteInMemory");
            } else if(useSqliteInMemory) {
              connectionString = "FullUri=file:memorydb.db?mode=memory&cache=shared;PRAGMA journal_mode=WAL;";
              return connectionString;
            } else if(useMysqlTest) {
              connectionString = "Server=localhost; Port=3306; Database=radial-test; Uid=root; Pwd=; SslMode=none; AllowUserVariables=True; UseAffectedRows=False; ";
              return connectionString;
            } else {
              return connectionString;
            }
          }
        case Env.dev_testing: {
            var dbCred = Config.GetEnvironmentRDS();
            var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1}; AllowUserVariables=True; UseAffectedRows=False; ", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);
            return connectionString;
          }

        default:
          throw new Exception("No database type");
      }
    }

    public static ISessionFactory GetDatabaseSessionFactory(Env? environmentOverride_testOnly = null) {
      Configuration c;
      var env = environmentOverride_testOnly ?? CurrentEnv ?? Config.GetEnv();
      CurrentEnv = env;
      if(!factories.ContainsKey(env)) {
        lock(lck) {
          if(factories.ContainsKey(env)) {
            return factories[env];
          }

          ChromeExtensionComms.SendCommand("dbStart");

          switch(environmentOverride_testOnly ?? Config.GetEnv()) {
            case Env.local_sqlite: {

                var connectionString = Config.GetConnectionString("LocalSqlite");
                var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
                DbFile = file;
                try {
                  c = new Configuration();

                  c.SetInterceptor(new NHSQLInterceptor());
                  factories[env] = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                  .Mappings(m => {
                    //m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                    //   .Conventions.Add<StringColumnLengthConvention>();
                    // m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
                    //m.AutoMappings.Add(CreateAutomappings);
                    //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                  })
                   .CurrentSessionContext("web")
                   .ExposeConfiguration(SetupAudit)
                   .ExposeConfiguration(async x => await BuildSqliteSchema(x))
                   .BuildSessionFactory();
                } catch(Exception e) {
                  throw;
                }
                break;
              }
            case Env.local_mysql: {
                try {
                  c = new Configuration();

                  c.SetInterceptor(new NHSQLInterceptor());



                  var connectionString = Config.GetConnectionString("LocalMysql");
                  Debug.WriteLine(" connectionString(local)..." + connectionString.ToString());
                  factories[env] = Fluently.Configure(c).Database(MySQLConfiguration.Standard.Driver<MySqlConnectorDriver>().Dialect<CustomMySQLDialect>().ConnectionString(connectionString))
                    .Mappings(m => {
                      m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                        .Conventions.Add<StringColumnLengthConvention>();
                    })
                    .CurrentSessionContext("web")
                    .ExposeConfiguration(cfg => {
                      cfg.LinqToHqlGeneratorsRegistry<CustomLinqToHqlGeneratorsRegistry>();
                      SetupAudit(cfg);
                    })
                    .ExposeConfiguration(SetupAudit)
                    .ExposeConfiguration(async x => await BuildProductionMySqlSchema(x))
                    .BuildSessionFactory();
                } catch(Exception e) {
                  var mbox = e.Message;
                  if(e.InnerException != null && e.InnerException.Message != null) {
                    mbox = e.InnerException.Message;
                  }
                  ChromeExtensionComms.SendCommand("dbError", mbox);
                  if(e.InnerException != null && e.InnerException.Message == "Unable to connect to any of the specified MySQL hosts.") {
                    throw new Exception("Could not connect to the specified database. Is your database running?", e);
                  }

                  throw;
                }
                break;
              }
            case Env.production: {
                var dbCred = Config.GetProductionDatabaseCredentials();

                Console.WriteLine("DB UserName:" + dbCred.Username);

                var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1}; AllowUserVariables=True; UseAffectedRows=False; Pipelining=False; IgnoreCommandTransaction=True; ", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);
                c = new Configuration();
                c.SetInterceptor(new AWSXRayInterceptor());

                factories[env] = Fluently.Configure(c)
                  .Database(
                      MySQLConfiguration.Standard.Driver<MySqlConnectorDriver>().Dialect<CustomMySQLDialect>().ConnectionString(connectionString)/*.ShowSql()*/)
                   .Mappings(m => {
                     m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                       .Conventions.Add<StringColumnLengthConvention>();
                   })
                   .CurrentSessionContext("web")
                   .ExposeConfiguration(cfg => {
                     cfg.LinqToHqlGeneratorsRegistry<CustomLinqToHqlGeneratorsRegistry>();
                     SetupAudit(cfg);
                   })
                   .ExposeConfiguration(SetupAudit)
                   .ExposeConfiguration(async x => await BuildProductionMySqlSchema(x))
                   .BuildSessionFactory();
                break;
              }
            case Env.local_test_sqlite: {

                string Path = "C:\\UITests";
                if(!Directory.Exists(Path)) {
                  Directory.CreateDirectory(Path);
                }
                DbFile = Path + "\\_testdb.db";
                AppDomain.CurrentDomain.SetData("DataDirectory", Path);
                var connectionString = "Data Source=|DataDirectory|\\_testdb.db";
                var forceDbCreate = false;
                var useSqliteInMemory = Config.GetAppSetting("local_test_sqlite_memory", "false").ToBooleanJS();
                var useMysqlTest = Config.GetAppSetting("use_local_test_mysql", "false").ToBooleanJS();


                IPersistenceConfigurer dbConfig;
                if(useMysqlTest && useSqliteInMemory) {
                  throw new Exception("Multiple database types selected. Choose either mysql test or sqliteInMemory");
                } else if(useSqliteInMemory) {
                  connectionString = "FullUri=file:memorydb.db?mode=memory&cache=shared;PRAGMA journal_mode=WAL;";
                  forceDbCreate = true;
                  dbConfig = SQLiteConfiguration.Standard.ConnectionString(connectionString).IsolationLevel(System.Data.IsolationLevel.ReadCommitted);
                } else if(useMysqlTest) {
                  connectionString = "Server=localhost; Port=3306; Database=radial-test; Uid=root; Pwd=; SslMode=none; AllowUserVariables=True; UseAffectedRows=False; ";
                  forceDbCreate = false;
                  dbConfig = MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionString);
                } else {
                  dbConfig = SQLiteConfiguration.Standard.ConnectionString(connectionString).IsolationLevel(System.Data.IsolationLevel.ReadCommitted);
                }

                try {
                  c = new Configuration();

                  c.SetInterceptor(new NHSQLInterceptor());
                  factories[env] = Fluently.Configure(c).Database(dbConfig)
                  .Mappings(m => {
                    m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                       .Conventions.Add<StringColumnLengthConvention>();
                  })
                  .CurrentSessionContext("web")
                   .ExposeConfiguration(SetupAudit)
                   .ExposeConfiguration(async x => await BuildSqliteSchema(x, forceDbCreate))
                   .BuildSessionFactory();
                } catch(Exception e) {
                  throw;
                }
                break;
              }
            case Env.dev_testing: {
                var dbCred = Config.GetEnvironmentRDS();
                var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1}; AllowUserVariables=True; UseAffectedRows=False; Pipelining=False; IgnoreCommandTransaction=True; ", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);

                c = new Configuration();

                factories[env] = Fluently.Configure(c).Database(
                      MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionString).ShowSql())
                   .Mappings(m => {
                     m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>().Conventions.Add<StringColumnLengthConvention>();
                   })
                   .CurrentSessionContext("web")
                   .ExposeConfiguration(SetupAudit)
                   .ExposeConfiguration(async x => await BuildProductionMySqlSchema(x))
                   .BuildSessionFactory();
                break;
              }

            default:
              throw new Exception("No database type");
          }
          Names = new RuntimeNames(c);
          ChromeExtensionComms.SendCommand("dbComplete");
        }
      }
      return factories[env];

    }

    public static bool CloseCurrentSession() {
      try {
        var session = (SingleRequestSession)HttpContextHelper.Current.NotNull(x => x.Items["NHibernateSession"]);


        if(session != null) {

          session.OuterDispose();

          if(session.IsOpen) {
            session.Close();
          }

          if(session.WasDisposed) {
            session.DisposeBackingSession();
          }
          lock(HttpContextHelper.Current) {
            HttpContextHelper.Current.Items.Remove("NHibernateSession");
          }
          return true;
        }
      } catch(Exception e) {
        Debug.WriteLine("Close Current Session Failed");
      }
      return false;
    }

    private static bool IsUsingGraphQLNHibernateHookInterceptor() {
      return GetGraphQLNHibernateHookInterceptor() != null;
    }
    private static GraphQLNHibernateHookInterceptor GetGraphQLNHibernateHookInterceptor() {
      if(HttpContextHelper.Current != null && HttpContextHelper.Current.Items != null)

        if(HttpContextHelper.Current != null && HttpContextHelper.Current.Items != null)
          return HttpContextHelper.Current.Items[GraphQLNHibernateHookInterceptor.GRAPHQL_NHIBERNATE_HOOK_INTERCEPTOR] as GraphQLNHibernateHookInterceptor;

      return null;
    }


    private static SingleRequestSession GetExistingSingleRequestSession() {
      if(!(HttpContextHelper.Current == null || HttpContextHelper.Current.Items == null) && HttpContextHelper.Current.Items["IsTest"] == null) {
        try {
          var session = (SingleRequestSession)HttpContextHelper.Current.Items["NHibernateSession"];
          return session;
        } catch(Exception) {
          //Something went wrong.. revert
          //var a = "Error";
        }
      } else if(HttpContextHelper.GraphqlSession != null) {
        var session = (SingleRequestSession)HttpContextHelper.GraphqlSession.Value;
        return session;
      }
      return null;
    }

    public static async Task RunAfterSuccessfulDisposeOrNow(ISession waitUntilFinished, Func<ISession, ITransaction, Task> method) {
      var s = (waitUntilFinished as SingleRequestSession) ?? GetExistingSingleRequestSession();
      if(s is SingleRequestSession) {
        s.RunAfterDispose(new SingleRequestSession.OnDisposedModel(method, true));
      } else if(IsUsingGraphQLNHibernateHookInterceptor()) {
        RunAfterGraphQL(method);
      } else {
        Debug.WriteStackTrace("Warning: Hook was not executed after database was closed.");
        HookExecutionService.RunAsync(method);
      }
    }

    private static void RunAfterGraphQL(Func<ISession, ITransaction, Task> action) {
      var hookData = HookData.ToReadOnly();
      GetGraphQLNHibernateHookInterceptor().AddAction(() => {
        //Run asyncronously...
        HookExecutionService.RunAsync(hookData, action);
      });
    }

    private static bool HasDatabaseTable(object obj) {
      if(obj == null)
        return false;
      if(obj.GetType().IsValueType)
        return false;
      if(GetDatabaseSessionFactory().GetClassMetadata(obj.GetType()) is AbstractEntityPersister p)
        return true;
      return false;
    }

    private class ReloadDatabaseModelsForExpression : ExpressionVisitor {
      public ReloadDatabaseModelsForExpression(ISession session) {
        this.session = session;
      }

      private ISession session { get; set; }

      protected override Expression VisitConstant(ConstantExpression expr) {
        var obj = expr.Value;
        if(obj != null) {
          if(HasDatabaseTable(obj)) {
            var updated = ReloadModel(obj);
            return Expression.Constant(updated);
          } else {
            var updated = false;
            var fields = obj.GetType()
              .GetFields().ToArray();

            foreach(var f in fields) {
              var subfield = f.GetValue(obj);
              if(HasDatabaseTable(subfield)) {
                var reloaded = ReloadModel(subfield);
                f.SetValue(obj, reloaded);
                updated = true;
              }
            }
            if(updated)
              return Expression.Constant(obj);
          }
        }

        return base.VisitConstant(expr);
      }

      private object ReloadModel(object obj) {
        var p = GetDatabaseSessionFactory().GetClassMetadata(obj.GetType()) as AbstractEntityPersister;
        var id = p.GetIdentifier(obj);
        var name = p.EntityName;
        var found = session.Get(name, id);
        return found;
      }
    }



    public static Expression ReloadDatabaseModelsWithinExpression(ISession s, Expression expr) {

      var modifier = new ReloadDatabaseModelsForExpression(s);
      return modifier.Visit(expr);
    }




    public static void DisableDeproxyObject(object obj) {
      DisableDeproxyObject(obj, new HashSet<object>());
    }

    private static Type GetAnyElementType(Type type) {
      // Type is Array
      // short-circuit if you expect lots of arrays
      if(type.IsArray)
        return type.GetElementType();

      // type is IEnumerable<T>;
      if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        return type.GetGenericArguments()[0];

      // type implements/extends IEnumerable<T>;
      var enumType = type.GetInterfaces()
                  .Where(t => t.IsGenericType &&
                       t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                  .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
      return enumType ?? type;
    }

    private static void DisableDeproxyObject(object obj, HashSet<object> seen) {
      try {
        if(obj == null)
          return;
        if(obj.GetType().IsValueType || obj is string)
          return;
        if(seen.Contains(obj))
          return;
        seen.Add(obj);
        if(HasDatabaseTable(obj)) {
          PropertyInfo[] properties = obj.GetType()
            .GetProperties()
            .Where(p => p.GetMethod != null && p.SetMethod != null && p.SetMethod.IsVirtual && p.GetMethod.IsVirtual && !p.PropertyType.IsValueType)
            .ToArray();

          foreach(var prop in properties) {
            var val = prop.GetValue(obj);
            if(val == null) {
              continue;
            } else if(val is INHibernateProxy proxy) {
              if(proxy.HibernateLazyInitializer.IsUninitialized) {
                proxy.HibernateLazyInitializer.UnsetSession();
              }
              DisableDeproxyObject(val, seen);
            } else if(val is IEnumerable enumerable) {
              var childType = GetAnyElementType(val.GetType());
              if(childType.IsValueType || childType == typeof(string))
                return;
              if(enumerable is ILazyInitializedCollection proxyCollection) {
                if(!proxyCollection.WasInitialized) {
                  prop.SetValue(val, null);
                } else {
                  foreach(var o in enumerable) {
                    DisableDeproxyObject(o, seen);
                  }
                }
              }
              foreach(var o in enumerable) {
                DisableDeproxyObject(o, seen);
              }
            } else if(val is object) {
              DisableDeproxyObject(val, seen);
            }
          }
        } else {
          var properties = obj.GetType().GetProperties().Where(x => x.GetMethod != null).ToArray();
          foreach(var prop in properties) {
            var propValue = prop.GetValue(obj, null);
            var elems = propValue as IEnumerable;
            if(elems != null) {
              foreach(var item in elems) {
                DisableDeproxyObject(item, seen);
              }
            } else {
              DisableDeproxyObject(propValue, seen);
            }
          }
          var fields = obj.GetType().GetFields().ToArray();
          foreach(var field in fields) {
            var propValue = field.GetValue(obj);
            var elems = propValue as IEnumerable;
            if(elems != null) {
              foreach(var item in elems) {
                DisableDeproxyObject(item, seen);
              }
            } else {
              DisableDeproxyObject(propValue, seen);
            }
          }
        }
      } catch(Exception e) {
        throw;
      }
    }

    public static IOuterSession CreateOuterSession(Env? environmentOverride_TestOnly = null) {
      var session = new SingleRequestSession(GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession(), true);
      return session;
    }

    public static bool CanDeproxy(INHibernateProxy proxy) {
      if(!proxy.HibernateLazyInitializer.IsUninitialized)
        return true;

      if((proxy.HibernateLazyInitializer.Session != null && proxy.HibernateLazyInitializer.Session.IsOpen)) {
        return true;
      }
      return false;
    }

    public static void InitializeSessionContextForRepository() {
      lock(HttpContextHelper.Current) {
        HttpContextHelper.Current.Items["GraphQL"] = true;
      }
    }

    public static ISession InitializeSessionContextAs(SessionContext context) {
      bool singleSession = true;
      Env? environmentOverride_TestOnly = null;
      lock(HttpContextHelper.Current) {
        if(context == SessionContext.GraphQL) {
          HttpContextHelper.Current.Items["GraphQL"] = true;
          singleSession = false;
        } else if(context == SessionContext.MVC) {
          //Clear existing session.
          HttpContextHelper.Current.Items.Remove("NHibernateSession");
        } else if(context == SessionContext.Tasks) {
          singleSession = false;
        } else {
          throw new NotImplementedException("Unknown SessionContext:" + context);
        }
        return GetCurrentSession(singleSession, environmentOverride_TestOnly);
      }
    }

    public static ISession GetCurrentSession(bool singleSession = true, Env? environmentOverride_TestOnly = null) {

      //TODO remove this line.
      //Debug.WriteStackTraceLine(3);

      var hasUnitTestSession = HttpContextHelper.GraphqlSession != null;
      var hasHttpContext = (HttpContextHelper.Current != null && HttpContextHelper.Current.Items != null);
      var isGraphQL = hasHttpContext && (HttpContextHelper.Current.Items["GraphQL"] as bool?) == true;
      var isTest = hasHttpContext && HttpContextHelper.Current.Items["IsTest"] != null;


      if(hasUnitTestSession || (singleSession && hasHttpContext && !isTest && !isGraphQL)) {
        try {
          var session = GetExistingSingleRequestSession();
          if(session == null) {
            session = new SingleRequestSession(GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession()); // Create session, like SessionFactory.createSession()...
            if(HttpContextHelper.Current != null) {
              lock(HttpContextHelper.Current) {
                HttpContextHelper.Current.Items.Add("NHibernateSession", session);
              }
            }

            if(HttpContextHelper.GraphqlSession != null)
              HttpContextHelper.GraphqlSession.Value = session;
          } else {
            session.AddContext();
          }
          return session;
        } catch(Exception) {
          //Something went wrong.. revert
          //var a = "Error";
        }
      }
      if(isTest && !isGraphQL) {
        return GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession();
      }

      if(singleSession == false || isGraphQL) {
        return GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession();
      }

      return CreateOuterSession(environmentOverride_TestOnly);


    }

    private static async Task BuildSqliteSchema(Configuration config, bool forceCreate = false) {
      // delete the existing db on each run
      if(await Unsafe.ShouldUpdateDB(config) || forceCreate) {
        if(!File.Exists(DbFile) || forceCreate) {
          new SchemaExport(config).Create(false, true);
        } else {
          new SchemaUpdate(config).Execute(false, true);
        }
        await Unsafe.SetDBWasUpdatedSuccessfully(config);
        //Config.DbUpdateSuccessful();
      }

      var auditEvents = new AuditEventListener();
      config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
      config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

      // this NHibernate tool takes a configuration (with mapping info in)
      // and exports a database schema from it
    }



    public static void SetupAudit(Configuration nhConf) {

      var enversConf = new FluentConfiguration();
      nhConf.SetEnversProperty(ConfigurationKey.StoreDataAtDelete, true);
      nhConf.SetEnversProperty(ConfigurationKey.AuditStrategyValidityStoreRevendTimestamp, true);
      nhConf.SetEnversProperty(ConfigurationKey.AuditStrategy, typeof(CustomValidityAuditStrategy));


      enversConf.Audit<VtoItem>().ExcludeRelationData(x => x.Vto);
      enversConf.Audit<VtoItem_Bool>().ExcludeRelationData(x => x.Vto);
      enversConf.Audit<VtoItem_String>().ExcludeRelationData(x => x.Vto);
      enversConf.Audit<VtoItem_DateTime>().ExcludeRelationData(x => x.Vto);
      enversConf.Audit<VtoItem_Decimal>().ExcludeRelationData(x => x.Vto);

      enversConf.Audit<CoreFocusModel>();
      enversConf.Audit<MarketingStrategyModel>();
      enversConf.Audit<OneYearPlanModel>();
      enversConf.Audit<QuarterlyRocksModel>();
      enversConf.Audit<ThreeYearPictureModel>();
      enversConf.Audit<VtoModel>()
        .ExcludeRelationData(x => x.CoreFocus)
        .ExcludeRelationData(x => x.MarketingStrategy)
        .ExcludeRelationData(x => x.OneYearPlan)
        .ExcludeRelationData(x => x.QuarterlyRocks)
        .ExcludeRelationData(x => x.ThreeYearPicture);

      enversConf.Audit<TodoModel>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified).Exclude(x => x.ContextNodeTitle).Exclude(x => x.ContextNodeType);

      enversConf.Audit<IssueModel>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified).Exclude(x => x.ContextNodeTitle).Exclude(x => x.ContextNodeType);
      enversConf.Audit<ScoreModel>();
      enversConf.Audit<MeasurableModel>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified).Exclude(x => x.HasV3Config).Exclude(x => x.Frequency).Exclude(x => x.Scores);
      enversConf.Audit<L10Meeting>()
        .Exclude(x => x.IssueVotingHasEnded);

      enversConf.Audit<L10Recurrence>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified).Exclude(x => x.VideoConferenceLink)
        .Exclude(x => x.IsPaused).Exclude(x => x.IssueVotingHasEnded).Exclude(x => x.IssueVoting).Exclude(x => x.ShowNumberedIssueList).Exclude(x => x.BusinessPlanId);
      enversConf.Audit<IssueModel_Recurrence>()
        .Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified).Exclude(x => x.AddToDepartmentPlan).Exclude(_ => _.Stars)
        .ExcludeRelationData(x => x.CopiedFrom)
        .ExcludeRelationData(x => x.ParentRecurrenceIssue);

      enversConf.Audit<ClientReviewModel>();
      enversConf.Audit<LongModel>();
      enversConf.Audit<LongTuple>();
      enversConf.Audit<PaymentModel>();
      enversConf.Audit<PaymentPlanModel>();
      enversConf.Audit<InvoiceModel>();
      enversConf.Audit<InvoiceItemModel>();
      enversConf.Audit<QuestionCategoryModel>();
      enversConf.Audit<LocalizedStringModel>();
      enversConf.Audit<LocalizedStringPairModel>();
      enversConf.Audit<ImageModel>();

      enversConf.Audit<SurveyResponse>().Exclude(x => x.Item).Exclude(x => x.ItemFormat).Exclude(x => x.About_SUN).Exclude(x => x.By_SUN);

      enversConf.Audit<PeriodModel>();
      enversConf.Audit<ReviewModel>().Exclude(x => x.ReviewerUserId);
      enversConf.Audit<ReviewsModel>().Exclude(x => x.OrganizationId);
      enversConf.Audit<RockModel>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified);
      enversConf.Audit<Milestone>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified);
      enversConf.Audit<Askable>().Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified);

      enversConf.Audit<L10Recurrence.L10Recurrence_Page>()
        .ExcludeRelationData(x => x.L10Recurrence).Exclude(x => x.Version).Exclude(x => x.LastUpdatedBy).Exclude(x => x.DateLastModified)
        .Exclude(x => x.TitleNoteId).Exclude(x => x.TitleNoteText).Exclude(x => x.TimeLastPaused).Exclude(x => x.TimeLastStarted).Exclude(x => x.TimePreviouslySpentS).Exclude(x => x.TimeSpentPausedS)
        .Exclude(x => x.CheckInType).Exclude(x => x.IceBreaker).Exclude(x => x.IsAttendanceVisible);

      enversConf.Audit<RoleModel_Deprecated>();
      enversConf.Audit<UserOrganizationModel>()
        .ExcludeRelationData(x => x.Groups)
        .ExcludeRelationData(x => x.ManagingGroups)
        .Exclude(x => x.Cache);
      enversConf.Audit<PositionDurationModel>()
        .ExcludeRelationData(x => x.DepricatedPosition);


      enversConf.Audit<QuestionModel>();
      enversConf.Audit<TeamDurationModel>();
      enversConf.Audit<ManagerDuration>().Exclude(x => x.ManagerId).Exclude(x => x.SubordinateId);
      enversConf.Audit<OrganizationTeamModel>();
      enversConf.Audit<Deprecated.OrganizationPositionModel>();
      enversConf.Audit<PositionModel>();

      enversConf.Audit<OrganizationModel>();
      enversConf.Audit<ResponsibilityGroupModel>();
      enversConf.Audit<ResponsibilityModel>();
      enversConf.Audit<TempUserModel>().Exclude(x => x.IsUsingV3);
      enversConf.Audit<UserModel>().Exclude(x => x.IsUsingV3);
      enversConf.Audit<UserRoleModel>();
      enversConf.Audit<UserLogin>();

      enversConf.Audit<PaymentSpringsToken>();
      enversConf.Audit<ScheduledTask>();


      enversConf.Audit<Dashboard>();
      enversConf.Audit<TileModel>();

      enversConf.Audit<AbstractVCProvider>();
      enversConf.Audit<ZoomUserLink>();
      enversConf.Audit<Variable>();
      enversConf.Audit<MetricCustomGoal>()
        .Exclude(x => x.MeasurableId);
      enversConf.Audit<MeetingConclusionModel>();
      enversConf.Audit<L10Note>().Exclude(x => x.CreateTime);
      enversConf.Audit<UserSettings>().Exclude(x => x.DrawerView);

      nhConf.IntegrateWithEnvers(enversConf);
    }


    private static void AddItem(List<string> list, string item, Stopwatch sw, RunningStatistics stats) {
      list.Add(item);
      stats.Push(sw.ElapsedMilliseconds);
      sw.Restart();
    }

    private static async Task BuildProductionMySqlSchema(Configuration config) {
      var swFull = Stopwatch.StartNew();
      //UPDATE DATABASE:
      var updates = new List<string>();
      var stats = new RunningStatistics();



      if(await Unsafe.ShouldUpdateDB(config)) {
        AuditForeignKeyInterceptor.Intercept(config, true);
        var su = new SchemaUpdate(config);
        var sw = Stopwatch.StartNew();
        su.Execute(x => AddItem(updates, x, sw, stats), true);
        await Unsafe.SetDBWasUpdatedSuccessfully(config);
        //Config.DbUpdateSuccessful();
        log.Info("[DatabaseUpdate] Done: Updated:" + stats.Count + "; Duration: " + swFull.ElapsedMilliseconds + "ms  Mean:" + stats.Mean + "ms  Std:" + stats.StandardDeviation + "ms");

      } else {
        log.Info("[DatabaseUpdate] Skipped. ");
      }

      var end = swFull.Elapsed;

      var auditEvents = new AuditEventListener();
      config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
      config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };


      config.SetProperty("command_timeout", "600");

    }

    public static DateTime GetDbTime(ISession s) {
      switch(Config.GetDatabaseType()) {
        case Config.DbType.MySql:
          return ((DateTime)s.CreateSQLQuery("select now();").List()[0]);
        case Config.DbType.Sqlite:
          var db = s.CreateSQLQuery("select CURRENT_TIMESTAMP;").List()[0];
          if(db is DateTime) {
            return (DateTime)db;
          }

          return DateTime.ParseExact((string)db, "yyyy-MM-dd HH:mm:ss", new System.Globalization.CultureInfo("en-us"));
        default:
          throw new NotImplementedException("Db type unknown");
      }
    }

    public static void ClearSessionFactory() {
      foreach(var f in factories) {
        factories.TryRemove(f.Key, out _);
      }
    }
  }
}
namespace NHibernate.Criterion {
  public static class ModHelper {
    public static Int64 Mod(this Int64 numericProperty, Int64 divisor) {
      throw new Exception("Not to be used directly - use inside QueryOver expression");
    }

    internal static IProjection ProcessMod(MethodCallExpression methodCallExpression) {
      IProjection property = ExpressionProcessor.FindMemberProjection(methodCallExpression.Arguments[0]).AsProjection();
      object divisor = ExpressionProcessor.FindValue(methodCallExpression.Arguments[1]);
      return Projections.SqlFunction("mod", NHibernateUtil.Int64, property, Projections.Constant(divisor));
    }
  }
}
