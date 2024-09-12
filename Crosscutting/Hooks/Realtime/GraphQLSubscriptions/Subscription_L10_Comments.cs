using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Comments : ICommentHook //, IMeetingNoteHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Comments(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task CreateComment(ISession s, UserOrganizationModel caller, CommentModel comment)
    {
      var c = comment.TransformComment();

      switch (c.CommentParentType)
      {
        case ParentType.Todo:
          await _eventSender.SendChangeAsync(ResourceNames.Todo(c.ParentId), Change<IMeetingChange>.Created(c.Id, c)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Todo(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.TodoQueryModel.Collections.Comment2.Comments), c.Id, c)).ConfigureAwait(false);
          break;

        case ParentType.Issue:
          await _eventSender.SendChangeAsync(ResourceNames.Issue(c.ParentId), Change<IMeetingChange>.Created(c.Id, c)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Issue(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.IssueQueryModel.Collections.Comment3.Comments), c.Id, c)).ConfigureAwait(false);
          break;

        case ParentType.PeopleHeadline:
          //await _eventSender.SendChangeAsync(ResourceNames.Headline(c.ParentId), Change<IMeetingChange>.Created(c.Id, c)).ConfigureAwait(false);
          //await _eventSender.SendChangeAsync(ResourceNames.Headline(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.HeadlineModel.Collections.Comment.Comments), c.Id, c)).ConfigureAwait(false);
          break;
      }
    }

    public async Task UpdateComment(ISession s, UserOrganizationModel caller, CommentModel note, ICommentHookUpdates updates)
    {
      await SendUpdatedEventOnChannels(note, updates);
    }

    private async Task SendUpdatedEventOnChannels(CommentModel comment, ICommentHookUpdates updates)
    {
      var c = comment.TransformComment();

      switch (c.CommentParentType)
      {
        case ParentType.Todo:
          var todo_targets = new[] {
            new ContainerTarget
            {
              Type = "todo",
              Id = c.ParentId,
              Property = "COMMENTS"
            }
          };
            await _eventSender.SendChangeAsync(ResourceNames.Todo(c.ParentId), Change<IMeetingChange>.Updated(c.Id, c, todo_targets)).ConfigureAwait(false);
            //await _eventSender.SendChangeAsync(ResourceNames.Todo(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.TodoModel.Collections.Comment2.Comments), c.Id, c)).ConfigureAwait(false);
          break;

        case ParentType.Issue:
          var issue_targets = new[] {
            new ContainerTarget
            {
              Type = "issue",
              Id = c.ParentId,
              Property = "COMMENTS"
            }
          };
          await _eventSender.SendChangeAsync(ResourceNames.Issue(c.ParentId), Change<IMeetingChange>.Updated(c.Id, c, issue_targets)).ConfigureAwait(false);
          //await _eventSender.SendChangeAsync(ResourceNames.Issue(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.IssueModel.Collections.Comment3.Comments), c.Id, c)).ConfigureAwait(false);
          break;

        case ParentType.PeopleHeadline:
          //var headline_targets = new[] {
          //  new ContainerTarget
          //  {
          //    Type = "issue",
          //    Id = c.ParentId,
          //    Property = "COMMENTS"
          //  }
          //};
          //await _eventSender.SendChangeAsync(ResourceNames.Headline(c.ParentId), Change<IMeetingChange>.Updated(c.Id, c, headline_targets)).ConfigureAwait(false);
          //await _eventSender.SendChangeAsync(ResourceNames.Headline(c.ParentId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(c.ParentId, GQL.Models.HeadlineModel.Collections.Comment.Comments), c.Id, c)).ConfigureAwait(false);
          break;
      }
    }
  }
}
