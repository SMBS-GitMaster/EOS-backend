using FluentNHibernate.Mapping;
using RadialReview.Models;
using System;
using System.Collections.Generic;

namespace RadialReview.Areas.CoreProcess.Models {


	public enum RequestTypeEnum {
        isHookRegistryAction,
        isHTTPRequest
    }

    public class MessageQueueModel {
        public string Identifier { get; set; }
        public object Model { get; set; }
        public string ModelType { get; set; } // name of model
        public string ReceiptHandle { get; set; }
        public string ApiUrl { get; set; }
        public long? UserOrgId { get; set; }
        public string UserName { get; set; }
        public Type type { get; set; }
        public RequestTypeEnum RequestType { get; set; }

        public string SerializedModel { get; set; }
    }

    public class SerializableHook {
        public object lambda { get; set; }
        public Type type { get; set; }
		public Dictionary<string,object> hookData { get; set; }
    }

    public class TokenIdentifier {
        public virtual string TokenKey { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public TokenIdentifier() {
            CreateTime = DateTime.UtcNow;
        }
    }

    public class TokenIdentifierMap : ClassMap<TokenIdentifier> {
        public TokenIdentifierMap() {
            Id(x => x.TokenKey).GeneratedBy.Assigned();
            Map(x => x.CreateTime);
        }
    }

}
