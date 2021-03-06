﻿Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports SMRUCC.Rsharp.Runtime.Interop

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes
#if netcore5=0 then
<Assembly: AssemblyTitle("R# graphics api library module")>
<Assembly: AssemblyDescription("R# graphics api library module")>
<Assembly: AssemblyCompany("SMRUCC")>
<Assembly: AssemblyProduct("R.graphics")>
<Assembly: AssemblyCopyright("Copyright © xie.guigang@gcmodeller.org 2019")>
<Assembly: AssemblyTrademark("R#")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid("dcdcf985-5dc5-42c5-9ac1-e895283a7139")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion("1.0.*")>

<Assembly: AssemblyVersion("1.33.*")>
<Assembly: AssemblyFileVersion("1.0.*")>
#end if
<Assembly: RPackageModule>