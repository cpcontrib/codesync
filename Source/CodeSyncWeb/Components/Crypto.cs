using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace CodeSyncWeb.Components
{

	public class Crypto
	{
		public static string Decrypt(string value)
		{
			if(value.StartsWith("secret:") == false)
				return value;
			else
			{
				return Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(7)));
			}
		}

		public static string Encrypt(string value)
		{
			var encryptedBytes = Encoding.UTF8.GetBytes(value);

			return "secret:" + Convert.ToBase64String(encryptedBytes);
		}
	}
}