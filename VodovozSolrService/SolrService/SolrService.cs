using System;
using System.Net;
using Nini.Config;
using NLog;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace SolrImportService
{
	public class SolrService : ISolrService, IDisposable
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private string solrServerAddress = "";
		private string solrServerPort = "";
		private string solrDb = "";
		private int workerDelay = 500;

		private ConcurrentDictionary<string, byte> requestPool = new ConcurrentDictionary<string, byte>();
		private CancellationTokenSource cts = new CancellationTokenSource();

		public SolrService(IConfig solrConfig)
		{
			if(solrConfig == null) {
				throw new ArgumentNullException(nameof(solrConfig));
			}

			solrServerAddress = solrConfig.GetString("solr_server_address");
			solrServerPort = solrConfig.GetString("solr_server_port", "8983");
			solrDb = solrConfig.GetString("solr_db");
			if(!int.TryParse(solrConfig.GetString("worker_delay"), out workerDelay)) {
				workerDelay = 500;
			}

			RunNext();
		}

		private string GetDeltaImportQuery(string solrEntityName)
		{
			if(string.IsNullOrWhiteSpace(solrEntityName)) {
				throw new ArgumentNullException(nameof(solrEntityName));
			}

			return $"http://{solrServerAddress}:{solrServerPort}/solr/{solrDb}/dataimport?command=delta-import&entity={solrEntityName}&clean=false&optimize=false";
		}

		public string RunDeltaImport(string solrEntityName)
		{
			AddToWorkQueue(solrEntityName);
			return "Ok";
		}

		private void AddToWorkQueue(string solrEntityName)
		{
			if(requestPool.ContainsKey(solrEntityName)) {
				return;
			}

			requestPool.TryAdd(solrEntityName, 0);
		}

		private void RunQuery(string solrEntityName)
		{
			using(var webClient = new WebClient()) {
				var response = webClient.DownloadString(GetDeltaImportQuery(solrEntityName));
				SolrResponse responseContent = JsonConvert.DeserializeObject<SolrResponse>(response);
				logger.Info($"Request import for: {solrEntityName}. Response status: {responseContent.Status}");
				if(responseContent.Status != "idle") {
					AddToWorkQueue(solrEntityName);
				}
			}
		}

		private void RunNext()
		{
			Task workTask = Task.Run(() => {
				string nextKey = requestPool.Keys.FirstOrDefault();
				if(string.IsNullOrWhiteSpace(nextKey)) {
					return;
				}
				if(requestPool.TryRemove(nextKey, out byte value)) {
					try {
						RunQuery(nextKey);
					}
					catch(Exception ex) {
						logger.Error(ex);
					}
				}
			}, cts.Token);

			workTask.ContinueWith((task) => NextIteration(), cts.Token);
		}

		private void NextIteration()
		{
			Task.Delay(workerDelay, cts.Token).ContinueWith((task) => RunNext(), cts.Token);
		}

		public void Dispose()
		{
			cts?.Cancel();
		}
	}
}
