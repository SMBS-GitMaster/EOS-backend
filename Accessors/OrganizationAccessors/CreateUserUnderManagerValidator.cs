using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Core.Properties;

namespace RadialReview.Accessors.OrganizationAccessors {

	public class CreateUserUnderManagerValidator {

		private readonly string _firstName;
		private readonly string _lastName;
		private readonly string _email;
		private readonly bool _isPlaceholder;
		private readonly ISession _databaseSession;

		public CreateUserUnderManagerValidator(string firstName, string lastName, string email, bool isPlaceholder, ISession session) {
			_firstName = firstName;
			_lastName = lastName;
			_email = email;
			_isPlaceholder = isPlaceholder;
			_databaseSession = session;
		}

		public void Execute() {
			if (!_isPlaceholder && !Emailer.IsValid(_email))
				throw new PermissionsException(ExceptionStrings.InvalidEmail);

			if (string.IsNullOrEmpty(_firstName))
				throw new PermissionsException("First name cannot be empty.") { NoErrorReport = true };

			if (string.IsNullOrEmpty(_lastName))
				throw new PermissionsException("Last name cannot be empty.") { NoErrorReport = true };

			if (_isPlaceholder && string.IsNullOrEmpty(_email))
				return;

		}
	}
}
