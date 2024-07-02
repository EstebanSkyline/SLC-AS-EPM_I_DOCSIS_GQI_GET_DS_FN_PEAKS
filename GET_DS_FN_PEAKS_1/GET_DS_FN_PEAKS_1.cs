/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

25/06/2024	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace GET_DS_FN_PEAKS_1
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	[GQIMetaData(Name = "Get DS FN Peaks")]
	public class Script : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{
		private readonly GQIStringArgument frontEndElementArg = new GQIStringArgument("FE Element")
		{
			IsRequired = true,
		};

		private readonly GQIDateTimeArgument initialTimeArg = new GQIDateTimeArgument("Initial Time")
		{
			IsRequired = false,
		};

		private readonly GQIDateTimeArgument finalTimeArg = new GQIDateTimeArgument("Final Time")
		{
			IsRequired = false,
		};

		private GQIDMS _dms;
		private string frontEndElement = String.Empty;
		private List<GQIRow> listGqiRows = new List<GQIRow> { };

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
			new GQIStringColumn("Fiber Node"),
			new GQIDoubleColumn("SCQAM Peak"),
			new GQIDoubleColumn("OFDM Peak"),
			};
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[]
			{
				frontEndElementArg,
				initialTimeArg,
				finalTimeArg,
			};
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			listGqiRows.Clear();
			var feEid = args.GetArgumentValue(frontEndElementArg);
			var initialTime = args.GetArgumentValue(initialTimeArg);
			var endTime = args.GetArgumentValue(finalTimeArg);

			var dmaID = feEid.Split('/').First();
			int.TryParse(dmaID, out var hostId);
			var response = GetDsFNPeaks(hostId, initialTime, endTime);
			Dictionary<string, FiberNodeRow> fiberNodeRow = JsonConvert.DeserializeObject<Dictionary<string, FiberNodeRow>>(response["Response"]);
			CreateRows(fiberNodeRow);
			return new OnArgumentsProcessedOutputArgs();
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			return new OnInitOutputArgs();
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return new GQIPage(listGqiRows.ToArray())
			{
				HasNextPage = false,
			};
		}

		public string ParseDoubleValue(double doubleValue, string unit)
		{
			if (doubleValue.Equals(-1))
			{
				return "N/A";
			}

			return Math.Round(doubleValue, 2).ToString("F2") + " " + unit;
		}

		private Dictionary<string, string> GetDsFNPeaks(int dmaId, DateTime initTime, DateTime endTime)
		{
			Skyline.DataMiner.Net.Messages.ExecuteScriptMessage scriptMessage = new ExecuteScriptMessage
			{
				DataMinerID = dmaId,
				ScriptName = "GetDataAggregatorFiles",
				Options = new SA(new[] { $"DEFER:{bool.FalseString}", $"PARAMETER:1:{Convert.ToString(initTime.ToUniversalTime())}", $"PARAMETER:2:{Convert.ToString(endTime.ToUniversalTime())}", $"PARAMETER:3:true" }),
			};
			var response = _dms.SendMessage(scriptMessage) as ExecuteScriptResponseMessage;
			var scriptResult = response?.ScriptOutput;
			return scriptResult;
		}

		private void CreateRows(Dictionary<string, FiberNodeRow> response)
		{
			foreach (var row in response)
			{
				List<GQICell> listGqiCells = new List<GQICell>
				{
					new GQICell
					{
						Value = row.Value.FnName,
					},
					new GQICell
					{
						Value = row.Value.DsFnUtilization,
						DisplayValue = ParseDoubleValue(row.Value.DsFnUtilization, "%"),
					},
					new GQICell
					{
						Value = row.Value.OfdmFnUtilization,
						DisplayValue = ParseDoubleValue(row.Value.OfdmFnUtilization, "%"),
					},
				};
				var gqiRow = new GQIRow(listGqiCells.ToArray());
				listGqiRows.Add(gqiRow);
			}
		}
	}
}
