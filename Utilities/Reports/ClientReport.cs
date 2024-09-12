using MathNet.Numerics.Interpolation;
using RadialReview.Models.Events;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Reports {
  public class ClientReport {
    public ClientReport(long orgId) {
      LogLines = new LogFile<LogLine>();
      //Tickets = new List<Tickets.Ticket>();
      Users = new List<UserLookup>();
      Events = new List<AccountEvent>();
      OrgId = orgId;
    }

    public string OrgName { get; set; }
    public string OrgStatus { get; set; }
    public DateTime OrgCreateTime { get; set; }
    public DateTime? OrgDeleteTime { get; set; }

    public long OrgId { get; set; }
    public LogFile<LogLine> LogLines { get; set; }
    //public List<Ticket> Tickets { get; set; }
    public List<AccountEvent> Events { get; set; }
    public List<UserLookup> Users { get; set; }
    public double ReportGenerationCost { get; set; }

    public class Ticket {
    }

    //public class TractionToolsUser {
    //  public long UserId { get; set; }
    //  public long OrgId { get; set; }
    //  public string Email { get; set; }
    //  public string Name { get; set; }
    //  public string Positions { get; set; }
    //  public DateTime? LastLogin { get; set; }
    //  public DateTime CreateTime { get; set; }
    //  public DateTime? DeleteTime { get; set; }
    //  public bool Deleted { get { return DeleteTime != null; } }
    //}
  }
}
