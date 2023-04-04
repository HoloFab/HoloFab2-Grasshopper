// #define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;

using System.Drawing;
using Rhino.Geometry;
using Grasshopper.Kernel;

using HoloFab.CustomData;

namespace HoloFab {
	// A HoloFab class to send mesh to AR device via TCP.
	public class MeshStreaming : HoloFabConnectedComponent {
        #region DEFAULT
        //////////////////////////////////////////////////////////////////////////
        // NECESSARY COMPONENT VARIABLES
        protected static string componentName = "Mesh Streaming",
			componentNickname = "MS",
			componentDescription = "Streams 3D Meshes",
			componentCategory = "HoloFab",
			componentSubCategory = "Main";
		protected override string componentGUID { get { return "86810363-cded-40bc-ad04-0d6a5b484b02"; } }
		protected override System.Drawing.Bitmap componentIcon { get { return Properties.Resources.HoloFab_MeshStreaming; } }
		protected override SourceCommunicationType communicationType { get { return SourceCommunicationType.Sender; } }
		protected override bool allowProtocolChanging { get { return true; } }
		// - debugging
		#if DEBUG
		protected override string sourceName { get { return "Mesh Streaming Component"; } }
		#endif
		//////////////////////////////////////////////////////////////////////////
		public MeshStreaming()
			: base(componentName, componentNickname, componentDescription,
				  componentCategory, componentSubCategory) {
		}
        #endregion
        //////////////////////////////////////////////////////////////////////////
        #region MAIN
        // - default settings
        private Color defaultColor = Color.Red;
		// - solution info
		private List<Mesh> inputMeshes = new List<Mesh>();
		private List<Color> inputColor = new List<Color>();

		//////////////////////////////////////////////////////////////////////////
		public override bool GetInputs(IGH_DataAccess DA) {
			string message;
			// Get inputs.
			this.inputMeshes = new List<Mesh>();
			this.inputColor = new List<Color>();
			if (!DA.GetDataList(1, this.inputMeshes)) return false;
			DA.GetDataList(2, this.inputColor);
			// Check inputs.
			if ((this.inputColor.Count > 1) && (this.inputColor.Count != this.inputMeshes.Count)) {
				message = (this.inputColor.Count > this.inputMeshes.Count) ?
						  "The number of Colors does not match the number of Mesh objects. Extra colors will be ignored." :
						  "The number of Colors does not match the number of Mesh objects. The last color will be repeated.";
				UniversalDebug(message, GH_RuntimeMessageLevel.Warning);
			}
			return true;
		}
		public override void Solve() {
			// Encode mesh data.
			List<MeshData> inputMeshData = new List<MeshData> { };
			for (int i = 0; i < this.inputMeshes.Count; i++) {
				Color currentColor = this.inputColor[Math.Min(i, this.inputColor.Count)];
				inputMeshData.Add(MeshUtilities.EncodeMesh(this.inputMeshes[i], currentColor));
			}
			// Send mesh data.
			byte[] bytes = EncodeUtilities.EncodeData("MESHSTREAMING", inputMeshData, out string currentMessage);
			if (this.flagForce || (this.lastMessage != currentMessage)) {
				this.lastMessage = currentMessage;
				this.connect.QueueUpData(this.communicatorID, bytes);
				this.connect.RefreshOwner();
			}
		}
		public override void Reset() { }
        public override void SetOutputs(IGH_DataAccess DA) { }
        //////////////////////////////////////////////////////////////////////////
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			base.RegisterInputParams(pManager);
			pManager.AddMeshParameter("Mesh", "M", "Mesh object to be encoded and sent via TCP.", GH_ParamAccess.list);
			pManager.AddColourParameter("Color", "C", "Color for each Mesh object.", GH_ParamAccess.list, this.defaultColor);
		}
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			base.RegisterOutputParams(pManager);
		}
        #endregion
        //////////////////////////////////////////////////////////////////////////
    }
}