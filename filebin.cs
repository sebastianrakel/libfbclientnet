using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Jayrock;
using Jayrock.Json;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using Jayrock.Json.Conversion;

namespace libfbclientnet
{
	public class UploadProgressEventArgs {
		private int _UploadTotal;
		public int UploadTotal
		{
			get { return _UploadTotal; } 
			set { _UploadTotal = value; }
		}

		private int _UploadCurrent;
		public int UploadCurrent
		{
			get { return _UploadCurrent; }
			set { _UploadCurrent = value; }
		}

		public UploadProgressEventArgs(int current, int total) {
			_UploadCurrent = current;
			_UploadTotal = total;
		}
	}

	public class UploadFinishedEventArgs {
		private UploadResult _uploadResult;
		public UploadResult Result {
			get { return _uploadResult; }
		}

		public UploadFinishedEventArgs(UploadResult result) {
			_uploadResult = result;
		}
	}

	public class UploadResult {
		private string _URL;
		public string URL {
			get { return _URL; }
		}

		private UploadStatus _status;
		public UploadStatus Status { 
			get { return _status; }
		}

		public enum UploadStatus {
			Error = 0,
			Success = 1
		}

		public UploadResult(string url, UploadStatus status) {
			_URL = url;
			_status = status;
		}
	}

	public class filebin {
		public delegate void UploadProgressEventHandler (object sender, UploadProgressEventArgs e);
		public delegate void UploadFinishedEventHandler (object sender, UploadFinishedEventArgs e);

		public event UploadProgressEventHandler UploadProgress;
		public event UploadFinishedEventHandler UploadFinished;

		private string _host;
		private string _useragent;
		private string _apikey;

		private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		public filebin(string host, string useragent)
		{
			if (!host.EndsWith ("/")) {
				host += "/";
			}

			_host = host;
			_useragent = useragent;

			ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);	
		}

		public string Host
		{
			get { return _host; }
		}

		public string Useragent
		{
			get { return _useragent; }
			set { _useragent = value;  }
		}

		public string APIKey {
			get { return _apikey; }
			set { _apikey = value; }
		}

		public UploadResult UploadFile(string filename)
		{
			string boundary = "----------------------------" +	DateTime.Now.Ticks.ToString("x");

			HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(this.Host + "file/do_upload");
			httpReq.ContentType = "multipart/form-data; boundary=" + boundary;
			httpReq.Method = "POST";
			httpReq.KeepAlive = true;
			httpReq.Credentials = System.Net.CredentialCache.DefaultCredentials;
			httpReq.Accept = "application/json";

			Stream memStream = new System.IO.MemoryStream();

			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			string formdataTemplate = "\r\n--" + boundary +	"\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

			NameValueCollection nvc = new NameValueCollection();

			nvc.Add ("apikey", this.APIKey);

			foreach (string key in nvc.Keys)
			{
				string formitem = string.Format(formdataTemplate, key, nvc[key]);
				byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
				memStream.Write(formitembytes, 0, formitembytes.Length);
			}

			memStream.Write(boundarybytes, 0, boundarybytes.Length);

			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

			string[] files = new string[] { filename };

			for (int i = 0; i < files.Length; i++)
			{

				//string header = string.Format(headerTemplate, "file" + i, files[i]);
				string header = string.Format(headerTemplate, "file", files[i]);

				byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

				memStream.Write(headerbytes, 0, headerbytes.Length);


				FileStream fileStream = new FileStream(files[i], FileMode.Open,
				                                       FileAccess.Read);
				byte[] buffer = new byte[1024];

				int bytesRead = 0;

				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					memStream.Write(buffer, 0, bytesRead);

				}

				memStream.Write(boundarybytes, 0, boundarybytes.Length);

				fileStream.Close();
			}

			httpReq.ContentLength = memStream.Length;

			Stream requestStream = httpReq.GetRequestStream();

			memStream.Position = 0;

			byte[] tempBuffer = new byte[memStream.Length];

			memStream.Read(tempBuffer, 0, tempBuffer.Length);

			int numBytesToRead = (int) memStream.Length;
			int numBytesRead = 0;
			int block;

			while(numBytesToRead > 0) {

				if (numBytesToRead < 1024) {
					block = numBytesToRead;
				} else {
					block = 1024;
				}

				requestStream.Write(tempBuffer, numBytesRead, block);

				numBytesRead += 1024;
				numBytesToRead -= 1024;

				OnUploadProgress (new UploadProgressEventArgs (numBytesRead, tempBuffer.Length));
			}

			memStream.Close();
			requestStream.Close();

			WebResponse webRes = httpReq.GetResponse();

			string jsonOutput;

			using (Stream stream2 = webRes.GetResponseStream()) {
				using (StreamReader reader2 = new StreamReader(stream2)) {
					jsonOutput = reader2.ReadToEnd ();
				}
			}

			webRes.Close();
			httpReq = null;
			webRes = null;


			JsonObject jsonDownloadResult = (JsonObject) JsonConvert.Import(jsonOutput);

			UploadResult.UploadStatus status;

			switch (jsonDownloadResult ["status"].ToString().ToUpperInvariant()) {
				case "SUCCESS":
					status = UploadResult.UploadStatus.Success;
					break;
				default:
					status = UploadResult.UploadStatus.Error;
					break;
			}

			UploadResult result = new UploadResult (jsonDownloadResult["data"].ToString(), status);

			return result;
		}

		private void UploadFile(object obj)
		{
			UploadResult result = this.UploadFile(Convert.ToString(obj));
			OnUploadFinished (new UploadFinishedEventArgs (result));
		}


		public void UploadFileAsync(string filename)
		{
			Thread pUploadThread = new System.Threading.Thread(new ParameterizedThreadStart(UploadFile));

			pUploadThread.SetApartmentState(ApartmentState.STA);
			pUploadThread.Start(filename);
		}       

		public List<filebin_item> GetUploadHistory()
		{
			List<filebin_item> historyItems = null;

			HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(this.Host + "file/upload_history?json");
			HttpWebResponse response;
			try {
				ASCIIEncoding encoding = new ASCIIEncoding();
				string postData = "apikey=" + this.APIKey;
				byte[] data = encoding.GetBytes(postData);

				httpWReq.Method = "POST";
				httpWReq.ContentType = "application/x-www-form-urlencoded";
				httpWReq.ContentLength = data.Length;
				httpWReq.UserAgent = this.Useragent;

				using (Stream stream = httpWReq.GetRequestStream())
				{
					stream.Write(data,0,data.Length);
				}

				response = (HttpWebResponse)httpWReq.GetResponse();

				string responseString;
				using (StreamReader sr = new StreamReader(response.GetResponseStream())) {
					responseString = sr.ReadToEnd();
				}

				JsonObject jsonDownloadResult = (JsonObject) JsonConvert.Import(responseString);

				historyItems = new List<filebin_item>(Jayrock.Json.Conversion.JsonConvert.Import<filebin_item[]>(jsonDownloadResult["data"]));
				foreach (filebin_item historyItem in historyItems) {
					historyItem.fb_host = this.Host;
				}
			} catch (Exception ex) {
				httpWReq.Abort ();
				throw ex;
			}

			return historyItems;
		}

		protected virtual void OnUploadProgress(UploadProgressEventArgs e)
		{
			if (UploadProgress != null) {
				UploadProgress(this, e);
			}
		}

		protected virtual void OnUploadFinished(UploadFinishedEventArgs e) {
			if (UploadFinished != null) {
				UploadFinished (this, e);
			}
		}        
	}

}
