using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DeliveryTermsAPI.Models;

namespace DeliveryTermsAPI.Controllers
{
    [RoutePrefix("api/v1")]
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(decimal latitude, decimal longitude)
        {
            var res = new DeliveryTerms();

            return res.GetRulesByDistrict(latitude, longitude);
        }
 
    }
}
