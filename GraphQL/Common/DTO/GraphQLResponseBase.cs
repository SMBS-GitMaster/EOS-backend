using RadialReview.Exceptions;
using System;
using System.Collections.Generic;

namespace RadialReview.Core.GraphQL.Common.DTO {
  public class GraphQLResponseBase {

    public const string ErrorMessageBase = "Not able to perform the action.";

    public GraphQLResponseBase() { }

    public GraphQLResponseBase(bool success)
    {
      Success = success;
    }


    public GraphQLResponseBase(bool success, string message)
    {
      Success = success;
      Message = message;
    }

    public GraphQLResponseBase(IEnumerable<ErrorDetail> details)
    {
      ErrorDetails = details;
      Success = false;
      Message = ErrorMessageBase;
    }

    public GraphQLResponseBase(ErrorDetail detail) => new GraphQLResponseBase(new List<ErrorDetail>() { detail});


    public static GraphQLResponseBase Successfully() => new GraphQLResponseBase(success: true);
    public static GraphQLResponseBase Error() => new GraphQLResponseBase(new List<ErrorDetail>() { new ErrorDetail("An error has ocurred.", GraphQLErrorType.InternalError) });
    public static GraphQLResponseBase Error(ErrorDetail detail) => new GraphQLResponseBase(new List<ErrorDetail>() { detail });
    public static GraphQLResponseBase Error(IEnumerable<ErrorDetail> details) => new GraphQLResponseBase(details);
    public static GraphQLResponseBase Error(Exception exception) {
      if (exception is PermissionsException) {
        var details = ErrorDetail.Forbidden();
        return Error(details);
      }

      return Error();
    }
    public IEnumerable<ErrorDetail> ErrorDetails { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
  }
}
