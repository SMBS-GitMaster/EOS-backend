using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Controllers {
  public class LogResponseWriter : IDisposable {
    private StreamWriter Writer { get;  set; }
    private HttpContext HttpContext { get;  set; }

    public LogResponseWriter(HttpContext httpContext) {
      HttpContext = httpContext;
      HttpContext.Response.ContentType = "text/event-stream";
      Writer = new StreamWriter(HttpContext.Response.Body);
    }

    public bool IsCancelled() {
      return HttpContext.RequestAborted.IsCancellationRequested;
    }

    public async Task Write(string message) {
      if (!IsCancelled()) {
        await Writer.WriteAsync(message);
        await Writer.FlushAsync();
      }
    }

    public async Task WriteLine(string message) {
      await Write(message+"\n");
    }

    public async Task WriteLineWithTimestamp(string message) {
      await Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff")+": "+ message+"\n");
    }

    public void Dispose() {
      Writer?.Dispose();
    }
  }
}
