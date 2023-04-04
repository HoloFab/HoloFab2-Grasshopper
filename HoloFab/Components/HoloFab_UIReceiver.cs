// #define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Newtonsoft.Json;

using HoloFab.CustomData;

namespace HoloFab {
	// A HoloFab class to receive UI elements from AR device.
	public class UIReceiver : HoloFabConnectedComponent {
        #region DEFAULT
        //////////////////////////////////////////////////////////////////////////
        // NECESSARY COMPONENT VARIABLES
        protected static string componentName = "UI Receiver",
			componentNickname = "UI",
			componentDescription = "Receieves Incoming data from the User Interface of AR device",
			componentCategory = "HoloFab",
			componentSubCategory = "UserInterface";
        protected override string componentGUID { get { return "ac5f5de3-cdf2-425a-b435-c97b718e1a09"; } }
		protected override System.Drawing.Bitmap componentIcon { get { return Properties.Resources.HoloFab_UIReceiver; } }
		protected override SourceCommunicationType communicationType { get { return SourceCommunicationType.Receiver; } }
		protected override bool allowProtocolChanging { get { return false; } }
		// - debugging
		#if DEBUG
		protected override string sourceName { get { return "UI Receiving Component"; } }
		#endif
		//////////////////////////////////////////////////////////////////////////
		public UIReceiver()
			: base(componentName, componentNickname, componentDescription,
				  componentCategory, componentSubCategory) {
		}
        #endregion
        //////////////////////////////////////////////////////////////////////////
        protected override List<string> validHeaders { get { return new List<string>() { "UIDATA" }; } }
        #region MAIN
        // - currents
        private List<bool> currentBools = new List<bool>();
		private List<int> currentInts = new List<int>();
		private List<float> currentFloats = new List<float>();
        private Queue<string> receiveQueue = new Queue<string>();
        //////////////////////////////////////////////////////////////////////////
        public override bool GetInputs(IGH_DataAccess DA) {
			return true;
		}
		public override void Solve() {
			this.NetworkReceiveSolve();
		}
		public override void ProcessNetworkInput(string currentData) {
            // If any new data received - process it.
            UIData data = JsonConvert.DeserializeObject<UIData>(currentData);
            this.currentBools = new List<bool>(data.bools);
            this.currentInts = new List<int>(data.ints);
            this.currentFloats = new List<float>(data.floats);
            UniversalDebug("Data Received!");
        }
        public override void Reset() {
			this.currentBools = new List<bool>();
			this.currentInts = new List<int>();
			this.currentFloats = new List<float>();
			base.ResetReceive();
        }
        public override void SetOutputs(IGH_DataAccess DA) {
			DA.SetDataList(0, this.currentBools);
			DA.SetDataList(1, this.currentInts);
			DA.SetDataList(2, this.currentFloats);
		}
        //////////////////////////////////////////////////////////////////////////
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			base.RegisterInputParams(pManager);
		}
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			base.RegisterOutputParams(pManager);
			pManager.AddBooleanParameter("Toggles", "T", "Boolean values coming from UI.", GH_ParamAccess.list);
			pManager.AddIntegerParameter("Counters", "C", "Integer values coming from UI.", GH_ParamAccess.list);
			pManager.AddNumberParameter("Sliders", "S", "Float values coming from UI.", GH_ParamAccess.list);
		}
        #endregion
	}
}