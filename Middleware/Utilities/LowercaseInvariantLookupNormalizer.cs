using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;

namespace RadialReview.Middleware.Utilities {
	public class LowercaseInvariantLookupNormalizer : ILookupNormalizer {
        [return: NotNullIfNotNull("name")]
        public string? NormalizeName(string? name) {
            if (name == null) {
                return null;
            }
            return name.Normalize().ToLowerInvariant().Trim();
        }

        /// <summary>
        /// Returns a normalized representation of the specified <paramref name="email"/>.
        /// </summary>
        /// <param name="email">The email to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="email"/>.</returns>
        [return: NotNullIfNotNull("email")]
        public string? NormalizeEmail(string? email) => NormalizeName(email);
    }
}
