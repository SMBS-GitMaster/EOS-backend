using Microsoft.Extensions.Logging;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.ScheduledTasks {
	public class ChargeAccountHandler : IScheduledTaskHandler {
		public async Task Handle(ScheduledTaskHandlerData data) {
			var pathParts = data.Url.AbsolutePath.Split("/");
			var organizationId = pathParts.Last().ToLong();
			var log = data.Logger;
			var taskId = data.TaskId;

			try {
				var jobId = await PaymentAccessor.EnqueueChargeOrganizationFromTask(organizationId, taskId, data.IsTest, executeTime: data.Now);
				log.LogInformation("ChargingOrganizationEnqueued(" + organizationId + ")");
			} catch (PaymentException paymentException) {
				await PaymentAccessor.Unsafe.RecordCapturedPaymentException(paymentException, taskId);
				if (paymentException.Type !=PaymentExceptionType.Fallthrough)
					throw;
			} catch (FallthroughException fallthroughException) {
				log.LogError("FallthroughCaptured", fallthroughException);
				return;
			} catch (Exception unknownException) {
				await PaymentAccessor.Unsafe.RecordUnknownPaymentException(unknownException, organizationId, taskId);
				throw new Exception("Unhandled Payment Exception");
			}
		}


		public bool ShouldHandle(ScheduledTaskHandlerData data) {
			return data.Url.AbsolutePath.ToLower().StartsWith("/scheduler/chargeaccount/") && data.TaskName.ToLower() == "monthly_payment_plan";
		}
	}
}
