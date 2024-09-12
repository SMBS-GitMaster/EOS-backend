using System.Collections.Generic;

namespace RadialReview.Utilities.NHibernate {
	public class AuditForeignKeyInterceptorData {
		/**
		 * Somehow, Fluent began generating new foreign key names. The purpose of this file is to revert the foreign key names. 
		 * This file is run right before generating the schema updates. The foreign keys are replaced in the Nhibernate Configuration.
		 * 
		 * This file is needed because the Audit tables are huge. Deadlocks occur when altering these tables.
		 */

		public static List<Alteration> Alterations = new List<Alteration> {
			/*20190604*/
			new Alteration("ScoreModel_AUD","REVEND","REVINFO","REV","FKD240EBC0D257F2BD","FKC6A2300A3E446951"),
			new Alteration("MeasurableModel_AUD","REV","REVINFO","REV","FK78F4232FC9FA7DA8","FK94D153F1AAE62361"),
			new Alteration("MeasurableModel_AUD","REVEND","REVINFO","REV","FK78F4232FD257F2BD","FK94D153F13E446951"),
			new Alteration("Milestone_AUD","REV","REVINFO","REV","FKEDAEB2D5C9FA7DA8","FK6B43B098AAE62361"),
			new Alteration("Milestone_AUD","REVEND","REVINFO","REV","FKEDAEB2D5D257F2BD","FK6B43B0983E446951"),
			new Alteration("ReviewsModel_AUD","REV","REVINFO","REV","FK95DFF62DC9FA7DA8","FK865D5F18AAE62361"),
			new Alteration("ReviewsModel_AUD","REVEND","REVINFO","REV","FK95DFF62DD257F2BD","FK865D5F183E446951"),
			new Alteration("LongTuple_AUD","REV","REVINFO","REV","FK9B0456D9C9FA7DA8","FKF105B4FCAAE62361"),
			new Alteration("LongTuple_AUD","REVEND","REVINFO","REV","FK9B0456D9D257F2BD","FKF105B4FC3E446951"),
			new Alteration("ClientReviewModel_AUD","REV","REVINFO","REV","FKA2D1C6CFC9FA7DA8","FK37BAEC6CAAE62361"),
			new Alteration("ClientReviewModel_AUD","REVEND","REVINFO","REV","FKA2D1C6CFD257F2BD","FK37BAEC6C3E446951"),
			new Alteration("ClientReviewModel_LongModel_AUD","REV","REVINFO","REV","FK7FA088DDC9FA7DA8","FK711C9AD4AAE62361"),
			new Alteration("ClientReviewModel_LongModel_AUD","REVEND","REVINFO","REV","FK7FA088DDD257F2BD","FK711C9AD43E446951"),
			new Alteration("ClientReviewModel_LongTuple_AUD","REV","REVINFO","REV","FK5DEF5664C9FA7DA8","FKB0E403F2AAE62361"),
			new Alteration("ClientReviewModel_LongTuple_AUD","REVEND","REVINFO","REV","FK5DEF5664D257F2BD","FKB0E403F23E446951"),
			new Alteration("ReviewModel_AUD","REV","REVINFO","REV","FK9A3874FAC9FA7DA8","FKA381A883AAE62361"),
			new Alteration("ReviewModel_AUD","REVEND","REVINFO","REV","FK9A3874FAD257F2BD","FKA381A8833E446951"),
			new Alteration("QuestionCategoryModel_AUD","REV","REVINFO","REV","FK31FBCFF6C9FA7DA8","FK7F1E3569AAE62361"),
			new Alteration("QuestionCategoryModel_AUD","REVEND","REVINFO","REV","FK31FBCFF6D257F2BD","FK7F1E35693E446951"),
			new Alteration("PeriodModel_AUD","REV","REVINFO","REV","FK76128309C9FA7DA8","FKBA15289BAAE62361"),
			new Alteration("PeriodModel_AUD","REVEND","REVINFO","REV","FK76128309D257F2BD","FKBA15289B3E446951"),
			new Alteration("PaymentSpringsToken_AUD","REV","REVINFO","REV","FK4D87241EC9FA7DA8","FK990C31EEAAE62361"),
			new Alteration("PaymentSpringsToken_AUD","REVEND","REVINFO","REV","FK4D87241ED257F2BD","FK990C31EE3E446951"),
			new Alteration("PaymentPlanModel_AUD","REV","REVINFO","REV","FK9DFE472DC9FA7DA8","FKCAFB4C1CAAE62361"),
			new Alteration("PaymentPlanModel_AUD","REVEND","REVINFO","REV","FK9DFE472DD257F2BD","FKCAFB4C1C3E446951"),
			new Alteration("PaymentModel_AUD","REV","REVINFO","REV","FKE668C8C0C9FA7DA8","FKB5F92B9AAAE62361"),
			new Alteration("PaymentModel_AUD","REVEND","REVINFO","REV","FKE668C8C0D257F2BD","FKB5F92B9A3E446951"),
			new Alteration("LongModel_AUD","REV","REVINFO","REV","FK2E85EE8C9FA7DA8","FKDD143E46AAE62361"),
			new Alteration("LongModel_AUD","REVEND","REVINFO","REV","FK2E85EE8D257F2BD","FKDD143E463E446951"),
			new Alteration("LocalizedStringPairModel_AUD","REV","REVINFO","REV","FK1F028FE6C9FA7DA8","FK213C7463AAE62361"),
			new Alteration("LocalizedStringPairModel_AUD","REVEND","REVINFO","REV","FK1F028FE6D257F2BD","FK213C74633E446951"),
			new Alteration("LocalizedStringModel_AUD","REV","REVINFO","REV","FKF635CA2AC9FA7DA8","FK70C59D4AAAE62361"),
			new Alteration("LocalizedStringModel_AUD","REVEND","REVINFO","REV","FKF635CA2AD257F2BD","FK70C59D4A3E446951"),
			new Alteration("LocalizedStringModel_LocalizedStringPairModel_AUD","REV","REVINFO","REV","FKCED6A3C0C9FA7DA8","FK8173F0B4AAE62361"),
			new Alteration("LocalizedStringModel_LocalizedStringPairModel_AUD","REVEND","REVINFO","REV","FKCED6A3C0D257F2BD","FK8173F0B43E446951"),
			new Alteration("L10Recurrence_AUD","REV","REVINFO","REV","FKDCDE74FCC9FA7DA8","FKEBE37C04AAE62361"),
			new Alteration("L10Recurrence_AUD","REVEND","REVINFO","REV","FKDCDE74FCD257F2BD","FKEBE37C043E446951"),
			new Alteration("L10Recurrence_Page_AUD","REV","REVINFO","REV","FKD9449B18C9FA7DA8","FK14FD7F11AAE62361"),
			new Alteration("L10Recurrence_Page_AUD","REVEND","REVINFO","REV","FKD9449B18D257F2BD","FK14FD7F113E446951"),
			new Alteration("L10Meeting_AUD","REV","REVINFO","REV","FK17E2989FC9FA7DA8","FKBC3D1C11AAE62361"),
			new Alteration("L10Meeting_AUD","REVEND","REVINFO","REV","FK17E2989FD257F2BD","FKBC3D1C113E446951"),
			new Alteration("IssueModel_Recurrence_AUD","REV","REVINFO","REV","FKA5286A10C9FA7DA8","FKA4386A7DAAE62361"),
			new Alteration("IssueModel_Recurrence_AUD","REVEND","REVINFO","REV","FKA5286A10D257F2BD","FKA4386A7D3E446951"),
			new Alteration("IssueModel_AUD","REV","REVINFO","REV","FK9B8EB969C9FA7DA8","FK37EA5BC9AAE62361"),
			new Alteration("IssueModel_AUD","REVEND","REVINFO","REV","FK9B8EB969D257F2BD","FK37EA5BC93E446951"),
			new Alteration("InvoiceModel_AUD","REV","REVINFO","REV","FKE6CEF199C9FA7DA8","FK36FEA282AAE62361"),
			new Alteration("InvoiceModel_AUD","REVEND","REVINFO","REV","FKE6CEF199D257F2BD","FK36FEA2823E446951"),
			new Alteration("InvoiceItemModel_AUD","REV","REVINFO","REV","FK2C003926C9FA7DA8","FKFFDA2A55AAE62361"),
			new Alteration("InvoiceItemModel_AUD","REVEND","REVINFO","REV","FK2C003926D257F2BD","FKFFDA2A553E446951"),
			new Alteration("ImageModel_AUD","REV","REVINFO","REV","FK27A94113C9FA7DA8","FK5354B99EAAE62361"),
			new Alteration("ImageModel_AUD","REVEND","REVINFO","REV","FK27A94113D257F2BD","FK5354B99E3E446951"),
			new Alteration("IdentityUserLogin_AUD","REV","REVINFO","REV","FKC8FA5993C9FA7DA8","FKBE6017CBAAE62361"),
			new Alteration("IdentityUserLogin_AUD","REVEND","REVINFO","REV","FKC8FA5993D257F2BD","FKBE6017CB3E446951"),
			new Alteration("IdentityUserClaim_AUD","REV","REVINFO","REV","FK548E8A08C9FA7DA8","FKA1A4BA17AAE62361"),
			new Alteration("IdentityUserClaim_AUD","REVEND","REVINFO","REV","FK548E8A08D257F2BD","FKA1A4BA173E446951"),
			new Alteration("TileModel_AUD","REV","REVINFO","REV","FK52060F6AC9FA7DA8","FK1F0A5F8DAAE62361"),
			new Alteration("TileModel_AUD","REVEND","REVINFO","REV","FK52060F6AD257F2BD","FK1F0A5F8D3E446951"),
			new Alteration("Dashboard_AUD","REV","REVINFO","REV","FK77710397C9FA7DA8","FK4CEDC50EAAE62361"),
			new Alteration("Dashboard_AUD","REVEND","REVINFO","REV","FK77710397D257F2BD","FK4CEDC50E3E446951"),
			new Alteration("ResponsibilityGroupModel_AUD","REV","REVINFO","REV","FK8881D667C9FA7DA8","FK8E8CA910AAE62361"),
			new Alteration("ResponsibilityGroupModel_AUD","REVEND","REVINFO","REV","FK8881D667D257F2BD","FK8E8CA9103E446951"),
			new Alteration("ResponsibilityGroupModel_ResponsibilityModel_AUD","REV","REVINFO","REV","FKE42BA9BC9FA7DA8","FKD2CC79BCAAE62361"),
			new Alteration("ResponsibilityGroupModel_ResponsibilityModel_AUD","REVEND","REVINFO","REV","FKE42BA9BD257F2BD","FKD2CC79BC3E446951"),
			new Alteration("OrganizationModel_UserOrganizationModel_AUD","REV","REVINFO","REV","FK33C90B85C9FA7DA8","FKB5F10C31AAE62361"),
			new Alteration("OrganizationModel_UserOrganizationModel_AUD","REVEND","REVINFO","REV","FK33C90B85D257F2BD","FKB5F10C313E446951"),
			new Alteration("PositionModel_AUD","REV","REVINFO","REV","FKA337BA91C9FA7DA8","FKD611AAA5AAE62361"),
			new Alteration("PositionModel_AUD","REVEND","REVINFO","REV","FKA337BA91D257F2BD","FKD611AAA53E446951"),
			new Alteration("Askable_AUD","REV","REVINFO","REV","FKC0464EA2C9FA7DA8","FK6359861DAAE62361"),
			new Alteration("Askable_AUD","REVEND","REVINFO","REV","FKC0464EA2D257F2BD","FK6359861D3E446951"),
			new Alteration("QuestionModel_LongModel_AUD","REV","REVINFO","REV","FK675A974AC9FA7DA8","FKAE3251FDAAE62361"),
			new Alteration("QuestionModel_LongModel_AUD","REVEND","REVINFO","REV","FK675A974AD257F2BD","FKAE3251FD3E446951"),
			new Alteration("SurveyResponse_AUD","REV","REVINFO","REV","FK536192AC9FA7DA8","FKDDE4791BAAE62361"),
			new Alteration("SurveyResponse_AUD","REVEND","REVINFO","REV","FK536192AD257F2BD","FKDDE4791B3E446951"),
			new Alteration("Task_Camunda_AUD","REV","REVINFO","REV","FKBFD67F7EC9FA7DA8","FKB7FF963FAAE62361"),
			new Alteration("Task_Camunda_AUD","REVEND","REVINFO","REV","FKBFD67F7ED257F2BD","FKB7FF963F3E446951"),
			new Alteration("ProcessInstance_Camunda_AUD","REV","REVINFO","REV","FK913E7DFBC9FA7DA8","FK1982A1DCAAE62361"),
			new Alteration("ProcessInstance_Camunda_AUD","REVEND","REVINFO","REV","FK913E7DFBD257F2BD","FK1982A1DC3E446951"),
			new Alteration("ProcessDef_Camunda_AUD","REV","REVINFO","REV","FK8560D9F1C9FA7DA8","FKFFDAB4DCAAE62361"),
			new Alteration("ProcessDef_Camunda_AUD","REVEND","REVINFO","REV","FK8560D9F1D257F2BD","FKFFDAB4DC3E446951"),
			new Alteration("ProcessDef_CamundaFile_AUD","REV","REVINFO","REV","FKC99B3C67C9FA7DA8","FK769AC43AAAE62361"),
			new Alteration("ProcessDef_CamundaFile_AUD","REVEND","REVINFO","REV","FKC99B3C67D257F2BD","FK769AC43A3E446951"),

			//Synclock issue.. fixed via manually adding foreign key.
			//new Alteration("IssueModel_Recurrence", "CopiedFromId", "FK_FA3FF049", "FKEC30BCBA81EC8B0"),


		};

	}
}
