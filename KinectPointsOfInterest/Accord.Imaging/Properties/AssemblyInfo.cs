using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Accord.Imaging")]
[assembly: AssemblyDescription("Accord.NET - Imaging Library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Accord.NET")]
[assembly: AssemblyProduct("Accord.Imaging")]
[assembly: AssemblyCopyright("Copyright © César Souza 2009-2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f73f155f-86f3-4b7e-8523-e64477824534")]

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
[assembly: AssemblyVersion("2.1.4.0")]
[assembly: AssemblyFileVersion("2.1.4.0")]


[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "k", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#.ctor(System.Single)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "k", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#.ctor(System.Single,System.Single)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "k", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#.ctor(System.Single,System.Single,System.Double)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "K", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#K")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#ProcessImage(AForge.Imaging.UnmanagedImage)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#ProcessImage(AForge.Imaging.UnmanagedImage)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#ProcessImage(System.Drawing.Bitmap)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#ProcessImage(System.Drawing.Imaging.BitmapData)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "height+1", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#.ctor(System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "width+1", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#.ctor(System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "m", Scope = "member", Target = "Accord.Imaging.MatrixH.#.ctor(System.Single,System.Single,System.Single,System.Single,System.Single,System.Single,System.Single,System.Single)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "m", Scope = "member", Target = "Accord.Imaging.MatrixH.#.ctor(System.Single,System.Single,System.Single,System.Single,System.Single,System.Single,System.Single,System.Single,System.Single)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "Accord.Imaging.MatrixH.#Elements")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Scope = "member", Target = "Accord.Imaging.Tools.#Normalize(Accord.Imaging.PointH[],Accord.Imaging.MatrixH&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Scope = "member", Target = "Accord.Imaging.Tools.#Normalize(System.Drawing.PointF[],Accord.Imaging.MatrixH&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "height", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSum(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "width", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSum(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "height", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSum2(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "width", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSum2(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w", Scope = "member", Target = "Accord.Imaging.PointH.#.ctor(System.Single,System.Single,System.Single)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "W", Scope = "member", Target = "Accord.Imaging.PointH.#W")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "measure", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#initialize(Accord.Imaging.HarrisCornerMeasure,System.Single,System.Single,System.Double,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sigma", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#initialize(Accord.Imaging.HarrisCornerMeasure,System.Single,System.Single,System.Double,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "size", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#initialize(Accord.Imaging.HarrisCornerMeasure,System.Single,System.Single,System.Double,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "threshold", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#initialize(Accord.Imaging.HarrisCornerMeasure,System.Single,System.Single,System.Double,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "height", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSumT(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "width", Scope = "member", Target = "Accord.Imaging.IntegralImage2.#GetSumT(System.Int32,System.Int32,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "k", Scope = "member", Target = "Accord.Imaging.HarrisCornersDetector.#initialize(Accord.Imaging.HarrisCornerMeasure,System.Single,System.Single,System.Double,System.Int32,System.Int32)")]

