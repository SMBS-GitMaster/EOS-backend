using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Middleware {
	public static class SwaggerMiddleware {
		public static void ConfigureSwagger(this IServiceCollection services) {
			services.AddSwaggerGen(options => {


				options.SwaggerDoc("v0", new OpenApiInfo { Version = "v0", Title = "Bloom Growth v0 API" });
				options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "Bloom Growth v1 API" });
				options.SwaggerDoc("v2", new OpenApiInfo { Version = "v2", Title = "Bloom Growth v2 API (release candidate)" });

				if (Config.IsLocal()) {
					options.SwaggerDoc("unset", new OpenApiInfo { Version = "unset", Title = "Sanity Check for [FAILED TO MAP NAMESPACE TO VERSION]" });
				}

				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
					Description = "JWT Bearer Authentication",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT"
				});
				options.AddSecurityRequirement(new OpenApiSecurityRequirement{{
						new OpenApiSecurityScheme{
							Reference = new OpenApiReference { 
								Type = ReferenceType.SecurityScheme, Id = "Bearer"
							}
						},
						new string[]{}
					}
				});


				options.IgnoreObsoleteActions();
				options.IncludeXmlComments(string.Format(@"{0}/XmlComments.xml", System.AppDomain.CurrentDomain.BaseDirectory));

				options.CustomSchemaIds(schemaIdStrategy);
				options.IgnoreObsoleteProperties();
				options.SchemaFilter<EnumSchemaFilter>();

				options.SchemaFilter<ClassNameSchemaFilter>();


				options.ResolveConflictingActions(x => {
					return x.First();
				});
			});
			services.AddSwaggerGenNewtonsoftSupport();
		}

		public static void ConfigureSwaggerUI(this IApplicationBuilder app) {


			app.UseSwaggerUI(x => {
				if (Config.IsLocal()) {
					x.SwaggerEndpoint("/swagger/unset/swagger.json", "Sanity Check for [FAILED TO MAP NAMESPACE TO VERSION]");
				}
				x.SwaggerEndpoint("/swagger/v1/swagger.json", "Bloom Growth v1 (stable)");
				x.SwaggerEndpoint("/swagger/v0/swagger.json", "Bloom Growth v0");
				x.SwaggerEndpoint("/swagger/v2/swagger.json", "Bloom Growth v2 (RC)");


				x.DocumentTitle = "Bloom Growth API";
				x.InjectStylesheet("/wwwroot/Swagger/custom.css");
				x.InjectJavascript("/wwwroot/Swagger/custom.js");

				x.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

			});
		}




		#region Helpers

		private static DefaultDictionary<string, bool> classNameSeen = new DefaultDictionary<string, bool>(x => false);
		private static DefaultDictionary<Type, string> className = new DefaultDictionary<Type, string>(x => {
			string returnedValue = x.Name;
			while (classNameSeen[returnedValue]) {
				returnedValue += "_";
			}
			classNameSeen[returnedValue] = true;
			return returnedValue;
		});
		private static string schemaIdStrategy(Type currentClass) {

			var returnedValue = className[currentClass];


			foreach (var customAttributeData in currentClass.CustomAttributes) {
				if (customAttributeData.AttributeType.Name.ToLower() == "swaggernameattribute") {
					foreach (var argument in customAttributeData.NamedArguments) {
						if (argument.MemberName.ToLower() == "name" && argument.TypedValue.Value != null) {
							returnedValue = argument.TypedValue.Value.ToString();
						}
					}
				}
			}

			if (returnedValue.StartsWith("Angular")) {
				returnedValue = returnedValue.SubstringAfter("Angular");
				while (classNameSeen[returnedValue]) {
					returnedValue += "_";
				}
				classNameSeen[returnedValue] = true;
			}

			if (returnedValue.EndsWith("Model")) {
				returnedValue = returnedValue.SubstringBefore("Model");
				while (classNameSeen[returnedValue]) {
					returnedValue += "_";
				}
				classNameSeen[returnedValue] = true;
			}


			className[currentClass] = returnedValue;
			return returnedValue;
		}

		private class EnumSchemaFilter : ISchemaFilter {
			public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
				if (context.Type.IsEnum) {
					var enumValues = schema.Enum.ToArray();
					var i = 0;
					schema.Enum.Clear();
					foreach (var n in Enum.GetNames(context.Type).ToList()) {
						schema.Enum.Add(new OpenApiString(n ));
						i++;

					}
				}
			}
		}


		public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention {

			private List<string> AllowedNames;

			public ApiExplorerGroupPerVersionConvention(string[] allowedNames) {
				AllowedNames = allowedNames.Select(x => x.ToLower()).ToList();
			}

			public void Apply(ControllerModel controller) {
				var controllerNamespace = controller.ControllerType.Namespace; 
				var split = controllerNamespace.ToLower().Split('.');
				if (split.Any(x => x == "api") && split.Last().StartsWith("v") && AllowedNames.Contains(split.Last())) {
					var apiVersion = split.Last().ToLower();
					controller.ApiExplorer.GroupName = apiVersion;
				} else {
					controller.ApiExplorer.GroupName = "unset";
				}

			}
		}



		private class ClassNameSchemaFilter : ISchemaFilter {
			public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
				var type = context.Type;
				var attributes = Attribute.GetCustomAttributes(context.Type, typeof(SwaggerNameAttribute));
				var found = attributes.Select(x => (SwaggerNameAttribute)x).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name));

				if (found != null) {
					schema.Title = found.Name;
				}
			}
		}
		#endregion
	}
}

namespace RadialReview {

	public class SwaggerNameAttribute : Attribute {
		public string Name { get; set; }
		public SwaggerNameAttribute() {

		}
	}
}
