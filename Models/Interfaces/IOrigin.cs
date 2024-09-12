using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.Interfaces
{
    public interface IOrigin: ILongIdentifiable
    {

        /// <summary>
        /// Organization,User,Application,Group
        /// </summary>
        OriginType GetOriginType();
        /// <summary>
        /// Radial Review,Clay Upton,RadialWorks Inc, 
        /// </summary>
        String GetSpecificNameForOrigin();

        List<IOrigin> OwnsOrigins();

        List<IOrigin> OwnedByOrigins();
    }
}
