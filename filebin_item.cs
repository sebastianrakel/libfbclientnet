using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libfbclientnet
{
	public class filebin_item
	{
		private string _ID;
		public string ID
		{
			get { return _ID; }
			set { _ID = value; }
		}

		private string _Filename;
		public string Filename
		{
			get { return _Filename; }
			set { _Filename = value; }
		}

		private string _MIMEType;
		public string MIMEType
		{
			get { return _MIMEType; }
			set { _MIMEType = value; }
		}

		private string _Filesize;
		public string Filesize
		{
			get { return _Filesize; }
			set { _Filesize = value; }
		}

		private string _Hash;
		public string Hash
		{
			get { return _Hash; }
			set { _Hash = value; }
		}

		private string _date;
		public string Date
		{
			get { return global_functions.GetDateFromUnixTimestamp(Convert.ToDouble(_date)).ToString(); }
			set { _date = value; }
		}

		private string _fb_host;
		public string fb_host {
			get { return _fb_host; }
			set { _fb_host = value; }
		}

		public string Link
		{
			get { return this.fb_host + "/" + this.ID; }
		}


		public filebin_item()
		{
		}
	}
}


