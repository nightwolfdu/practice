﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Features = System.Collections.Generic.Dictionary<string, bool>;

namespace Rooletochka
{
	public class Analyzer
	{
		private readonly string _url;
		private string _content;
		private List<String> _pages;

		public string Url
		{
			get { return _url; }
		}

		public string Content
		{
			get { return _content; }
		}

		public Analyzer(string url, bool mainPage)
		{
			_url = url.ToLower();

			WebClient client = new WebClient();
			_content = client.DownloadString(url).ToLower();

			if(mainPage)
				_pages = GetPages(_content, _url);
			else _pages = new List<string>();
		}

		public Report Analyze()
		{
			Report report = new Report();
			report.MainUrl = Url;
			report.RobotsTxt = CheckRobotsTxt(Url);
			Thread.Sleep(500);
			report.Error404 = CheckError404(Url);

			report.mainPageResult = this.AnalyzePage(Url);

			Features result = new Features();
			foreach (string page in _pages)
			{
				try
				{
					Analyzer analyzer = new Analyzer(page, false);
					result = analyzer.AnalyzePage(analyzer.Url);
					report.AddCheckedPage(result, page);
					Thread.Sleep(500);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			return report;
		}

		private Features AnalyzePage(string url)
		{
			Features result = new Features();
			result["tagBody"] = CheckBodyTag(Content);
			result["tagHead"] = CheckHeadTag(Content);
			result["tagTitle"] = CheckTitleTags(Content);
			result["tagHtml"] = CheckHtmlTag(Content);
			result["inlineJs"] = CheckInlineJS(Content);
			result["inlineCss"] = CheckInlineCSS(Content);
			return result;
		}

		private List<String> GetPages(string content, string url)
		{
			url = url.TrimEnd('/');
			List<String> pages = new List<String>();
			string pattern = @"<a.*?href\s*=(['""][^""]*['""])";
			Regex rgx = new Regex(pattern);
			MatchCollection matches = rgx.Matches(content);
			foreach (Match match in matches)
			{
				string link = Regex.Replace(match.ToString(),
				                            @"<a.*?href\s*=(['""][^""]*['""])", @"$1",
				                            RegexOptions.IgnoreCase);
				link = link.Trim("\"".ToCharArray());
				if (link.Length > 2 && (link[0] == '/' || link.Contains(url)))
				{
					if (link[0] == '/' && link[1] == '/') continue;
					if (link[0] == '/') link = url + link;
					pages.Add(link);
				}
			}
			return pages;
		}

		#region Methods for checking common rules
		public bool CheckRobotsTxt(string url)
		{
			string str = "";
			if (url[url.Length - 1] == '/') str = url + "robots.txt";
			else str = url + "/robots.txt";

			bool redirect = false;
			int statusCode = CheckStatusCode(str, redirect);
			if (statusCode > 400 || statusCode == 0) return false;
			else return true;
		}

		public bool CheckError404(string url)
		{
			string str = "";
			if (url[url.Length - 1] == '/') str = url + "asdfjhkxjcv";
			else str = url + "/asdfjhkxjcv";

			bool redirect = true;
			int statusCode = CheckStatusCode(str, redirect);
			return (statusCode == 404);
		}

		private int CheckStatusCode(string url, bool redirect)
		{
			try
			{
				HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
				webRequest.AllowAutoRedirect = redirect;
				//Timeout of request (default timeout = 100s)
				webRequest.Timeout = 50000;
				HttpWebResponse response = (HttpWebResponse) webRequest.GetResponse();
				int wRespStatusCode;
				wRespStatusCode = (int) response.StatusCode;
				return wRespStatusCode;
			}
			catch (WebException we)
			{
				try
				{
					int wRespStatusCode = (int) ((HttpWebResponse) we.Response).StatusCode;
					return wRespStatusCode;
				}
				catch (NullReferenceException e)
				{
					Console.WriteLine(e.Message);
					return 0;
				}
			}
		}

		#endregion

		#region Methods for checking Html tags (true - OK, false - necessary corrections)
		private bool CheckInlineJS(string content)
		{
			string pattern = @"<script.*?>";
			Regex rgx = new Regex(pattern);
			MatchCollection matches = rgx.Matches(content);
			foreach (Match match in matches)
			{
				string value = match.ToString();
				if (value.Contains("src") && value.Contains(".js")) continue;
				return false;
			}
			return true;
		}

		private bool CheckInlineCSS(string content)
		{
			string pattern = @"style\s*=\s*"".*?""";
			Regex rgx = new Regex(pattern);
			MatchCollection matches = rgx.Matches(content);
			if (matches.Count == 0) return true;
			return false;
		}

		private bool CheckTitleTags(string content)
		{
			string titleTag = "<h";
			string closingTitleTag = "</h";
			for (int i = 0; i < 6; i++)
			{
				if (content.Contains(titleTag + i) && content.Contains(closingTitleTag + i))
					return true;
			}
			return false;
		}

		private bool CheckHtmlTag(string content)
		{
			string openHeadTag = "<html";
			string closingHeadTag = "</html>";
			if ((content.Contains(openHeadTag) && content.Contains(closingHeadTag)) == true)
				return true;
			return false;
		}

		private bool CheckBodyTag(string content)
		{
			string openBodyTag = "<body";
			string closingBodyTag = "</body>";
			if ((content.Contains(openBodyTag) && content.Contains(closingBodyTag)) == true)
				return true;
			return false;
		}

		private bool CheckHeadTag(string content)
		{
			string openHeadTag = "<head";
			string closingHeadTag = "</head>";
			if ((content.Contains(openHeadTag) && content.Contains(closingHeadTag)) == true)
				return true;
			return false;
		}

		#endregion
	}
}
