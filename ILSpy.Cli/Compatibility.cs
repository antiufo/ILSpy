﻿using ICSharpCode.Decompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.ILSpy;
using System.Collections.ObjectModel;
using System.IO;

namespace System.Windows.Threading
{
	internal enum DispatcherPriority
	{
		Background,
		Normal,
		ContextIdle
	}


}
namespace System.Windows.Controls
{
	internal class TextBlock
	{
		public string Text;
	}
}

namespace ICSharpCode.ILSpy.TextView
{
	public class DecompilerTextViewState
	{

	}

	internal class DecompilerTextView
	{
		internal static string CleanUpName(string text)
		{
			int pos = text.IndexOf(':');
			if (pos > 0)
				text = text.Substring(0, pos);
			pos = text.IndexOf('`');
			if (pos > 0)
				text = text.Substring(0, pos);
			text = text.Trim();
			foreach (char c in Path.GetInvalidFileNameChars())
				text = text.Replace(c, '-');
			return text;
		}
	}
}

namespace System.ComponentModel.Composition
{
	public class ExportAttribute : Attribute
	{
		public ExportAttribute(Type t)
		{
		}
	}
}
namespace App
{
	internal class CommandLineArguments
	{
		internal static Guid? FixedGuid;
	}
	internal class CompositionContainer
	{
		internal static IEnumerable<T> GetExportedValues<T>()
		{
			if (typeof(T) == typeof(IResourceFileHandler))
			{

				// BamlDecompiler is in an assembly that references the full ILSpy GUI. Ignore BAML.
				// return new[] { (T)(object)new BamlResourceFileHandler()  }; 
				return Enumerable.Empty<T>();
			}
			throw new NotSupportedException();
		}
	}
}

namespace App.Current
{
	internal static class Dispatcher
	{
		public static void BeginInvoke(DispatcherPriority background, Action action)
		{
			// Don't execute anything. All the three call site don't apply in CLI.
			// action();
		}

		public static void VerifyAccess()
		{
		}

		private static int lockThread = -1;
	

		internal static TResult Invoke<T1, TResult>(DispatcherPriority normal, Func<T1, TResult> func, T1 arg1)
		{
			lock (typeof(Dispatcher))
			{
				var old = lockThread;
				lockThread = Environment.CurrentManagedThreadId;
				try
				{
					return func(arg1);
				}
				finally
				{
					lockThread = old;
				}
			}
		}

		public static bool CheckAccess()
		{
			lock (typeof(Dispatcher))
			{
				return lockThread == Environment.CurrentManagedThreadId;
			}
		}
	}
}

namespace ICSharpCode.ILSpy
{

	internal class MainWindow
	{
		public static MainWindow Instance = new MainWindow();
		public AssemblyList CurrentAssemblyList;
	}
	internal static class ExtensionMethods
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> toadd)
		{
			foreach (var item in toadd)
			{
				collection.Add(item);
			}
		}
	}
}
namespace ICSharpCode.ILSpy.Options
{
	internal class DecompilerSettingsPanel
	{
		public static DecompilerSettings CurrentDecompilerSettings;
	}
}