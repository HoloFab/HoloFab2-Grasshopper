// #define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Attributes;

using HoloFab.CustomData;

namespace HoloFab {
	// A HoloFab class to create Connection object used in other HoloFab components.
	public class HoloConnect : GH_Component {
		//////////////////////////////////////////////////////////////////////////
		// - default settings
		private string defaultIP = "127.0.0.1";
        // - settings
        // If messages in queues - expire solution after this time.
        private readonly int expireDelay = 40;

		public ClientFinder deviceFinder;
		public HoloConnection connect;

		// - debugging
		#if DEBUG
		private string sourceName = "HoloConnect Component";
		public List<string> debugMessages = new List<string>();
		#endif
        
		/// <summary>
		/// This is the method that actually does the work.
		/// </summary>
		/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
		protected override void SolveInstance(IGH_DataAccess DA) {
			if (this.deviceFinder == null) {
                this.deviceFinder = new ClientFinder(this);
				this.deviceFinder.Connect();
                this.deviceFinder.StartReceiving();
			}
			// Get inputs.
			string remoteIP = this.defaultIP;
			if (!DA.GetData(0, ref remoteIP)) return;
			//////////////////////////////////////////////////////
			if (this.connect == null) // New Connection.
				this.connect = new HoloConnection(this, remoteIP);
			else if (this.connect.remoteIP != remoteIP) {
				// If IP Changed first Disconnect the old one.
				this.connect.Disconnect();
				this.connect = new HoloConnection(this, remoteIP);
			}

			if (this.connect.status) {
				if (!this.deviceFinder.devices.ContainsKey(remoteIP)) { 
					this.connect.Disconnect();
					//ExpireSolution(false);

					string message = "Client not found.";
					GH_RuntimeMessageLevel messageType = GH_RuntimeMessageLevel.Error;
					UniversalDebug(message, messageType);
				} else {
					this.connect.SetupUpdate();
					//// Start connections
					///// Now managed by the acknowleger
					//bool success = this.connect.Connect();
					//string message = (success)
					//	? "Connection established."
					//	: "Connection failed, please check your network connection and try again.";
					//GH_RuntimeMessageLevel messageType = (success)
					//	? GH_RuntimeMessageLevel.Remark
					//	: GH_RuntimeMessageLevel.Error;
					//UniversalDebug(message, messageType);
				}
			} else {
				this.connect.Disconnect();
			}
			//////////////////////////////////////////////////////
			// Output.
			DA.SetData(0, this.connect);
			#if DEBUG
			if (this.debugMessages.Count > 0)
				DA.SetData(1, this.debugMessages[this.debugMessages.Count-1]);
			#endif
			
			// Expire Solution.
			if (connect.status) {//((connect.status) && (connect.MessagesAvailable)) {
                GH_Document document = this.OnPingDocument();
				if (document != null)
					document.ScheduleSolution(this.expireDelay, ScheduleCallback);
			}
		}
		private void ScheduleCallback(GH_Document document) {
			if (this.deviceFinder.FlagChanged) {
				ExpireSolution(true);
                Instances.InvalidateCanvas();
				this.deviceFinder.FlagChanged = false;
            } else
				ExpireSolution(false);
        }
		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Initializes a new instance of the CreateConnection class.
		/// Each implementation of GH_Component must provide a public
		/// constructor without any arguments.
		/// Category represents the Tab in which the component will appear,
		/// Subcategory the panel. If you use non-existing tab or panel names,
		/// new tabs/panels will automatically be created.
		/// </summary>
		public HoloConnect()
			: base("Create Connection", "C",
			       "Sets the Ip address of receiver",
			       "HoloFab", "Communication") {}
        
		public override void CreateAttributes() {
			this.m_attributes = new HoloConnect_Attributes_Custom(this);
		}
		/// <summary>
		/// Provides an Icon for every component that will be visible in the User Interface.
		/// Icons need to be 24x24 pixels.
		/// </summary>
		protected override System.Drawing.Bitmap Icon {
			get { return Properties.Resources.HoloFab_Logo; }
		}
		/// <summary>
		/// Each component must have a unique Guid to identify it.
		/// It is vital this Guid doesn't change otherwise old ghx files
		/// that use the old ID will partially fail during loading.
		/// </summary>
		public override Guid ComponentGuid {
			get { return new Guid("54aac8c6-1cfa-43e2-9a0c-0f5c3422146c"); }
		}
		/// <summary>
		/// Registers all the input parameters for this component.
		/// </summary>
		protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
			pManager.AddTextParameter("Address", "@", "Remote IP address of the AR device.", GH_ParamAccess.item, this.defaultIP);
		}
		/// <summary>
		/// Registers all the output parameters for this component.
		/// </summary>
		protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
			pManager.AddGenericParameter("Connect", "Cn", "Connection object to be used in other HoloFab components.", GH_ParamAccess.list);
			#if DEBUG
			pManager.AddTextParameter("Debug", "D", "Debug console.", GH_ParamAccess.item);
			#endif
		}
		////////////////////////////////////////////////////////////////////////
		// Common way to Communicate messages.
		private void UniversalDebug(string message, GH_RuntimeMessageLevel messageType = GH_RuntimeMessageLevel.Remark) {
			#if DEBUG
			DebugUtilities.UniversalDebug(this.sourceName, message, ref this.debugMessages);
			#endif
			this.AddRuntimeMessage(messageType, message);
		}
	}
	//////////////////////////////////////////////////////////////////////////
	// A structure to extend Component appearance.
	public class HoloConnect_Attributes_Custom : Grasshopper.Kernel.Attributes.GH_ComponentAttributes {
		private HoloConnect component;
		private System.Drawing.Rectangle BoundsButton { get; set; }
		private System.Drawing.Rectangle BoundsText { get; set; }
        
		public HoloConnect_Attributes_Custom(GH_Component owner) : base(owner) {
			this.component = this.Owner as HoloConnect;
		}
        
		protected override void Layout() {
			// Create default layout.
			base.Layout();
			// Extend Component Bounds.
			System.Drawing.Rectangle boundsComponent = GH_Convert.ToRectangle(this.Bounds);
			boundsComponent.Height += 22;
			this.Bounds = boundsComponent;
			// Generate Button Bounds.
			System.Drawing.Rectangle boundsButton = boundsComponent;
			boundsButton.Y = boundsButton.Bottom - 22;
			boundsButton.Height = 22;
			boundsButton.Inflate(-2, -2);
			this.BoundsButton = boundsButton;
			// Generate Text Bounds.
			System.Drawing.Rectangle boundsText = boundsComponent;
			boundsText.Y = boundsText.Bottom + 20;
			boundsText.X -= 25;
			boundsText.Height = 100;
			boundsText.Width += 50;
			boundsText.Inflate(-2, -2);
			this.BoundsText = boundsText;
		}
        
		protected override void Render(GH_Canvas canvas,
		                               System.Drawing.Graphics graphics,
		                               GH_CanvasChannel channel) {
			// Draw base component.
			base.Render(canvas, graphics, channel);
			// Add custom elements.
			if (channel == GH_CanvasChannel.Objects) {
				// Add list of Found Devices.
				// IPAddress ipv4Addresse = Array.FindLast(Dns.GetHostEntry(string.Empty).AddressList,
				//                                         a => a.AddressFamily == AddressFamily.InterNetwork);
				if (this.component.deviceFinder != null) { 
                    string message = "Devices: ";
					if (this.component.deviceFinder.devices.Count > 0) { 
					    foreach (HoloDevice device in this.component.deviceFinder.devices.Values)
						    message += "\n" + device.ToString();
					}
				    else
					    message += "(not found)";
				    graphics.DrawString(message, GH_FontServer.NewFont(FontFamily.GenericMonospace, 6, FontStyle.Regular),
				                        Brushes.Black, this.BoundsText, GH_TextRenderingConstants.CenterCenter);
					//// if client not visible - disconnect
					//if ((this.component.connect != null) 
					//	&& (this.component.connect.status) 
					//	&& (!HoloConnect.deviceFinder.devices.ContainsKey(this.component.connect.remoteIP))) {
					//	this.component.connect.Disconnect();
					//	this.component.ExpireSolution(true);
					//}
				}
				// Add button to connect/disconect.
				if (this.component.connect != null) { 
					GH_Capsule button = GH_Capsule.CreateTextCapsule(this.BoundsButton, this.BoundsButton, GH_Palette.Black,
																 this.component.connect.status ? "Disconnect" : "Connect", 2, 0);
					button.Render(graphics, this.Selected, this.Owner.Locked, false);
					button.Dispose();
				}
			}
		}
		public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender,
		                                                     GH_CanvasMouseEvent canvasMouseEvent) {
			// Intersept mose click event, if clicked on the button.
			if (canvasMouseEvent.Button == System.Windows.Forms.MouseButtons.Left) {
				System.Drawing.RectangleF boundsButton = this.BoundsButton;
				if (boundsButton.Contains(canvasMouseEvent.CanvasLocation)) {
					if (this.component.connect != null) {
						this.component.connect.status = !this.component.connect.status;
						this.component.ExpireSolution(true);
						return GH_ObjectResponse.Handled;
					}
				}
			}
			// If not - perform normal reaction.
			return base.RespondToMouseDown(sender, canvasMouseEvent);
		}
	}
}