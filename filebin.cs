﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SeasideResearch.LibCurlNet;
using System.Threading;

namespace libfbclient.net
{
    class UploadProgressEventArgs {
        private double _UploadTotal;
        public double UploadTotal
        {
            get { return _UploadTotal; } 
            set { _UploadTotal = value; }
        }

        private double _UploadCurrent;
        public double UploadCurrent
        {
            get { return _UploadCurrent; }
            set { _UploadCurrent = value; }
        }
    }

    class filebin {
        public delegate void UploadProgressEventHandler(object sender, UploadProgressEventArgs e);

        public event UploadProgressEventHandler UploadProgress;

        private string _host;
        private string _useragent;

        public filebin(string host, string useragent)
        {
            _host = host;
            _useragent = useragent;
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

        public void UploadFile(object obj)
        {
            this.UploadFile(Convert.ToString(obj));
        }

        public void UploadFile(string filename)
        {
            //must happend first
            Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);

            Slist headerlist = new Slist();
            Easy easy = new Easy();
            MultiPartForm mf = new MultiPartForm();

            mf.AddSection(CURLformoption.CURLFORM_COPYNAME, "file", CURLformoption.CURLFORM_FILE, filename, CURLformoption.CURLFORM_END);

            easy.SetOpt(CURLoption.CURLOPT_DEBUGFUNCTION, new Easy.DebugFunction(OnDebug));
            easy.SetOpt(CURLoption.CURLOPT_VERBOSE, true);
            
            easy.SetOpt(CURLoption.CURLOPT_PROGRESSFUNCTION, new Easy.ProgressFunction(OnProgress));
            
            easy.SetOpt(CURLoption.CURLOPT_URL, this.Host);
            easy.SetOpt(CURLoption.CURLOPT_HTTPPOST, mf);

            headerlist.Append("Expect:");

            easy.SetOpt(CURLoption.CURLOPT_HTTPHEADER, headerlist);
            easy.SetOpt(CURLoption.CURLOPT_NETRC, CURLnetrcOption.CURL_NETRC_REQUIRED);

            easy.SetOpt(CURLoption.CURLOPT_USERAGENT, this.Useragent);
            easy.SetOpt(CURLoption.CURLOPT_SSL_VERIFYPEER, false);
            easy.SetOpt(CURLoption.CURLOPT_FOLLOWLOCATION, true);

            easy.Perform();
            easy.Cleanup();

            mf.Free();
            Curl.GlobalCleanup();
        }

        public void UploadFileAsync(string filename)
        {
            Thread pUploadThread = new System.Threading.Thread(new ParameterizedThreadStart(UploadFile));
                        
            pUploadThread.SetApartmentState(ApartmentState.STA);
            pUploadThread.Start(filename);
        }       

        public List<filebin_item> GetUploadHistory()
        {
            //must happend first
            Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);

            _CompleteHistoryJSONOutput = "";

            Easy easy = new Easy();
            Slist headerlist = new Slist();
            Easy.WriteFunction wf = default(Easy.WriteFunction);

            wf = new Easy.WriteFunction(OnWriteData);

            
            easy.SetOpt(CURLoption.CURLOPT_URL, this.Host + "/file/upload_history?json");

            headerlist.Append("Expect:");

            easy.SetOpt(CURLoption.CURLOPT_HTTPHEADER, headerlist);
            easy.SetOpt(CURLoption.CURLOPT_NETRC, CURLnetrcOption.CURL_NETRC_REQUIRED);

            easy.SetOpt(CURLoption.CURLOPT_BUFFERSIZE, 8 * 8192);
            easy.SetOpt(CURLoption.CURLOPT_WRITEFUNCTION, wf);

            easy.SetOpt(CURLoption.CURLOPT_USERAGENT, this.Useragent);
            easy.SetOpt(CURLoption.CURLOPT_SSL_VERIFYPEER, false);
            easy.SetOpt(CURLoption.CURLOPT_FOLLOWLOCATION, true);

            easy.Perform();
            easy.Cleanup();

            Curl.GlobalCleanup();


            if (_CompleteHistoryJSONOutput.Length > 0)
            {
                return new List<filebin_item>(Jayrock.Json.Conversion.JsonConvert.Import<filebin_item[]>(_CompleteHistoryJSONOutput));
            }
            else
            {
                return null;
            }
        }
        
        static string _CompleteHistoryJSONOutput;
        private static Int32 OnWriteData(byte[] buf, Int32 size, Int32 nmemb, object extraData)
        {
            _CompleteHistoryJSONOutput += System.Text.Encoding.UTF8.GetString(buf);
            return size * nmemb;
        }

        private void OnDebug(CURLINFOTYPE infoType, string msg, object extraData)
        {
            //only dump received data
            if ((infoType == CURLINFOTYPE.CURLINFO_DATA_IN))
            {
                if (msg.StartsWith("http"))
                    SetClipboardTextDispatcher(msg);
                Log_WriteLine(msg);
            }
        }

        private Int32 OnProgress(object extraData, double dlTotal, double dlNow, double ulTotal, double ulNow)
        {
            UploadProgressEventArgs e = new UploadProgressEventArgs();

            e.UploadTotal = ulTotal;
            e.UploadCurrent = ulNow;

            OnUploadProgress(e);
            
            return 0;
        }

        protected virtual void OnUploadProgress(UploadProgressEventArgs e)
        {
            if (UploadProgress != null)
            {
                UploadProgress(this, e);
            }
        }

        
    }

}