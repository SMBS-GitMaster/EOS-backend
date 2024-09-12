using RadialReview.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Exceptions {

  public enum UploadExpectionStatus {
    Info,
    Warning,
    Error
  }

  public class UploadException : Exception, ISafeExceptionMessage {

    public UploadExpectionStatus Status { get; set; }

    public UploadException(UploadExpectionStatus status, string message) : base(message) {
    }
  }
}
