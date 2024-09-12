﻿using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {
    public PermissionsUtility SubscribeToEvent(long subscriberUserId, IEventAnalyzerGenerator analyzer) {
      var permChecked = false;
      Self(subscriberUserId);
      if (analyzer is IEventAnalyzerGenerator) {
        ViewL10Recurrence(((IRecurrenceEventAnalyerGenerator)analyzer).RecurrenceId);
        permChecked = true;
      }

      if (permChecked) {
        return this;
      }

      throw new PermissionsException("no permissions were checked");
    }
    public PermissionsUtility ViewEvent(long eventId, IEventAnalyzerGenerator analyzer) {
      var evt = session.Get<EventSubscription>(eventId);
      return SubscribeToEvent(evt.SubscriberId, analyzer);
    }
  }
}
