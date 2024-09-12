using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.TwiML.Voice;

namespace RadialReview.Core.Middleware.Request.GraphQL {
  public class GraphQLNHibernateHookInterceptor : ExecutionDiagnosticEventListener {
    private const string IS_ACTIVE = "IsActive";
    private const string POST_GRAPHQL_ACTIONS = "PostGraphQLActions";
    public const string GRAPHQL_NHIBERNATE_HOOK_INTERCEPTOR = "GraphQLNHibernateHookInterceptor";
    private readonly HttpContext httpContext;
    private readonly ConcurrentBag<Action> _actions;

    public GraphQLNHibernateHookInterceptor() {
     //this.httpContext=httpContext;
      _actions= new ConcurrentBag<Action>();
    }

    public override IDisposable ExecuteRequest(IRequestContext context) {
      lock (context)
      {
        context.ContextData[IS_ACTIVE] = true;
        context.ContextData[POST_GRAPHQL_ACTIONS] = _actions;
        context.ContextData[GRAPHQL_NHIBERNATE_HOOK_INTERCEPTOR] = this;      

        if (context.ContextData.ContainsKey("HttpContext"))
        {
          ((DefaultHttpContext)context.ContextData["HttpContext"]).Items[GRAPHQL_NHIBERNATE_HOOK_INTERCEPTOR] = this;
        }
        return new RequestScope(context);
      }
    }

    public void AddAction(Action action) {
      _actions.Add(action);
    }



    public class RequestScope : IDisposable {
      private IRequestContext context;

      public RequestScope(IRequestContext context) {
        this.context=context;
      }

      public void Dispose() {
        context.ContextData[IS_ACTIVE] = false;
        object obj;
        if (context != null && context.ContextData.TryGetValue(POST_GRAPHQL_ACTIONS, out obj)) {
          var actions = obj as ConcurrentBag<Action>;
          if (actions != null) {
            Action action;
            while (actions.TryTake(out action)) {
              try {
                action();
              } catch (Exception ex) {
                Debug.WriteLine("Failed to run GraphQL hook actions");
                Debug.WriteStackTrace(ex.Message);
              }
            }
          }
        }

      }
    }
  }
  //public class GraphQLNHibernateHookMiddleware : RequestCoreMiddleware {

  //}
}
