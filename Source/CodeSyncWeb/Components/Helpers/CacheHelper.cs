using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ArborMemorialWeb.Components.Helpers
{

	public class CacheHelper
	{

		static System.Runtime.Caching.MemoryCache Cache = System.Runtime.Caching.MemoryCache.Default;

		public static T DeserializeCachedXML<T>(WebFileCacheKeyProvider cacheKeyProvider)
		{
			return DeserializeCachedXML<T>(cacheKeyProvider, () => {
				T result = default(T);
				string filePath = cacheKeyProvider.GetFilePath();
				using(var reader = new StreamReader(filePath))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(T));
					result = (T)serializer.Deserialize(reader);
				}

				return result;
			});
		}

		public static XDocument DeserializeCachedXML(WebFileCacheKeyProvider cacheKeyProvider)
		{
			return DeserializeCachedXML<XDocument>(cacheKeyProvider, () => {

				string filePath = cacheKeyProvider.GetFilePath();
				using(var reader = new StreamReader(filePath))
				{
					return XDocument.Load(reader);
				}

			});
		}

		public static T DeserializeCachedJSON<T>(WebFileCacheKeyProvider cacheKeyProvider)
		{
			return DeserializeCachedXML(cacheKeyProvider, () => {

				string filePath = cacheKeyProvider.GetFilePath();
				using(var sr = new StreamReader(filePath))
				{
					using(var reader = new JsonTextReader(sr))
					{
						var obj = JsonSerializer.CreateDefault().Deserialize<T>(reader);
						return obj;
					}
				}

			});
		}

		public class CacheKeyProvider
		{
			public string Value { get; set; }
			public Func<CacheItemPolicy> GetCacheItemPolicy { get; set; }

		}
		public class WebFileCacheKeyProvider : CacheKeyProvider
		{
			public WebFileCacheKeyProvider(Func<string, string> mappathFunc, string appRelativeFilePath)
			{
				this.Value = appRelativeFilePath;
				this.GetFilePathFunc = mappathFunc;
				this.GetCacheItemPolicy = new Func<CacheItemPolicy>(() => GetWebFilePolicy(mappathFunc, appRelativeFilePath));
			}
			public WebFileCacheKeyProvider(string appRelativeFilePath)
			{
				this.Value = appRelativeFilePath;
				this.GetFilePathFunc = System.Web.Hosting.HostingEnvironment.MapPath;
				this.GetCacheItemPolicy = new Func<CacheItemPolicy>(() => GetWebFilePolicy(this.GetFilePathFunc, this.Value));
			}

			static CacheItemPolicy GetWebFilePolicy(Func<string, string> mappathFunc, string appRelativeFilePath)
			{
				string filePath = mappathFunc(appRelativeFilePath);

				CacheItemPolicy p = new CacheItemPolicy();
				p.Priority = CacheItemPriority.Default;
				p.ChangeMonitors.Add(new HostFileChangeMonitor(new string[] { filePath }));
				//p.UpdateCallback = new CacheEntryUpdateCallback(x=>x.)

				return p;
			}

			public Func<string, string> GetFilePathFunc;
			public string GetFilePath()
			{
				return GetFilePathFunc(this.Value);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static T DeserializeCachedXML<T>(CacheKeyProvider cacheKeyProvider, Func<T> objectfactory)
		{
			T result = default(T);
			string cacheKey = cacheKeyProvider.Value;

			try
			{
				result = (T)Cache[cacheKey];
			}
			catch { }

			if(null == result)
			{
				try
				{
					result = objectfactory();

					CacheItemPolicy p = cacheKeyProvider.GetCacheItemPolicy();

					Cache.Add(cacheKey, result, p);
				}
				catch(Exception ex)
				{

				}
			}

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static T DeserializeCachedXML<T>(HttpContext context, string filePath)
		{
			return DeserializeCachedXML<T>(new CacheHelper.WebFileCacheKeyProvider(filePath));
		}

	}

}
