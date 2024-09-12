using System;
using System.Net;
using System.Threading.Tasks;
using RadialReview.Core.GraphQL.Common.DTO;

namespace RadialReview.Repositories {
  public partial class RadialReviewRepository : IRadialReviewRepository {
    public GraphQLResponse<bool> IframeEmbedCheck(string url) {

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "HEAD"; // Use the HEAD method to get only the response headers

        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
              string header = response.Headers["x-frame-options"];
                if (string.Equals(header, "SAMEORIGIN", StringComparison.OrdinalIgnoreCase) || string.Equals(header, "DENY", StringComparison.OrdinalIgnoreCase))
                {
                    return GraphQLResponse<bool>.Successfully(false);
                }
                else
                {
                    return GraphQLResponse<bool>.Successfully(true);
                }
            }
        }
        catch (WebException ex)
        {
            return GraphQLResponse<bool>.Error(ex);
        }
    }
  }
}