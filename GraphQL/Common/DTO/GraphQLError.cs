using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Core.GraphQL.Common.DTO {
  public sealed class ErrorDetail
  {
    public ErrorDetail(string message, GraphQLErrorType type)
    {
      Message = message;
      Type = type;
    }

    public static ErrorDetail Forbidden() => new ErrorDetail("User not allowed to perform this action", GraphQLErrorType.Forbidden);
    public static ErrorDetail Validation(string message) => new ErrorDetail(message, GraphQLErrorType.Validation);
    public static IEnumerable<ErrorDetail> Validation(IEnumerable<string> messages) => messages.Select(x => Validation(x));
    public static ErrorDetail InternalError() => new ErrorDetail("An error has ocurred.", GraphQLErrorType.InternalError);

    public string Message { get; set; }
    public GraphQLErrorType Type { get; set; }
  }

  public enum GraphQLErrorType {
    Forbidden,
    InternalError,
    Validation
  }
}
