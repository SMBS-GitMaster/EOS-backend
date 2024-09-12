using RadialReview.Utilities;
using System;

namespace RadialReview.Accessors {
	public class AwsMetadataAccessor {

		public static string InstanceId = null;

		public static string GetInstanceId() {
			if (InstanceId == null) {

				if (Config.IsLocal()) {
					InstanceId = "i-local";
				} else {

					try {
						InstanceId =  Amazon.Util.EC2InstanceMetadata.InstanceId.ToString();
					} catch (Exception e) {
						InstanceId = "?";
					}
				}
			}
			return InstanceId;
		}
	}
}