using System;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class RemoveScriptAttribute : ValidationAttribute {	
	public RemoveScriptAttribute() {
		
	}

	protected override ValidationResult IsValid(object value, ValidationContext ctx) {
		var valueStr = value as string;
		var initalValueStr = value as string;
		if (valueStr != null) {		
			var sanitizer = new Ganss.XSS.HtmlSanitizer();

			valueStr = sanitizer.Sanitize(valueStr);

			if (initalValueStr != valueStr) {
				var prop = ctx.ObjectType.GetProperty(ctx.MemberName);
				prop.SetValue(ctx.ObjectInstance, valueStr);
			}
		}

		return null;
	}
}