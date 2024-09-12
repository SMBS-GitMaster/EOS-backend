using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public class DefaultOrg {

    public class KeyValue {
      public KeyValue() {
      }
      public KeyValue(string value, string text, string showBoxLabel) {
        this.value = value;
        this.text = text;
        this.showBoxLabel = showBoxLabel;
      }
      public KeyValue(string value, string text) {
        this.value = value;
        this.text = text;
        this.showBoxLabel = null;
      }
      public string value { get; set; }
      public string text { get; set; }
      public string showBoxLabel { get; set; }
    }
    public bool EnableL10 { get; set; }
    public bool EnablelAC { get; set; }
    public bool EnableZapier { get; set; }
    public bool EnableDocs { get; set; }
    public bool EnablePeopleTools { get; set; }
    public bool EnableProcess { get; set; }
    public bool EnableOldReview { get; set; }
    public int MaxGuessDistance { get; set; }
    public int GuessThreshold { get; set; }
    public int TrialDays { get; set; }

    public List<KeyValue> ReferralSources { get; set; }
    public List<KeyValue> IndustryList { get; set; }
    public List<KeyValue> CompanySizeList { get; set; }



    public List<Tuple<string, EosUserType, int?>> GuessPairs { get; set; }

    public DefaultOrg(bool initializeList = false) {
      EnableL10 = true;
      EnablelAC = true;
      EnableZapier = false;
      EnableDocs = false;
      EnablePeopleTools = false;
      EnableProcess = false;
      EnableOldReview = false;
      TrialDays = 30;

      GuessThreshold = 5;
      MaxGuessDistance = 3;

      if(initializeList) {
        ReferralSources = new List<KeyValue>() {
          new KeyValue("implementer",  "From a Bloom Growth Guide","Which Bloom Growth Guide?"),
          new KeyValue("coach", "From a Coach or Implementer","Which Coach?"),
          new KeyValue("networkGroup", "From my Networking Group (i.e. EO®/Vistage®/Masterminds/etc.)","Which Group?"),
          new KeyValue("event", "At an Event","Which Event?"),
          new KeyValue("client", "From an existing Bloom Growth client","Which Client?"),
          new KeyValue("online", "I Found You Online"),
        };
        IndustryList = new List<KeyValue>() {
          new KeyValue("agribusiness_food_systems", "Agribusiness/Food Systems" ),
          new KeyValue("aviation", "Aviation" ),
          new KeyValue("construction", "Construction" ),
          new KeyValue("entertainment", "Entertainment" ),
          new KeyValue("financial_institutions", "Financial Institutions" ),
          new KeyValue("healthcare", "Healthcare" ),
          new KeyValue("manufacturing_heavy_industry", "Manufacturing & Heavy Industry" ),
          new KeyValue("marine", "Marine" ),
          new KeyValue("natural_resources", "Natural Resources" ),
          new KeyValue("pharmaceutical_chemical", "Pharmaceutical / Chemical" ),
          new KeyValue("professional_services", "Professional Services" ),
          new KeyValue("public_sector", "Public Sector" ),
          new KeyValue("railway", "Railway" ),
          new KeyValue("real_estate", "Real Estate" ),
          new KeyValue("technology", "Technology" ),
          new KeyValue("trucking", "Trucking" ),
          new KeyValue("wholesale_trade", "Wholesale Trade" ),
          new KeyValue("other", "Other" )
        };

        CompanySizeList = new List<KeyValue>(){
          new KeyValue("1-10", "1-10" ),
          new KeyValue("11-25", "11-25" ),
          new KeyValue("26-50", "26-50" ),
          new KeyValue("51-100", "51-100" ),
          new KeyValue("101-250", "101-250" ),
          new KeyValue("250+", "250+" ),
        };


        GuessPairs = new List<Tuple<string, EosUserType, int?>>() {
          Tuple.Create("visionary",EosUserType.Visionary,(int?)null),
          Tuple.Create("integrator",EosUserType.Integrator ,(int?)null),
          Tuple.Create("sys admin",EosUserType.SystemAdmin ,(int?)null),
          Tuple.Create("human resources", EosUserType.HR ,(int?)null),
          Tuple.Create("operations", EosUserType.Ops ,(int?)null),
          Tuple.Create("ops", EosUserType.Ops ,(int?)0),
          Tuple.Create("hr", EosUserType.HR ,(int?)0),
          Tuple.Create("system admin", EosUserType.SystemAdmin,(int?)null),
          Tuple.Create("system administrator", EosUserType.SystemAdmin,(int?)null),
          Tuple.Create("sales and marketing", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("sales & marketing", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("marketing & sales", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("marketing/sales", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("sales", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("marketing", EosUserType.SalesOrMarketing,(int?)null),
          Tuple.Create("eos implementer",EosUserType.Implementor ,(int?)null),
          Tuple.Create("eos implementor",EosUserType.Implementor ,(int?)null),
          Tuple.Create("eos coach",EosUserType.Implementor ,(int?)null),
          Tuple.Create("implementer",EosUserType.Implementor ,(int?)null),
          Tuple.Create("implementor",EosUserType.Implementor ,(int?)null),
          Tuple.Create("coach",EosUserType.Implementor ,(int?)null),
        };
      }
    }
  }
}
