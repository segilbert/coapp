//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Original Code from http://www.ikriv.com/en/prog/info/dotnet/Eval.html
//     Copyright Ivan Kriyakov
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------


using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using Microsoft.CSharp;

namespace CoApp.Toolkit.DynamicXml {
    public class Compiler : MarshalByRefObject 
    {
        CodeGenerator _codeGenerator;
        CompilerImpl _compilerImpl;
        DomainUtil _domainUtil;

        List<string> _namespaces = new List<string>();
        List<string> _references = new List<string>();
        List<string> _definitions = new List<string>();
        IStackWalk _permissions = GetDefaultPermissions();

        public Compiler()
            :
            this(GetDotNet40CSharpProvider())
        {
        }

        public Compiler(CodeDomProvider provider)
            :
            this(new CompilerImpl(provider), new CodeGenerator(), new DomainUtil())
        {
        }

        internal Compiler(CompilerImpl compilerImpl, CodeGenerator codeGenerator, DomainUtil domainUtil)
        {
            _compilerImpl = compilerImpl;
            _codeGenerator = codeGenerator;
            _domainUtil = domainUtil;
        }

        public ReadOnlyCollection<string> Namespaces
        {
            get { return _namespaces.AsReadOnly(); }
        }

        public Compiler Using(string @namespace)
        {
            _namespaces.Add(@namespace);
            return this;
        }

        public Compiler Using(IEnumerable<string> namespaces)
        {
            _namespaces.AddRange(namespaces);
            return this;
        }

        public ReadOnlyCollection<string> References
        {
            get { return _references.AsReadOnly(); }
        }

        public Compiler Reference(string assembly)
        {
            _references.Add(assembly);
            return this;
        }

        public Compiler Reference(IEnumerable<string> assemblies)
        {
            _references.AddRange(assemblies);
            return this;
        }

        public Compiler ReferenceAllLoaded()
        {
            _references.AddRange(_domainUtil.GetNamesOfLoadedAssemblies());
            return this;
        }

        public ReadOnlyCollection<string> Definitions
        {
            get { return _definitions.AsReadOnly(); }
        }

        public Compiler Define(string source)
        {
            _definitions.Add(source);
            return this;
        }

        public Compiler Define(IEnumerable<string> sources)
        {
            _definitions.AddRange(sources);
            return this;
        }

        public IStackWalk Permissions
        {
            get { return _permissions; }
        }

        public Compiler SetPermissions(IStackWalk permissions)
        {
            _permissions = permissions;
            return this;
        }

        public void Clear()
        {
            _references.Clear();
            _definitions.Clear();
            _namespaces.Clear();
            _permissions = GetDefaultPermissions();
        }

        public object Eval(string expression)
        {
            return Eval(expression, null);
        }

        public object Eval(string expression, IDictionary<object, object> args)
        {
            var sources = _codeGenerator.GetSources(_code, expression, _namespaces, _definitions);
            _Assembly asm = _compilerImpl.Compile(sources, _references);
            var obj = (IComparer<IDictionary<object, object>>)asm.CreateInstance("CoApp.Eval.GeneratedCode.Foo");
            var result = new Dictionary<object, object>();
            _permissions.PermitOnly();
            obj.Compare(args, result);
            return result[String.Empty];
        }

        private static CodeDomProvider GetDotNet40CSharpProvider()
        {
            return new CSharpCodeProvider(
                new Dictionary<string, string>() { {"CompilerVersion", "v4.0"} });
        }

        private static IStackWalk GetDefaultPermissions()
        {
            return new PermissionSet(PermissionState.None);
        }

        const string _code =
            "[assembly:System.Security.SecurityTransparent]\n" +
            "namespace CoApp.Eval.GeneratedCode\n" +
            "{\n"+
            "   public class Foo : System.Collections.Generic.IComparer<System.Collections.Generic.IDictionary<object,object>>\n" +
            "   {\n" +
            "       public int Compare( System.Collections.Generic.IDictionary<object,object> args, System.Collections.Generic.IDictionary<object,object> result)\n" +
            "       {\n" +
            "           result[String.Empty] = ?;\n" +
            "           return 0;\n" +
            "       }\n" +
            "   }\n" +
            "}";
    }


    public class CompilerException : Exception {
        private CompilerResults _results;

        public CompilerException(CompilerResults results)
            :
            this(results, new MessageBuilder()) {
        }

        internal CompilerException(CompilerResults results, MessageBuilder messageBuilder)
            :
            base(messageBuilder.GetMessage(results)) {
            _results = results;
        }

        public CompilerResults CompilerResults {
            get { return _results; }
        }

        internal class MessageBuilder {
            public virtual string GetMessage(CompilerResults results) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Compilation error(s):");
                foreach (CompilerError error in results.Errors) {
                    sb.AppendLine(GetMessageForError(error));
                }

                return sb.ToString();
            }

            internal virtual string GetMessageForError(CompilerError error) {
                return
                    String.Format("{0}({1},{2}): Error {3}: {4}",
                        error.FileName,
                        error.Line,
                        error.Column,
                        error.ErrorNumber,
                        error.ErrorText);
            }
        }
    }

    internal class CompilerImpl : MarshalByRefObject {
        CodeDomProvider _provider;

        public CompilerImpl(CodeDomProvider provider) {
            _provider = provider;
        }

        public _Assembly Compile(IEnumerable<string> sources, IEnumerable<string> references) {
            CompilerParameters options = new CompilerParameters();
            options.GenerateInMemory = true;
            options.ReferencedAssemblies.AddRange(references.ToArray());
            return Check(_provider.CompileAssemblyFromSource(options, sources.ToArray()));
        }

        private static _Assembly Check(CompilerResults results) {
            if (results.Errors.Count > 0) {
                throw new CompilerException(results);
            }

            return results.CompiledAssembly;
        }

    }
    internal class DomainUtil : MarshalByRefObject {
        public IEnumerable<string> GetNamesOfLoadedAssemblies() {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                string location = GetSafeLocation(assembly);
                if (!String.IsNullOrEmpty(location)) {
                    yield return location;
                }
            }
        }

        private static string GetSafeLocation(Assembly assembly) {
            try {
                return assembly.Location;
            } catch (NotSupportedException) {
                // thrown for dynamic assemblies
                // unfortunately, I did not find a way to check whether the assembly is dynamic
                return null;
            }
        }
    }
    internal class CodeGenerator : MarshalByRefObject {
        public IEnumerable<string> GetSources(string codeTemplate, string expression, IEnumerable<string> namespaces, ICollection<string> definitions) {
            yield return GetCode(codeTemplate, expression, namespaces);

            foreach (string source in definitions) {
                yield return source;
            }
        }

        /* internal virtual for tests */
        internal virtual string GetCode(string codeTemplate, string expression, IEnumerable<string> namespaces) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            foreach (string @namespace in namespaces) {
                sb.AppendFormat("using {0};\r\n", @namespace);
            }

            sb.Append(codeTemplate.Replace("?", expression));
            return sb.ToString();
        }
    }

}
