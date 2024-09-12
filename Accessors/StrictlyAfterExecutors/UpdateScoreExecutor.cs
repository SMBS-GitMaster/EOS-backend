using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Utilities.Types;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RadialReview.Core.Accessors.StrictlyAfterExecutors {
  public class UpdateScoreExecutor : IStrictlyAfter {
    public StrictlyAfterBehavior Behavior => new StrictlyAfterBehavior(true);

    public long scoreId { get; }
    public TOptional<decimal?> value { get; }
    public TOptional<string> notesText { get; set; }
    private ScoreModel Result { get; set; }

    public UpdateScoreExecutor(long scoreId, TOptional<decimal?> value, TOptional<string> notesText = default) {
      this.scoreId=scoreId;
      this.value=value;
      this.notesText = notesText;
      if (scoreId <= 0) {
        throw new PermissionsException("ScoreId was negative");
      }
    }

    public async Task EnsurePermitted(ISession s, PermissionsUtility perms) {
      perms.EditScore(scoreId);
    }
    public async Task AtomicUpdate(IOrderedSession s) {
      Result = await ScorecardAccessor.Restricted.UpdateScore_Unsafe(s, scoreId, value, notesText:notesText);
    }
    public async Task AfterAtomicUpdate(ISession s, PermissionsUtility perms) {
      //
    }

    public ScoreModel GetResult() {
      return Result;
    }
  }
}
