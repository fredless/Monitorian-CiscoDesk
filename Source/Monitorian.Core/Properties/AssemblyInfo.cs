using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Monitorian.Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Monitorian.Core")]
[assembly: AssemblyCopyright("Copyright © 2019 emoacht")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("0c509b78-ff37-4f5d-9582-189ee5316c27")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// The first three parts follow the version of upstream Monitorian this fork is based on.
// The fourth part counts releases of this fork on top of that upstream version.
[assembly: AssemblyVersion("4.15.0.1")]
[assembly: AssemblyFileVersion("4.15.0.1")]
[assembly: AssemblyInformationalVersion("4.15.0.1 (Cisco Desk fork of Monitorian 4.15.0)")]
[assembly: NeutralResourcesLanguage("en-US")]

// For unit test
[assembly: InternalsVisibleTo("Monitorian.Test")]
