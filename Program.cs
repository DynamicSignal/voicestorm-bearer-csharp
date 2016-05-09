using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Script.Serialization;

namespace voicestorm_csharp_bearer
{
	class Program
	{
		static void Main(string[] args)
		{
			// TODO: FILL THESE IN!
			const string community = "";
			const string accessToken = "";
			const string tokenSecret = "";

			// sanity
			if (string.IsNullOrEmpty(community) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(tokenSecret))
			{
				Console.WriteLine("You must enter a VoiceStorm community, access token, and secret!");
				return;
			}

			// make a simple request to get the stream definitions, this does not require authentication
			VoiceStormSimpleApiRequest streams = new VoiceStormSimpleApiRequest(community, "/v1/streams/definitions", null);
			if (streams.Result != null)
			{
				Console.WriteLine("Streams: {0}", streams.Result);  // dump the raw streams json
				Console.WriteLine();
			}

			// now encode the server credentials and request a bearer token
			VoiceStormServerCredentials creds = new VoiceStormServerCredentials(accessToken, tokenSecret);
			VoiceStormBearerRequest bearerRequest = new VoiceStormBearerRequest(community, creds);
			if (bearerRequest.BearerToken != null)
			{
				Console.WriteLine("Bearer token: {0}", bearerRequest.BearerToken.access_token); // dump the access token, DO NOT SHARE THIS!
				Console.WriteLine();

				// now that we have a bearer token, we can make a request that requires authentication
				VoiceStormSimpleApiRequest groups = new VoiceStormSimpleApiRequest(community, "/v1/groups", bearerRequest.BearerToken);
				if (groups.Result != null)
				{
					Console.WriteLine("Groups: {0}", groups.Result);    // dump the raw groups json
					Console.WriteLine();
				}
			}
		}
	}

	/// <summary>
	/// encodes the VoiceStorm basic credentials
	/// see https://dev.voicestorm.com/start#authentication_1
	/// </summary>
	public class VoiceStormServerCredentials
	{
		public string Encoded { get; private set; }

		public VoiceStormServerCredentials(string token, string secret)
		{
			// first url encode
			token = WebUtility.UrlEncode(token);
			secret = WebUtility.UrlEncode(secret);

			// concat with separator
			string credentials = token + ':' + secret;

			// base64 encode
			byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);
			Encoded = Convert.ToBase64String(credentialsBytes);
		}
	}

	/// <summary>
	/// represents the returned results from a VoiceStormBearerRequest
	/// </summary>
	public class VoiceStormBearerResult
	{
		public string token_type { get; set; }
		public string access_token { get; set; }

		public static VoiceStormBearerResult FromJson(string json)
		{
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Deserialize<VoiceStormBearerResult>(json);
		}
	}

	/// <summary>
	/// request a bearer token from the server credentials
	/// see https://dev.voicestorm.com/start#authentication_2
	/// </summary>
	public class VoiceStormBearerRequest
	{
		public VoiceStormBearerResult BearerToken { get; private set; }
		public VoiceStormBearerRequest(string host, VoiceStormServerCredentials creds)
		{
			// create a web client
			using (HttpClient httpClient = new HttpClient())
			{
				// set auth header and post data
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds.Encoded);
				using (StringContent postData = new StringContent("grant_type=client_credentials"))
				{
					postData.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
					try
					{
						// POST a request for a bearer token
						var requestUri = new UriBuilder(Uri.UriSchemeHttps, host, -1, "/v1/oauth2/token").Uri;
						HttpResponseMessage response = httpClient.PostAsync(requestUri, postData).Result;
						BearerToken = VoiceStormBearerResult.FromJson(response.Content.ReadAsStringAsync().Result);
					}
					catch (Exception ex)
					{
						while (ex.InnerException != null) ex = ex.InnerException;
						Console.WriteLine("There was an error getting the bearer token: {0}", ex.Message);
					}
				}
			}
		}
	}

	/// <summary>
	/// make a simple API request, optionally with a bearer token authentication
	/// see https://dev.voicestorm.com/start#authentication_3
	/// </summary>
	public class VoiceStormSimpleApiRequest
	{
		public string Result { get; private set; }
		public VoiceStormSimpleApiRequest(string host, string path, VoiceStormBearerResult creds)
		{
			// create a web client
			using (HttpClient httpClient = new HttpClient())
			{
				// set auth header if we have credentials
				if (creds != null)
				{
					httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creds.access_token);
				}

				try
				{
					// make a simple GET request
					var requestUri = new UriBuilder(Uri.UriSchemeHttps, host, -1, path).Uri;
					Result = httpClient.GetStringAsync(requestUri).Result;
				}
				catch (Exception ex)
				{
					while (ex.InnerException != null) ex = ex.InnerException;
					Console.WriteLine("There was an error making a request for {0}: {1}", path, ex.Message);
				}
			}
		}
	}
}
