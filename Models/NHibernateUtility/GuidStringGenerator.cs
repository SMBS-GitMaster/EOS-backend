using NHibernate.Engine;
using NHibernate.Id;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Models {
	public class GuidStringGenerator : IIdentifierGenerator {
		public object Generate(ISessionImplementor session, object obj) {
			return new GuidCombGenerator().Generate(session, obj).ToString();
		}

		public async Task<object> GenerateAsync(ISessionImplementor session, object obj, CancellationToken cancellationToken) {
			return (await new GuidCombGenerator().GenerateAsync(session, obj, cancellationToken)).ToString();
		}
	}
}
