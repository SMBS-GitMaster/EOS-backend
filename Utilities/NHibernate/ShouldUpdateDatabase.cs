using NHibernate.Cfg;
using NHibernate.Dialect;
using RadialReview.Utilities.Encrypt;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Utilities.NHibernate {
	public class ShouldUpdateDatabase {

		private readonly Configuration configuration;

		private readonly Dialect dialect;

		public ShouldUpdateDatabase(Configuration cfg): this(cfg, cfg.Properties) {
		}

		private ShouldUpdateDatabase(Configuration cfg, IDictionary<string, string> configProperties) {
			configuration = cfg;
			dialect = Dialect.GetDialect(configProperties);
			Dictionary<string, string> dictionary = new Dictionary<string, string>(dialect.DefaultProperties);
			foreach (KeyValuePair<string, string> configProperty in configProperties) {
				dictionary[configProperty.Key] = configProperty.Value;
			}			
		}
		public async Task<string> GetDatabaseCreationHash() {
			string[] createSQL = configuration.GenerateSchemaCreationScript(dialect);
			var joined = string.Join("\n", createSQL);
			return Crypto.UniqueHash(joined);
		}
	}
}
