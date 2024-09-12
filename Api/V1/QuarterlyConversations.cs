using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Utilities;
using System.Net;
using System;
using RadialReview.Api.Common;
using Microsoft.AspNetCore.Http;
using RadialReview.Api;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Models.Survey;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace RadialReview.Core.Api.V1 {
  [Route("api/v1")]
  public class QuarterlyConversations : BaseApiController {

    public QuarterlyConversations(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

    /// <summary>
    /// This endpoint is for getting detailed data about a specific quarterly conversation
    /// </summary>
    /// <param name="QUARTERLY_CONVERSATION_ID"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("quarterlyconversations/data/{QUARTERLY_CONVERSATION_ID:long}")]
    public async Task<AngularSurveyContainer> Data(long QUARTERLY_CONVERSATION_ID) {
        var output = SurveyAccessor.GetAngularSurveyContainerBy(GetUser(), GetUser(), QUARTERLY_CONVERSATION_ID);
        return output;
    }

    /// <summary>
    /// This endpoint is for getting all of the quarterly conversations and data that have been created
    /// </summary>
    [HttpGet]
    [Route("quarterlyconversations/")]
    public async Task<IOrderedEnumerable<AngularSurveyContainer>> Containers() {
      var containers = SurveyAccessor.GetSurveyContainersBy(GetUser(), GetUser(), SurveyType.QuarterlyConversation).OrderByDescending(x => x.IssueDate);
      return containers;

    }
  }

}
