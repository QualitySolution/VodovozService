using System;
using System.Net;
using Nini.Config;
using NLog;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

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

		private IEnumerable<string> automaticImportsEntities = new string[] {
			"counterparty",
			"delivery_points"
		};

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

			RunImportTask();
			RunPeriodicImportTask();
		}

		public void Dispose()
		{
			cts?.Cancel();
		}

		#region Группировщик запросов

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

		private void RunImportTask()
		{
			Task workTask = Task.Run(() => {
				string nextKey = requestPool.Keys.FirstOrDefault();
				if(string.IsNullOrWhiteSpace(nextKey)) {
					return;
				}
				if(requestPool.TryRemove(nextKey, out byte value)) {
					try {
						if(cts.IsCancellationRequested) {
							return;
						}
						RunQuery(nextKey);
					}
					catch(Exception ex) {
						logger.Error(ex);
					}
				}
			}, cts.Token);

			workTask.ContinueWith((task) => NextImport(), cts.Token);
		}

		private void NextImport()
		{
			Task.Delay(workerDelay, cts.Token).ContinueWith((task) => RunImportTask(), cts.Token);
		}

		#endregion Группировщик запросов


		#region Периодический запуск

		private void RunPeriodicImportTask()
		{
			Task periodicImport = Task.Run(() => {
				foreach(var entityName in automaticImportsEntities) {
					AddToWorkQueue(entityName);
				}
			}, cts.Token);
			periodicImport.ContinueWith((task) => NextPeriodicImport(), cts.Token);
		}

		private void NextPeriodicImport()
		{
			Task.Delay(30000, cts.Token).ContinueWith((task) => RunPeriodicImportTask(), cts.Token);
		}

		#endregion Периодический запуск
	}
}
