using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Utilities.RealTime {
	public class StatusUpdater {
		public class Status {
			public string Message { get; protected set; }
			public decimal Percentage { get; protected set; }
			public bool? ShowMessage { get; protected set; }
			public bool? ShowPercentage { get; protected set; }
			public int Timeout { get; protected set; }

			public Status() {
				Timeout = 6000; //6 seconds
			}

			public Status SetMessage(string message) {
				ShowMessage = true;
				Message = message;
				return this;
			}
			public Status SetPercentage(Ratio ratio) {
				return SetPercentage(ratio.Numerator, ratio.Denominator);
			}
			public Status SetPercentage(decimal numerator, decimal denominator) {
				ShowPercentage = true;
				if (denominator == 0)
					Percentage = 0;
				else
					Percentage = Math.Min(denominator, numerator) / denominator;
				return this;
			}
			public Status Hide() {
				return SetTimeout(0);
			}
			public Status SetTimeout(TimeSpan duration) {
				Timeout = (int)Math.Max(0, duration.TotalMilliseconds);
				return this;
			}
			public Status SetTimeout(int milliseconds) {
				return SetTimeout(TimeSpan.FromMilliseconds(milliseconds));
			}
			public Status Failed(string message = "Failed.") {
				ShowPercentage = false;
				return SetTimeout(6000).SetMessage(message);
			}
		}

		public StatusUpdater(RealTimeUtility.IBaseUpdater updater) {
			Updater = updater;
		}

		private RealTimeUtility.IBaseUpdater Updater { get; set; }

		private List<Action<Status>> _onStatusUpdate = new List<Action<Status>>();

		public void OnStatusUpdate(Action<Status> action) {
			if (action != null)
				_onStatusUpdate.Add(action);
		}

		public async Task<bool> UpdateStatus(Action<Status> status) {
			if (Updater != null) {
				var stat = new Status();
				//Manipulate
				status(stat);
				//Send to client right away
				await Updater.CallImmediately("status", stat);
				//Trigger events
				_onStatusUpdate.ForEach(x => x(stat));
				return true;
			}
			return false;
		}

		public static StatusUpdater NoOp() {
			return new StatusUpdater(null);
		}
	}
}
