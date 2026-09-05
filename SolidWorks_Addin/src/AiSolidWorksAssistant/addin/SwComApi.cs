using System;
using System.Runtime.InteropServices;

namespace Ai.SolidWorksAssistant.Addin
{
    /// <summary>
    /// COM declaration of SOLIDWORKS' add-in callback interface (swpublished).
    /// GUID verified against the shipped SolidWorks.Interop.swpublished metadata:
    /// {DA306A0D-EAC5-4406-8610-B1DA805D9270}, IUnknown-based.
    /// SOLIDWORKS CoCreates the registered add-in class and QueryInterface()s this IID.
    /// </summary>
    [ComImport]
    [Guid("DA306A0D-EAC5-4406-8610-B1DA805D9270")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISwAddin
    {
        /// <summary>Called by SOLIDWORKS when the add-in is loaded. ThisSW is the ISldWorks application object.</summary>
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool ConnectToSW([MarshalAs(UnmanagedType.IDispatch), In] object ThisSW, [In] int Cookie);

        /// <summary>Called by SOLIDWORKS when the add-in is unloaded.</summary>
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool DisconnectFromSW();
    }

    /// <summary>
    /// Minimal subset of swconst enum values we need in Phase 1.
    /// Kept as plain int constants so the build does not depend on interop assemblies.
    /// </summary>
    public static class SwConst
    {
        // swDocumentTypes_e
        public const int swDocNONE = 0;
        public const int swDocPART = 1;
        public const int swDocASSEMBLY = 2;
        public const int swDocDRAWING = 3;

        // swEndConditions_e (subset)
        public const int swEndCondBlind = 0;
        public const int swEndCondThroughAll = 1;

        // swSelectType_e (subset)
        public const string SelTypePlane = "PLANE";

        // swCommandItemType_e
        public const int swDefaultButton = 0;

        // CommandManager cookie/group ids used by this add-in
        public const int CommandGroupId = 99;
    }
}
