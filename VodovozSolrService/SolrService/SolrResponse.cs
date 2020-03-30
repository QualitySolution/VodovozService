using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SolrImportService
{
	[DataContract]
	public class SolrResponse
	{
		[JsonProperty(PropertyName = "status")]
		public string Status { get; set; }
	}
}
