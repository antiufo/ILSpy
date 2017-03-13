// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;

using ICSharpCode.Decompiler;
using Mono.Cecil;
using System.Drawing;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Base class for language-specific decompiler implementations.
	/// </summary>
	public abstract class Language
	{
		/// <summary>
		/// Gets the name of the language (as shown in the UI)
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the file extension used by source code files in this language.
		/// </summary>
		public abstract string FileExtension { get; }

		public virtual string ProjectFileExtension
		{
			get { return null; }
		}

#if !CLI
		/// <summary>
		/// Gets the syntax highlighting used for this language.
		/// </summary>
		public virtual ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition SyntaxHighlighting
		{
			get
			{
				return ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(this.FileExtension);
			}
		}
#endif

		public virtual void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, true) + "." + method.Name);
		}

		public virtual void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, true) + "." + property.Name);
		}

		public virtual void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, true) + "." + field.Name);
		}

		public virtual void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, true) + "." + ev.Name);
		}

		public virtual void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(type, true));
		}

		public virtual void DecompileNamespace(string nameSpace, IEnumerable<TypeDefinition> types, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, nameSpace);
		}

		public virtual void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, assembly.FileName);
			if (assembly.AssemblyDefinition != null) {
				var name = assembly.AssemblyDefinition.Name;
				if (name.IsWindowsRuntime) {
					WriteCommentLine(output, name.Name + " [WinRT]");
				} else {
					WriteCommentLine(output, name.FullName);
				}
			} else {
				WriteCommentLine(output, assembly.ModuleDefinition.Name);
			}
		}

		public virtual void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("// " + comment);
		}

		/// <summary>
		/// Converts a type reference into a string. This method is used by the member tree node for parameter and return types.
		/// </summary>
		public virtual string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
		{
			if (includeNamespace)
				return type.FullName;
			else
				return type.Name;
		}

		/// <summary>
		/// Converts a member signature to a string.
		/// This is used for displaying the tooltip on a member reference.
		/// </summary>
		public virtual string GetTooltip(MemberReference member)
		{
			if (member is TypeReference)
				return TypeToString((TypeReference)member, true);
			else
				return member.ToString();
		}

		public virtual string FormatPropertyName(PropertyDefinition property, bool? isIndexer = null)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			return property.Name;
		}

		public virtual string FormatMethodName(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			return method.Name;
		}

		public virtual string FormatTypeName(TypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type.Name;
		}

		/// <summary>
		/// Used for WPF keyboard navigation.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		public virtual bool ShowMember(MemberReference member)
		{
			return true;
		}

		/// <summary>
		/// Used by the analyzer to map compiler generated code back to the original code's location
		/// </summary>
		public virtual MemberReference GetOriginalCodeLocation(MemberReference member)
		{
			return member;
		}

#region WriteResourceFilesInProject
		protected virtual IEnumerable<Tuple<string, string>> WriteResourceFilesInProject(LoadedAssembly assembly, DecompilationOptions options, HashSet<string> directories)
		{
			foreach (EmbeddedResource r in assembly.ModuleDefinition.Resources.OfType<EmbeddedResource>()) {
				
				Stream stream = r.GetResourceStream();
				stream.Position = 0;

				if (r.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
				{
					var res = new ResourcesFileTreeNode(r);
					res.EnsureLazyChildren();
					var refs = new Dictionary<string, ResXFileRef>();
					foreach (var resourceItem in res.Children.Cast<ResourceEntryNode>())
					{
						
						
						
						string fileName = Path.Combine("Resources", Path.Combine(((string)resourceItem.Key).Split('/').Select(p => TextView.DecompilerTextView.CleanUpName(p)).ToArray()));
						string dirName = Path.GetDirectoryName(fileName);
						if (!string.IsNullOrEmpty(dirName) && directories.Add(dirName)) {
							Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dirName));
						}
						Stream entryStream = resourceItem.Data;
						bool handled = false;
						foreach (var handler in App.CompositionContainer.GetExportedValues<IResourceFileHandler>()) {
							if (handler.CanHandle(fileName, options)) {
								handled = true;
								entryStream.Position = 0;
								yield return Tuple.Create(handler.EntryType, handler.WriteResourceToFile(assembly, fileName, entryStream, options));
								break;
							}
						}
						if (!handled) {
							var ext = Path.GetExtension(fileName);
							if (string.IsNullOrEmpty(ext))
							{
								entryStream.Position = 0;
								ext = SniffFileType(entryStream);
								if (ext != null) fileName += ext;
							}
							using (FileStream fs = new FileStream(Path.Combine(options.SaveAsProjectDirectory, fileName), FileMode.Create, FileAccess.Write)) {
								entryStream.Position = 0;
								entryStream.CopyTo(fs);
							}
							refs.Add(resourceItem.Key, new ResXFileRef(fileName, resourceItem.Type.AssemblyQualifiedName));
							//yield return Tuple.Create("EmbeddedResource", fileName);
						}
					}
						
					string resx = Path.Combine("Resources", GetFileNameForResource(Path.ChangeExtension(r.Name, ".resx"), directories));
					var dirName2 = Path.GetDirectoryName(resx);
					if (!string.IsNullOrEmpty(dirName2) && directories.Add(dirName2))
					{
						Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dirName2));
					}
					//using (ResourceReader reader = new ResourceReader(stream))
					if (res.StringTableEntries.Count + res.OtherEntries.Count + refs.Count != 0)
					{
						using (FileStream fs = new FileStream(Path.Combine(options.SaveAsProjectDirectory, resx), FileMode.Create, FileAccess.Write))
						using (ResXResourceWriter writer = new ResXResourceWriter(fs))
						{
							foreach (var item in res.StringTableEntries)
							{
								writer.AddResource(item.Key, item.Value);
							}
							foreach (var item in res.OtherEntries)
							{
								writer.AddResource(new ResXDataNode(item.Key, item.Value));
							}

							foreach (var item in refs)
							{
								writer.AddResource(new ResXDataNode(item.Key, item.Value));
							}
						}

						yield return Tuple.Create("EmbeddedResource", resx);
					}
					
					
				} else {
					string fileName = Path.Combine("Resources", GetFileNameForResource(r.Name, directories));
					using (FileStream fs = new FileStream(Path.Combine(options.SaveAsProjectDirectory, fileName), FileMode.Create, FileAccess.Write)) {
						stream.Position = 0;
						stream.CopyTo(fs);
					}
					yield return Tuple.Create("EmbeddedResource", fileName);
				}
			}
		}

		private static string SniffFileType(Stream entryStream)
		{
			var buffer = new byte[10];
			var len = entryStream.Read(buffer, 0, buffer.Length);
			var b = System.Text.Encoding.GetEncoding("windows-1252").GetString(buffer, 0, len);
			if (b.StartsWith("‰PNG")) return ".png";
			if (b.StartsWith("GIF89a")) return ".gif";
			if (b.StartsWith("GIF87a")) return ".gif";
			if (b.StartsWith("ÿØÿ")) return ".jpg";
			if (b.StartsWith("MZ")) return ".dll";
			if (b.StartsWith("PK")) return ".zip";
			if (b.StartsWith("BM")) return ".bmp";
			if (b.StartsWith("<!doctype")) return ".html";
			if (b.StartsWith("\0\0\x01\0")) return ".ico";


			return null;
		}

		string GetFileNameForResource(string fullName, HashSet<string> directories)
		{
			return TextView.DecompilerTextView.CleanUpName(fullName);
			/*
			string[] splitName = fullName.Split('.');
			string fileName = TextView.DecompilerTextView.CleanUpName(fullName);
			for (int i = splitName.Length - 1; i > 0; i--) {
				string ns = string.Join(".", splitName, 0, i);
				if (directories.Contains(ns)) {
					string name = string.Join(".", splitName, i, splitName.Length - i);
					fileName = Path.Combine(ns, TextView.DecompilerTextView.CleanUpName(name));
					break;
				}
			}
			return fileName;
			*/
		}

		ResourceSet GetEntries(Stream stream)
		{
			try {
				return new ResourceSet(stream);
			} catch (ArgumentException) {
				return null;
			}
		}
#endregion

	}
}
