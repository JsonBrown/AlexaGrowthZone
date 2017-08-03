using AlexaGrowthZone.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AlexaGrowthZone.Controllers
{
    public class AlexaController : ApiController
    {
        [Route("alexa/sample-session")]
        [HttpPost]
        public HttpResponseMessage SampleSession()
        {
            var speechlet = new GrowthzoneSpeechlet();
            return speechlet.GetResponse(Request);
        }
    }
}
