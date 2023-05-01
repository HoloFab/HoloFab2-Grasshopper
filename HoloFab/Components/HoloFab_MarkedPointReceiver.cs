// #define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Newtonsoft.Json;
using HoloFab.CustomData;

namespace HoloFab {
	// A HoloFab class to receive UI elements from AR device.
	public class MarkedPointReceiver : HoloFabConnectedComponent {
        #region DEFAULT
        //////////////////////////////////////////////////////////////////////////
        // NECESSARY COMPONENT VARIABLES
        protected static string componentName = "Marked Point Receiver",
			componentNickname = "MP",
			componentDescription = "Receieves Incoming data from the Marked Points of AR device",
			componentCategory = "HoloFab",
			componentSubCategory = "Main";
        protected override string componentGUID { get { return "B9EBDE88-1BE4-4180-A131-11E334779695"; } }
		protected override System.Drawing.Bitmap componentIcon { get { return Properties.Resources.HoloFab_Logo; } }
		protected override SourceCommunicationType communicationType { get { return SourceCommunicationType.Receiver; } }
		protected override bool allowProtocolChanging { get { return false; } }
		// - debugging
		#if DEBUG
		protected override string sourceName { get { return "Marked Point Receiving Component"; } }
		#endif
		//////////////////////////////////////////////////////////////////////////
        public HoloFab_MarkedPointReceiver()
			: base(componentName, componentNickname, componentDescription,
				  componentCategory, componentSubCategory) {
        }
        #endregion
        //////////////////////////////////////////////////////////////////////////
        #region MAIN
        protected override List<string> validHeaders { get { return new List<string>() { "MPDATA" }; } }
        // - currents
        private List<Plane> currentPlanes = new List<Plane>();
        //////////////////////////////////////////////////////////////////////////
        public override bool GetInputs(IGH_DataAccess DA) {
			return true;
		}
		public override void Solve() {
			this.NetworkReceiveSolve();
		}
		public override void ProcessNetworkInput(string currentData) {
            // If any new data received - process it.
            MarkedPointData data = JsonConvert.DeserializeObject<MarkedPointData>(currentData);
			if (this.currentPlanes == null)
                this.currentPlanes = new List<Plane>();
            for (int i = 0; i < data.points.Count; i++) { 
				while (this.currentPlanes.Count <= i)
					this.currentPlanes.Add(new Plane());
				this.currentPlanes[i] = new Plane(
					new Point3d((double)data.points[i][0], (double)data.points[i][1], (double)data.points[i][2]),
					new Vector3d((double)data.normals[i][0], (double)data.normals[i][1], (double)data.normals[i][2]));

            }
            UniversalDebug("Data Received!");
        }
        public override void Reset() {
            this.currentPlanes = new List<Plane>();
			base.ResetReceive();
        }
        public override void SetOutputs(IGH_DataAccess DA) {
			DA.SetDataList(0, this.currentPlanes);
		}
        //////////////////////////////////////////////////////////////////////////
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			base.RegisterInputParams(pManager);
		}
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			base.RegisterOutputParams(pManager);
			pManager.AddPlaneParameter("Frames", "F", "Frames coming from Marked Points.", GH_ParamAccess.list);
		}
        #endregion
    }
}