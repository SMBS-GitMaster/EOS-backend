using System;
using System.IO;

namespace RadialReview.Utilities.Build
{
    public static partial class BuildConstants
    {
        public static DateTime CompilationTimestampUtc { get { return File.GetCreationTime(typeof(BuildConstants).Assembly.Location); } }

    }
}
