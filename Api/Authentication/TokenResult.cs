namespace RadialReview.Api.Authentication
{
  using System;

  public class TokenResult
  {
        public string Id { get; set; }
        public string Token { get; set; }
        public DateTime ValidTo { get; set; }
  }
}