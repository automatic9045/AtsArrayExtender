using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using BveTypes;
using BveTypes.ClassWrappers;
using FastMember;
using Mackoy.Bvets;
using ObjectiveHarmonyPatch;
using TypeWrapping;

namespace AtsArrayExtender
{
    public class InputDevice : IInputDevice
    {
        private static readonly Type[] PluginLoaderConstructorParamTypes;

        static InputDevice()
        {
#if DEBUG
            Debugger.Launch();
#endif

            Assembly assembly = Assembly.GetExecutingAssembly();
            string searchLocation = Path.Combine(Path.GetDirectoryName(assembly.Location), "AtsArrayExtender");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                AssemblyName assemblyName = new AssemblyName(e.Name);
                string assemblyPath = Path.Combine(searchLocation, assemblyName.Name + ".dll");

                return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
            };

            PluginLoaderConstructorParamTypes = GetPluginLoaderConstructorParamTypes();


            Type[] GetPluginLoaderConstructorParamTypes() => new[]
            {
                typeof(UserVehicleLocationManager),
                typeof(KeyProvider),
                typeof(HandleSet),
                typeof(HandleSet),
                typeof(VehicleStateStore),
                typeof(SectionManager),
                typeof(MapFunctionList),
                typeof(DoorSet),
            };
        }

        private readonly BveTypeSet BveTypes;
        private readonly HarmonyPatch ConstructPluginLoaderPatch;

        public event InputEventHandler LeverMoved;
        public event InputEventHandler KeyDown;
        public event InputEventHandler KeyUp;

        public InputDevice()
        {
            try
            {
                Assembly bveAssembly = Assembly.GetEntryAssembly();
                Version bveVersion = bveAssembly.GetName().Version;
                BveTypes = BveTypeSet.Load(bveAssembly, bveVersion, true);

                ClassMemberSet pluginLoaderMembers = BveTypes.GetClassInfoOf<PluginLoader>();
                FastConstructor pluginLoaderConstructor = pluginLoaderMembers.GetSourceConstructor(PluginLoaderConstructorParamTypes);
                
                ConstructPluginLoaderPatch = HarmonyPatch.Patch(nameof(AtsArrayExtender), pluginLoaderConstructor.Source, PatchType.Postfix);
                ConstructPluginLoaderPatch.Invoked += PluginLoaderConstructed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Failed to initialize.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public void Dispose()
        {
            ConstructPluginLoaderPatch?.Dispose();
        }

        private PatchInvokationResult PluginLoaderConstructed(object sender, PatchInvokedEventArgs e)
        {
            PluginLoader pluginLoader = PluginLoader.FromSource(e.Instance);

            pluginLoader._PanelArray = new int[1024];
            pluginLoader._SoundArray = new int[1024];
            pluginLoader._OldSoundArray = new int[1024];

            pluginLoader.StateStore.PanelArray = new double[1024];

            return PatchInvokationResult.DoNothing(e);
        }

        public void Configure(IWin32Window owner)
        {
        }

        public void Load(string settingsPath)
        {
        }

        public void SetAxisRanges(int[][] ranges)
        {
        }

        public void Tick()
        {
        }
    }
}
