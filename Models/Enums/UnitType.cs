using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Enums {
	[JsonConverter(typeof(StringEnumConverter))]
	public enum UnitType {
		[Display(Name = "No units")] [Description("No units")] None = 0,
		[Display(Name = "Dollars")] [Description("Dollars")] Dollar = 1,
		[Display(Name = "Percent")] [Description("Percent")] Percent = 2,
		[Display(Name = "Pounds")] [Description("Pounds")] Pound = 3,
		[Display(Name = "Euros")] [Description("Euros")] Euros = 4,
		[Display(Name = "Pesos")] [Description("Pesos")] Pesos = 5,
		[Display(Name = "Yen")] [Description("Yen")] Yen = 6,
    [DoNotDisplay]
    [Display(Name = "YesNo")][Description("YesNo")] YesNo = 7,
    [Display(Name = "INR")][Description("INR")] INR = 8

  }

  public enum Frequency
  {
    [Display(Name = "WEEKLY")] [Description("WEEKLY")] WEEKLY = 0,
    [Display(Name = "MONTHLY")] [Description("MONTHLY")] MONTHLY = 1,
    [Display(Name = "QUARTERLY")] [Description("QUARTERLY")] QUARTERLY = 2,
    [Display(Name = "DAILY")][Description("DAILY")] DAILY = 3,
  }

  public enum TrackedMetricColor
  {
    [Display(Name = "COLOR1")] [Description("COLOR1")] COLOR1 = 0,
    [Display(Name = "COLOR2")] [Description("COLOR2")] COLOR2 = 1,
    [Display(Name = "COLOR3")] [Description("COLOR3")] COLOR3 = 2,
    [Display(Name = "COLOR4")] [Description("COLOR4")] COLOR4 = 3,
    [Display(Name = "COLOR5")] [Description("COLOR5")] COLOR5 = 4,
  }

}

namespace RadialReview {
	public static class UnitTypeExtensions {
		public static string ToTypeString(this UnitType type) {
			switch (type) {
				case UnitType.None:
					return "units";
				case UnitType.Dollar:
					return "dollars";
				case UnitType.Percent:
					return "%";
				case UnitType.Pound:
					return "pounds";
				case UnitType.Euros:
					return "euros";
				case UnitType.Pesos:
					return "pesos";
				case UnitType.Yen:
					return "yen";
        case UnitType.YesNo:
          return "yesno";
        case UnitType.INR:
          return "Rs.";
        default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string Format(this UnitType type, string value) {
			switch (type) {
				case UnitType.None:
					return string.Format("{0}", value);
				case UnitType.Dollar:
					return string.Format("${0}", value);
				case UnitType.Percent:
					return string.Format("{0}%", value);
				case UnitType.Pound:
					return string.Format("£{0}", value);
				case UnitType.Euros:
					return string.Format("€{0}", value);
				case UnitType.Pesos:
					return string.Format("₱{0}", value);
				case UnitType.Yen:
					return string.Format("¥{0}", value);
        case UnitType.YesNo:
          return value == "1" ? "Yes" : "No";
        case UnitType.INR:
          return string.Format("Rs.{0}", value);
        default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string Format(this UnitType type, decimal value) {
			return Format(type, string.Format("{0:#,##0.####}", value));
		}
	}
}
