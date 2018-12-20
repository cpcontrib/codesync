using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace CodeSyncWeb.ApiControllers
{
	[RoutePrefix("api/v1")]
	public class ConfigController : ApiController
	{
		//PUT config
		[Route("config")]
		[HttpPut]
		public IHttpActionResult PutConfig(Models.Instance instanceConfig)
		{
			if(ModelState.IsValid==false)
			{
				return BadRequest(ModelState);
			}

			codesynccore.PutConfig(instanceConfig);

			return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Accepted));
		}

		// GET api/<controller>/5
		public string Get(int id)
		{
			return "value";
		}

		// POST api/<controller>
		public void Post([FromBody]string value)
		{
		}

		// PUT api/<controller>/5
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/<controller>/5
		public void Delete(int id)
		{
		}
	}
}