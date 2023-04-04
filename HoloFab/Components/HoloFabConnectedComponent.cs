// #define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;

using System.Windows.Forms;
using System.Drawing;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Newtonsoft.Json;

using HoloFab.CustomData;

namespace HoloFab {
	// A template HoloFab class for a component.
	public abstract class HoloFabConnectedComponent : GH_Component {
		////////////////////////////////////////////////////////////////////////////
		//// NECESSARY COMPONENT VARIABLES
		//protected string componentName = "name",
		//	componentNickname = "nickname",
		//	componentDescription = "desctiption",
		//	componentCategory = "category",
		//	componentSubCategory = "subCategory",
		protected abstract string componentGUID { get; }
		protected abstract System.Drawing.Bitmap componentIcon { get; }
		//////////////////////////////////////////////////////////////////////////
		// - history
		protected string lastMessage = string.Empty;
		// - settings
		//// If messages in queues - expire solution after this time.
		//private static int expireDelay = 40;
		// - debugging
		#if DEBUG
		protected abstract string sourceName { get; }
		public List<string> debugMessages = new List<string>();
		#endif
		// TODO: doesn't seem to be saved?
		protected SourceType communicationProtocolType = SourceType.UDP;
		protected abstract SourceCommunicationType communicationType { get; }
		protected abstract bool allowProtocolChanging{ get; }
		protected int communicatorID = -1;
		protected HoloConnection connect = null;
		protected bool wasConnected = false;
		protected bool flagForce = false; // force messages even if they haven't changed
        protected override void BeforeSolveInstance() {
			base.BeforeSolveInstance();
			if (this.wasConnected) {
				if (this.connect != null) {
					if (!DependsOn(this.connect.owner)) {
						this.wasConnected = false;
						this.communicatorID = -1;
					}
				}
				else {
					this.wasConnected = false;
					this.communicatorID = -1;
				}
			}
		}

		public abstract bool GetInputs(IGH_DataAccess DA);
		public abstract void Solve();
		public abstract void Reset();
		public abstract void SetOutputs(IGH_DataAccess DA);
		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
		protected override void SolveInstance(IGH_DataAccess DA) {
			// Get inputs.
			if (!TryGetHoloConnect(DA)) return;
			if (!GetInputs(DA)) return;
			////////////////////////////////////////////////////////////////////

			// If connection open start acting.
			if (this.connect.status) {
				Solve();
			} else {
				Reset();
				this.wasConnected = false;
				this.communicatorID = -1;
				this.lastMessage = string.Empty;
				UniversalDebug("Set 'Send' on true in HoloFab 'HoloConnect'", GH_RuntimeMessageLevel.Warning);
			}

			// Output.
			#if DEBUG
			DA.SetData(0, this.debugMessages[this.debugMessages.Count-1]);
			#endif
			SetOutputs(DA);

            //// Expire Solution.
            //if ((connect.status) && (connect.MessagesAvailable)) {
            //    GH_Document document = this.OnPingDocument();
            //    if (document != null)
            //        document.ScheduleSolution(HoloFabConnectedComponent.expireDelay, ScheduleCallback);
            //}
        }
		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Initializes a new instance of the MeshStreaming class.
		/// Each implementation of GH_Component must provide a public
		/// constructor without any arguments.
		/// Category represents the Tab in which the component will appear,
		/// Subcategory the panel. If you use non-existing tab or panel names,
		/// new tabs/panels will automatically be created.
		/// </summary>
		public HoloFabConnectedComponent(string componentName, string componentNickname, string componentDescription,
				  string componentCategory, string componentSubCategory)
			: base(componentName, componentNickname, componentDescription,
				  componentCategory, componentSubCategory) {
			UpdateMessage(); 
		}
		/// <summary>
		/// Provides an Icon for every component that will be visible in the User Interface.
		/// Icons need to be 24x24 pixels.
		/// </summary>
		protected override System.Drawing.Bitmap Icon
		{
			get { return this.componentIcon; }
		}
		/// <summary>
		/// Each component must have a unique Guid to identify it.
		/// It is vital this Guid doesn't change otherwise old ghx files
		/// that use the old ID will partially fail during loading.
		/// </summary>
		public override Guid ComponentGuid {
			get { return new Guid(componentGUID); }
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
			#if DEBUG
			pManager.AddTextParameter("Debug", "D", "Debug console.", GH_ParamAccess.item);
			#endif
		}

        //////////////////////////////////////////////////////////////////////////
        #region RECEIVE
        protected virtual List<string> validHeaders { get; }
        protected string lastData;
		protected Queue<string> receiveQueue = new Queue<string>();
        protected bool MessagesAvailable{
            get {
                // TODO: Check if any messages are available on any network agent.
                return this.receiveQueue.Count > 0;
            }
        }
        protected void OnDataReceived(object sender, DataReceivedArgs data) {
			// TODO restructure to use HoloFabConnectedComponent
			string currentInput = data.data;
			UniversalDebug("New Message without Message Splitter removed: " + currentInput);
			string[] messageComponents = currentInput.Split(
				new string[] { EncodeUtilities.headerSplitter }, 2, 
				StringSplitOptions.RemoveEmptyEntries);
			if (messageComponents.Length > 1) {
				string header = messageComponents[0], content = messageComponents[1];
				UniversalDebug("Header: " + header + ", content: " + content);
				if (this.validHeaders.Contains(header)) {
					lock (this.receiveQueue){
						this.receiveQueue.Enqueue(content);
					}
                } else
					UniversalDebug("Header Not Recognized!", GH_RuntimeMessageLevel.Warning);
			} else
				UniversalDebug("Improper Message!", GH_RuntimeMessageLevel.Warning);
    }
		protected void NetworkReceiveSolve() {
            // Prepare to receive UI data.
            try {
                if (this.MessagesAvailable)  {
                    string currentData = string.Empty;
                    lock (this.receiveQueue) {
                        currentData = this.receiveQueue.Dequeue();
                    }
                    if (this.lastData != currentData) {
                        this.lastData = currentData;
                        ProcessNetworkInput(currentData);
                    }
                }
            }
            catch (Exception exception) {
                UniversalDebug("Error Processing Data: exception: " + exception.ToString(), GH_RuntimeMessageLevel.Error);
            }
        }
		public virtual void ProcessNetworkInput(string input) { }
		protected void ResetReceive() {
			this.receiveQueue = new Queue<string>();
            this.lastData = string.Empty;
        }
        #endregion
        //////////////////////////////////////////////////////////////////////////
        #region ADDITIONAL
        protected bool TryGetHoloConnect(IGH_DataAccess DA) {
			if (!DA.GetData<HoloConnection>(0, ref this.connect)) return false;
			else if (!this.wasConnected) {
				this.wasConnected = true;
				TryUpdateCommunicationData();
			}
			return true;
		}
		protected void TryUpdateCommunicationData() {
			if ((this.communicatorID == -1) && this.connect.status) {
				this.communicatorID = this.connect.RegisterAgent(this.communicationProtocolType, this.communicationType);
				if (this.communicationType == SourceCommunicationType.Receiver
					|| this.communicationType == SourceCommunicationType.SenderReceiver) {
					this.connect.RegisterReceiverCallback(this.communicatorID, OnDataReceived);
                }
				this.connect.RefreshOwner();
			}
		}
		// Common way to Communicate messages.
		protected void UniversalDebug(string message, GH_RuntimeMessageLevel messageType = GH_RuntimeMessageLevel.Remark) {
			#if DEBUG
			DebugUtilities.UniversalDebug(this.sourceName, message, ref this.debugMessages);
			#endif
			this.AddRuntimeMessage(messageType, message);
		}
		protected void ScheduleCallback(GH_Document document) {
			ExpireSolution(false);
		}
		//////////////////////////////////////////////////////////////////////////
		// Manage protocols
		// Update Component Message.
		protected void UpdateMessage() {
			if (this.allowProtocolChanging)
				this.Message = (this.communicationProtocolType == SourceType.UDP) ? "UDP" : "TCP";
		}
		// Action to be performed on Click.
		protected void SwitchProtocol(object sender, EventArgs eventArgs) {
			this.wasConnected = false;
			this.communicatorID = -1;
			this.communicationProtocolType = (this.communicationProtocolType == SourceType.UDP) ? SourceType.TCP : SourceType.UDP;
			UpdateMessage();
			// Update Grasshopper.
			Grasshopper.Instances.RedrawCanvas();
		}
		// Customize Grasshopper Component Menu.
		protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu) {
			// Create Base Menu
			base.AppendAdditionalComponentMenuItems(menu);
			if (this.allowProtocolChanging) { 
			// Add Custom Settings
				Menu_AppendSeparator(menu);
				Menu_AppendItem(menu, "Change Protocol(TCP/UDP)", SwitchProtocol, true);
			}
		}
		////////////////////////////////////////////////////////////////////////
		// Try to solve the saving of source Type.
		public override bool Write(GH_IO.Serialization.GH_IWriter writer) {
			// First add our own field.
			writer.SetInt32("sourceType", (int)this.communicationProtocolType);
			// Then call the base class implementation.
			return base.Write(writer);
		}
		public override bool Read(GH_IO.Serialization.GH_IReader reader) {
			// First read our own field.
			try {
				this.communicationProtocolType = (SourceType)reader.GetInt32("sourceType");
			}
			catch {
				this.communicationProtocolType = SourceType.UDP;
			}
			UpdateMessage();
			// Then call the base class implementation.
			return base.Read(reader);
		}
        #endregion
	}
}