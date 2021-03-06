﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{
    public class GeoPatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            
            if (requestInfo.Visitor != null)
            {                
                var whois = new WhoIsInformation();
                if (requestInfo.Visitor.Variables.SetIfPresent("Country", v => whois.Country = v)
                    | requestInfo.Visitor.Variables.SetIfPresent("Region", v => whois.Region = v)
                    | requestInfo.Visitor.Variables.SetIfPresent("City", v => whois.City = v))
                {
                    session.Interaction.SetGeoData(whois);
                }
            }
        }
    }
}
