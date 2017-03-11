using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;
using Shaman.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILSpy.Cli
{
	class Program
	{

		[Configuration(CommandLineAlias = "input")]
		public static string[] Configuration_Input;

		[Configuration(CommandLineAlias = "references")]
		public static string[] Configuration_References;

		[Configuration(CommandLineAlias = "output")]
		public static string Configuration_OutputFolder;

		[Configuration(CommandLineAlias = "deterministic")]
		public static bool Configuration_Deterministic;




		static int Main(string[] args)
		{
			try
			{
				ConfigurationManager.Initialize(typeof(Program).Assembly, false);
				MainInternal(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return 1;
			}
			return 0;
		}

		static void MainInternal(string[] args)
		{
			var settings = new ILSpySettings();
			var assemblyListManager = new AssemblyListManager(settings);
			var assemblyList = assemblyListManager.LoadList(settings, string.Empty);
			MainWindow.Instance.CurrentAssemblyList = assemblyList;

			if (Configuration_OutputFolder == null) throw new ArgumentNullException("output");
			if (Configuration_Input == null || Configuration_Input.Length == 0) throw new ArgumentNullException("input");

			if (Configuration_References != null)
			{
				foreach (var refer in Configuration_References)
				{
					assemblyList.OpenAssembly(refer);
				}
			}

			var asmsToDecompile = new List<LoadedAssembly>();
			foreach (var dll in Configuration_Input)
			{
				asmsToDecompile.Add(assemblyList.OpenAssembly(dll));
			}

			if (Configuration_Deterministic) App.CommandLineArguments.FixedGuid = default(Guid);
			var lang = new ICSharpCode.ILSpy.CSharpLanguage();

			var decompSettings = new DecompilerSettings();
			var decompOptions = new DecompilationOptions();
			decompOptions.FullDecompilation = true;

			DecompilerSettingsPanel.CurrentDecompilerSettings = decompSettings;
			if (Configuration_Deterministic)
			{
				decompSettings.UseDebugSymbols = false;
				decompSettings.Deterministic = true;
			}
			decompSettings.QueryExpressions = false;
			decompOptions.DecompilerSettings = decompSettings;

			foreach (var asm in asmsToDecompile)
			{
				
				decompOptions.SaveAsProjectDirectory = Path.Combine(Configuration_OutputFolder, Path.GetFileNameWithoutExtension(asm.FileName));
				var sw = new PlainTextOutput();
				lang.DecompileAssembly(asm, sw, decompOptions);
				File.WriteAllText(Path.Combine(decompOptions.SaveAsProjectDirectory, Path.GetFileNameWithoutExtension(asm.FileName) + ".csproj"), sw.ToString(), Encoding.UTF8);
			}
			

		}
	}
}
