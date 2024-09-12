using NHibernate.Criterion;
using RadialReview.Models.Documents;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Documents.Queries;

using RadialReview.Models.Documents.Interceptors;
using System.Threading.Tasks;
using NHibernate;
using System;
using RadialReview.Models;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {

    private delegate QueryOver<DocumentItemLocation> ItemIdCriterionGenerator(DocumentItemType documentType);


    public static IEnumerable<IDocumentItemQueryMethods_Unsafe> QueryMethods = ReflectionUtility.GetAllImplementationsOfInterface<IDocumentItemQueryMethods_Unsafe>();
    public static IEnumerable<IDocumentFolderInterceptor_Unsafe> FolderInterceptors = ReflectionUtility.GetAllImplementationsOfInterface<IDocumentFolderInterceptor_Unsafe>();

    public static IDocumentItemQueryMethods_Unsafe GetQueryMethodsForType(DocumentItemType type) {
      return QueryMethods.Single(x => x.ForItemType() == type);
    }


  }
}