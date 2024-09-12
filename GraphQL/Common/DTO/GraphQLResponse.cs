using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using System;
using System.Collections.Generic;

namespace RadialReview.Core.GraphQL.Common.DTO {
  public class GraphQLResponse<T>
    : GraphQLResponseBase {
    public GraphQLResponse() { }

    public GraphQLResponse(T data)
      : base(true)
    {
      Data = data;
    }


    public GraphQLResponse(IEnumerable<ErrorDetail> errorDetails)
      : base(errorDetails)
    {
    }

    public GraphQLResponse(ErrorDetail errorDetail)
      : base(errorDetail)
    {
    }

    public static GraphQLResponse<T> Successfully(T data) => new GraphQLResponse<T>(data: data);

    public static GraphQLResponse<T> Error(IEnumerable<ErrorDetail> details)
    {
      return new GraphQLResponse<T>(details);
    }

    public static GraphQLResponse<T> Error() => new GraphQLResponse<T>()
      {
        ErrorDetails = new List<ErrorDetail>() { new ErrorDetail("An error has ocurred.", GraphQLErrorType.InternalError) },
        Success = false,
        Message = ErrorMessageBase
      };

    public static GraphQLResponse<T> Error(ErrorDetail detail) => new GraphQLResponse<T>()
    {
        ErrorDetails = new List<ErrorDetail>() { new ErrorDetail(detail.Message, GraphQLErrorType.InternalError) },
        Success = false,
        Message = ErrorMessageBase
    };

    public static GraphQLResponse<T> Error(Exception exception) {
      if (exception is PermissionsException) {
        var details = ErrorDetail.Forbidden();
        return Error(details);
      }
      if (exception is MeetingException)
      {
        var message = exception.Message;
        var details = ErrorDetail.Validation(message);
        return Error(details);
      }

        return Error();
    }

    public T Data { get; set; }
  }
}
