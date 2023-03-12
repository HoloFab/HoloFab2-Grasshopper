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
	public class HoloTag : HoloFabConnectedComponent {
        #region DEFAULT
        //////////////////////////////////////////////////////////////////////////
        // NECESSARY COMPONENT VARIABLES
        protected static string componentName = "HoloTag Streaming",
			componentNickname = "HTS",
			componentDescription = "Streams spatial labels.",
			componentCategory = "HoloFab",
			componentSubCategory = "Main";
		protected override string componentGUID { get { return "51f68dce-f87b-4ae7-b7b5-4905cb8badd4"; } }
		protected override System.Drawing.Bitmap componentIcon { get { return Properties.Resources.HoloFab_HoloTag; } }
		protected override SourceCommunicationType communicationType { get { return SourceCommunicationType.Sender; } }
		protected override bool allowProtocolChanging { get { return false; } }
		// - debugging
		#if DEBUG
		protected override string sourceName { get { return "HoloTag Streaming Component"; } }
		#endif
		//////////////////////////////////////////////////////////////////////////
		public HoloTag()
			: base(componentName, componentNickname, componentDescription,
				  componentCategory, componentSubCategory) {
		}
		#endregion
		//////////////////////////////////////////////////////////////////////////
		#region MAIN
		// - default settings
		public float defaultTextSize = 20.0f;
		public Color defaultTextColor = Color.White;
		// - solution info
		private List<string> inputText = new List<string>();
		private List<Point3d> inputTextLocations = new List<Point3d>();
		private List<double> inputTextSize = new List<double>();
		private List<Color> inputTextColor = new List<Color>();

		//////////////////////////////////////////////////////////////////////////
		public override bool GetInputs(IGH_DataAccess DA) {
			string message;
			// Get inputs.
			this.inputText = new List<string>();
			this.inputTextLocations = new List<Point3d>();
			this.inputTextSize = new List<double>();
			this.inputTextColor = new List<Color>();
			if (!DA.GetDataList(1, inputText)) return false;
			if (!DA.GetDataList(2, inputTextLocations)) return false;
			DA.GetDataList(3, inputTextSize);
			DA.GetDataList(4, inputTextColor);
			// Check inputs.
			if (inputTextLocations.Count != inputText.Count) {
				message = "The number of 'tag locations' and 'tag texts' should be equal.";
				UniversalDebug(message, GH_RuntimeMessageLevel.Error);
				return false;
			}
			if ((inputTextSize.Count > 1) && (inputTextSize.Count != inputText.Count)) {
				message = "The number of 'tag text sizes' should be one or equal to one or the number of 'tag texts'.";
				UniversalDebug(message, GH_RuntimeMessageLevel.Error);
				return false;
			}
			if ((inputTextColor.Count > 1) && (inputTextColor.Count != inputText.Count)) {
				message = "The number of 'tag text colors' should be one or equal to one or the number of 'tag texts'.";
				UniversalDebug(message, GH_RuntimeMessageLevel.Error);
				return false;
			}
			return true;
		}
		public override void Solve() {
			List<string> currentTexts = new List<string>(){};
			List<float[]> currentTextLocations = new List<float[]>(){};
			List<float> currentTextSizes = new List<float>(){};
			List<int[]> currentTextColors = new List<int[]>(){};
			for(int i=0; i < inputText.Count; i++) {
				float currentSize = (float) ((inputTextSize.Count > 1) ? inputTextSize[i] : inputTextSize[0]);
				Color currentColor = (inputTextColor.Count > 1) ? inputTextColor[i] : inputTextColor[0];
				currentTexts.Add(inputText[i]);
				currentTextLocations.Add(EncodeUtilities.EncodeLocation(inputTextLocations[i]));
				currentTextSizes.Add((float)Math.Round(currentSize/1000.0, 3));
				currentTextColors.Add(EncodeUtilities.EncodeColor(currentColor));
			}
			LabelData tags = new LabelData(currentTexts, currentTextLocations, currentTextSizes, currentTextColors);
                
			// Send tag data.
			byte[] bytes = EncodeUtilities.EncodeData("HOLOTAG", tags, out string currentMessage);
			if (this.flagForce || (this.lastMessage != currentMessage)) {
				this.lastMessage = currentMessage;
				this.connect.QueueUpData(this.communicatorID, bytes);
				this.connect.RefreshOwner();
			}
		} 
		public override void SetOutputs(IGH_DataAccess DA) { }
        //////////////////////////////////////////////////////////////////////////
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			base.RegisterInputParams(pManager);
			pManager.AddTextParameter("Text String", "S", "Tags as string", GH_ParamAccess.list);
			pManager.AddPointParameter("Text Location", "L", "Sets the location of Tags", GH_ParamAccess.list);
			pManager.AddNumberParameter("Text Size", "TS", "Size of text Tags in AR environment", GH_ParamAccess.list, this.defaultTextSize);
			pManager[3].Optional = true;
			pManager.AddColourParameter("Text Color", "C", "Color of text Tags", GH_ParamAccess.list, this.defaultTextColor);
			pManager[4].Optional = true;
		}
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			base.RegisterOutputParams(pManager);
		}
        #endregion
        //////////////////////////////////////////////////////////////////////////
    }
}