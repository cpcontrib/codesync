using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CodeSyncWeb.Models
{

	[JsonConverter(typeof(Converters.AccessApiConfig_Converter))]
	public class AccessApiConfig
	{
		//[JsonConverter(typeof(Converters.InstancesConverter))]
		public List<Instance> Instances { get; set; }
	}

	public class Instance
	{
		[JsonProperty("instance")]
		public string Title { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string ApiKey { get; set; }
		public string Server { get; set; }
	}
}

namespace CodeSyncWeb.Models.Converters
{ 
	public class AccessApiConfig_Converter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(AccessApiConfig);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var array = JArray.Load(reader);
			return array.Children<JObject>()
						.SelectMany(jo => jo.Properties())
						.Select(jp => new Instance {
							Title = jp.Name,
							Username = (string)jp.Value["Username"],
							Password = (string)jp.Value["Password"],
							ApiKey = (string)jp.Value["ApiKey"]
						})
						.ToList();
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}

	public class KeyedInstancesConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(List<Instance>);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var array = JArray.Load(reader);
			return array.Children<JObject>()
						.SelectMany(jo => jo.Properties())
						.Select(jp => new Instance {
							Title = jp.Name,
							Username = (string)jp.Value["Username"],
							Password = (string)jp.Value["Password"],
							ApiKey = (string)jp.Value["ApiKey"]
						})
						.ToList();
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}