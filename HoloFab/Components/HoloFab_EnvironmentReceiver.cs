#define DEBUG
// #undef DEBUG

using System;
using System.Collections.Generic;

using Rhino.Geometry;

using Grasshopper.Kernel;
using Newtonsoft.Json;

using HoloFab.CustomData;

namespace HoloFab {
	// A HoloFab class to receive Environment Mesh from AR device.
	public class EnvironmentReceiver : GH_Component {
		//////////////////////////////////////////////////////////////////////////
		// - currents
		private static string currentData;
		private List<Mesh> meshes = new List<Mesh>();
		// - history
		private static string lastData;
		//private static bool flagProcessed = false;
		// - settings
		private static int expireDelay = 40;
		// - debugging
		#if DEBUG
		private string sourceName = "Environment Mesh Receiving Component";
		public static List<string> debugMessages = new List<string>();
		#endif
        
		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
		protected override void SolveInstance(IGH_DataAccess DA) {
			// Get inputs.
			Connection connect = null;
			if (!DA.GetData(0, ref connect)) return;
			//////////////////////////////////////////////////////
			// Process data.
			if (connect.status) {
				// If connection open start acting.
				// Prepare to receive UI data.
				try {
					if (connect.tcpReceiver.dataMessages.Count > 0) {
						EnvironmentReceiver.currentData = connect.tcpReceiver.dataMessages.Peek();
						EnvironmentReceiver.currentData = EncodeUtilities.StripSplitter(EnvironmentReceiver.currentData);
						if (EnvironmentReceiver.lastData != EnvironmentReceiver.currentData) {
							EnvironmentReceiver.lastData = EnvironmentReceiver.currentData;
							UniversalDebug("New Message without Message Splitter removed: " + EnvironmentReceiver.currentData);
							//this.meshes = new List<Mesh>();
							string[] messageComponents = EnvironmentReceiver.currentData.Split(new string[] {EncodeUtilities.headerSplitter}, StringSplitOptions.RemoveEmptyEntries);
							if (messageComponents.Length > 1) {
								for (int i=0; i<messageComponents.Length; i += 2) { 
									string header = messageComponents[i], content = messageComponents[i+1];
									UniversalDebug("Header: " + header + ", content: " + content);
									if (header == "ENVIRONMENT") {
										// If any new data received - process it.
										MeshData data = JsonConvert.DeserializeObject<MeshData>(content);
										this.meshes.Add(MeshUtilities.DecodeMesh(data));
									}
									// else
									//	UniversalDebug("Header Not Recognized!", GH_RuntimeMessageLevel.Warning);
								}

								UniversalDebug("Data Received!");
								connect.tcpReceiver.dataMessages.Dequeue(); // Actually remove from the queue since it has been processed.
							} else
								UniversalDebug("Data not Received!", GH_RuntimeMessageLevel.Warning);
						} else
							UniversalDebug("Improper Message!", GH_RuntimeMessageLevel.Warning);
					} else
						UniversalDebug("No data received.");
				} catch {
					UniversalDebug("Error Processing Data.", GH_RuntimeMessageLevel.Error);
				}
			} else {
				// If connection disabled - reset memoty.
				EnvironmentReceiver.lastData = string.Empty;
				UniversalDebug("Set 'Send' on true in HoloFab 'HoloConnect'", GH_RuntimeMessageLevel.Warning);
			}
			//////////////////////////////////////////////////////
			// Output.
			DA.SetDataList(0, this.meshes);
			#if DEBUG
			DA.SetDataList(1, connect.tcpReceiver.debugMessages);
			//DA.SetData(1, EnvironmentReceiver.debugMessages[EnvironmentReceiver.debugMessages.Count-1]);
			#endif
            
			//// Expire Solution.
			//if (connect.status) {
			//	GH_Document document = this.OnPingDocument();
			//	if (document != null)
			//		document.ScheduleSolution(EnvironmentReceiver.expireDelay, ScheduleCallback);
			//}
		}
		//private void ScheduleCallback(GH_Document document) {
		//	ExpireSolution(false);
		//}
		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Initializes a new instance of the EnvironmentReceiver class.
		/// Each implementation of GH_Component must provide a public
		/// constructor without any arguments.
		/// Category represents the Tab in which the component will appear,
		/// Subcategory the panel. If you use non-existing tab or panel names,
		/// new tabs/panels will automatically be created.
		/// </summary>
		public EnvironmentReceiver()
			: base("Environment Meshes Receiver", "Environment",
			       "Receieves Incoming data of the Environment Meshes from AR device",
			       "HoloFab", "EnvironmentMeshes") {}
		/// <summary>
		/// Provides an Icon for every component that will be visible in the User Interface.
		/// Icons need to be 24x24 pixels.
		/// </summary>
		protected override System.Drawing.Bitmap Icon {
			get { return Properties.Resources.HoloFab_Environment; }
		}
		/// <summary>
		/// Each component must have a unique Guid to identify it.
		/// It is vital this Guid doesn't change otherwise old ghx files
		/// that use the old ID will partially fail during loading.
		/// </summary>
		public override Guid ComponentGuid {
			get { return new Guid("37560f5d-8d94-45d0-825a-b779a38f4772"); }
		}
		/// <summary>
		/// Registers all the input parameters for this component.
		/// </summary>
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			pManager.AddGenericParameter("Connect", "Cn", "Connection object from Holofab 'Create Connection' component.", GH_ParamAccess.item);
		}
		/// <summary>
		/// Registers all the output parameters for this component.
		/// </summary>
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			pManager.AddMeshParameter("Environment Meshes", "EM", "Environment Meshes from AR device.", GH_ParamAccess.list);
			#if DEBUG
			pManager.AddTextParameter("Debug", "D", "Debug console.", GH_ParamAccess.list);
			//pManager.AddTextParameter("Debug", "D", "Debug console.", GH_ParamAccess.item);
			#endif
		}
		////////////////////////////////////////////////////////////////////////
		// Common way to Communicate messages.
		private void UniversalDebug(string message, GH_RuntimeMessageLevel messageType = GH_RuntimeMessageLevel.Remark) {
			#if DEBUG
			DebugUtilities.UniversalDebug(this.sourceName, message, ref EnvironmentReceiver.debugMessages);
			#endif
			this.AddRuntimeMessage(messageType, message);
		}
	}
}