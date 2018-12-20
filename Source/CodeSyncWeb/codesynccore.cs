using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ArborMemorialWeb.Components.Helpers;
using CodeSyncWeb.Components;
using CrownPeak.AccessAPI;
using CrownPeak.AccessApiHelper;
using Newtonsoft.Json;

namespace CodeSyncWeb
{
	public class codesynccore
	{
		private static string S_AppDataPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
		private static CacheHelper.WebFileCacheKeyProvider S_Provider;

		public codesynccore()
		{
			
			//AccessAPI = new CrownPeak.AccessApiHelper.ApiAccessor.SimpleApiAccessor();
		}
		//CrownPeak.AccessApiHelper.ApiAccessor.IApiAccessor AccessAPI;

		private static IList<Models.Instance> GetCachedInstances()
		{
			if(S_Provider == null)
				S_Provider = new CacheHelper.WebFileCacheKeyProvider("~/App_Data/accessapi-config.json");

			var list = CacheHelper.DeserializeCachedJSON<IList<Models.Instance>>(S_Provider);
			return list;
		}

		private static CrownPeak.AccessApiHelper.CmsApi CreateCmsApi(string clientLibrary)
		{
			
			var configInstance = GetCachedInstances().SingleOrDefault(_=>
					(_.Title??"").Equals(clientLibrary, StringComparison.OrdinalIgnoreCase)
				);
				
			if(configInstance == null)
			{
				throw new ApplicationException(string.Format("clientLibrary '{0}' configuration not available.", clientLibrary));
			}
			else
			{
				var AccessAPI = new CrownPeak.AccessApiHelper.ApiAccessor.SimpleApiAccessor();

				AccessAPI.Init(configInstance.Server ?? "cms.crownpeak.net", configInstance.Title, publicKey: Crypto.Decrypt(configInstance.ApiKey));
				AccessAPI.Login(Crypto.Decrypt(configInstance.Username), Crypto.Decrypt(configInstance.Password));

				return new CmsApi(AccessAPI);
			}
		}

		public static bool PutConfig(Models.Instance instanceConfig)
		{
			var instances = GetCachedInstances();

			var instancesDict = instances.ToDictionary(_ => _.Title, StringComparer.OrdinalIgnoreCase);

			instancesDict[instanceConfig.Title] = instanceConfig;

			string targetPath = S_Provider.GetFilePath();
			using(var sw = new StreamWriter(File.OpenWrite(targetPath)))
			{
				var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
				JsonSerializer.Create(settings).Serialize(sw, instances);
			}

			return true;
		}

		public static bool RefreshLibrary(string clientLibrary)
		{

			var CmsApi = CreateCmsApi(clientLibrary);

			int assetId;
			if(CmsApi.Asset.Exists("/System/Scripts/CodeSync", out assetId))
			{
				Dictionary<string, string> fields = new Dictionary<string, string>();
				fields["modified_since"] = "";

				WorklistAsset asset;
				if(CmsApi.Asset.Update(assetId, fields, out asset, runPostSave: true))
				{
					return true;
				}

			}

			return false;
		}

	}
}